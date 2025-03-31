import numpy as np
import statistics
import pyrealsense2 as rs
from deep_sort_realtime.deepsort_tracker import DeepSort
from ultralytics import YOLO

class Tracker:
    def __init__(self):
        self.intrinsics = None                  # Will store camera intrinsics
        self.pipeline = rs.pipeline()           # Initialize and start RealSense pipeline
        self.config = rs.config()
        self.config.enable_stream(rs.stream.depth, 1280, 720, rs.format.z16, 30)
        self.config.enable_stream(rs.stream.color, 1920, 1080, rs.format.bgr8, 30)
        self.profile = self.pipeline.start(self.config)

        self.align = rs.align(rs.stream.color)  # Aligns depth to color
        self.colorizer =  rs.colorizer()        # Create colorizer for depth visualization

        self.model = YOLO('yolo11n.pt')         # Initialize YOLO model for object detection

        # Initialize DeepSORT for object tracking
        # max_age = number of frames a track can persist without being associated with a new detection
        # Increase to combat occlusions or missed detections
        # Decrease to reduce "ghost tracks" where object has left the scene (more responsive)
        self.tracker = DeepSort(max_age=30)
        self.depth_history = {}                 # for smoothing (dictionary to store depth values for each track)
        self.max_history = 10                   # Number of frames to keep in history for smoothing

        self.person_class = 0                   # YOLO - Class 0 is 'person'
        self.person_confidence = 0.6            # YOLO - Percent confident of detection

        self.roi_start = 0.3                    # Region of Interest (ROI) - target torso in bounding box
        self.roi_end = 0.7
        self.roi_width_samples = 10
        self.roi_min_depth = 0.5                # min valid depth range (in meters)
        self.roi_max_depth = 3                  # max valid depth range (in meters)
        self.roi_feet_offset = 1                # add offset to approx from torso depth to feet depth

    def process_frame(self, with_images=False):
        """Process a frame and return tracking data, optionally with images."""
        # Get frames and convert to numpy array
        frames = self.pipeline.wait_for_frames()
        aligned_frames = self.align.process(frames)
        depth_frame = aligned_frames.get_depth_frame()
        color_frame = aligned_frames.get_color_frame()

        if not depth_frame or not color_frame:
            return [], None, None if with_images else []
        
        # Get camera intrinsics from the color frame (only once)
        if self.intrinsics is None:
            color_profile = color_frame.profile.as_video_stream_profile()
            self.intrinsics = color_profile.intrinsics

        color_image = np.asanyarray(color_frame.get_data())

        # Detection with YOLO
        results = self.model(color_image)
        detections = results[0].boxes.data.cpu().numpy()
        # Filter for 'person' class with confidence
        person_detections = [
            ([int(x) for x in det[:4]], det[4])
            for det in detections if det[5] == self.person_class and det[4] > self.person_confidence
        ]

        # Update DeepSort tracker with filtered detections
        tracks = self.tracker.update_tracks(person_detections, frame=color_image)

        # Process each track
        tracking_data = []
        for track in tracks:
            if not track.is_confirmed():
                continue
            track_id = track.track_id
            # Top-left, bottom-right bounding box coordinates
            bbox = track.to_tlbr()      # [x_min, y_min, x_max, y_max]

            # Convert to integers and clip to frame boundaries
            x_min = max(0, int(bbox[0]))
            y_min = max(0, int(bbox[1]))
            x_max = min(color_frame.get_width() - 1, int(bbox[2]))
            y_max = min(color_frame.get_height() - 1, int(bbox[3]))

            # Skip if box is too small
            if x_max - x_min < 10 or y_max - y_min < 10:
                continue

            # Create a depth ROI (Region of Interest) for the torso area
            torso_top = y_min + int((y_max - y_min) * self.roi_start)   # Top of torso (% down from top)
            torso_bottom = y_min + int((y_max - y_min) * self.roi_end)  # Bottom of torso (% down from top)
            step = max(5, int((x_max - x_min) / self.roi_width_samples))# sampling density based on box size (>10 samples across width)

            # Sample multiple points within the ROI
            depths = []
            for y in range(torso_top, torso_bottom, step):
                for x in range(x_min + step, x_max - step, step):
                    if 0 <= x < depth_frame.get_width() and 0 <= y < depth_frame.get_height():
                        depth_val = depth_frame.get_distance(x, y)                  # Get distance in meters
                        if self.roi_min_depth < depth_val < self.roi_max_depth:     # Valid depth range
                            depths.append(depth_val)

            depth = None
            if depths:
                # Calculate depth if we have samples
                # Remove outliers before calculating median
                # Sort depths and remove top and bottom 10% if we have enough samples
                if len(depths) > 10:
                    depths.sort()
                    cut_off = len(depths) // 10
                    depths = depths[cut_off:-cut_off]

                current_depth = statistics.median(depths)
                # Initialize history if this is a new track
                if track_id not in self.depth_history:
                    self.depth_history[track_id] = []
                # Add current depth to history
                self.depth_history[track_id].append(current_depth)
                # Keep only the most recent values
                if len(self.depth_history[track_id]) > self.max_history:
                    self.depth_history[track_id].pop(0)
                # Calculate smoothed depth from history
                depth = statistics.median(self.depth_history[track_id])

            # Define feet pixel as bottom center of the bounding box
            feet_pixel = [x_min + (x_max - x_min) // 2, y_max]

            # Deproject to 3D if depth is valid
            point_3d = None
            if depth is not None:
                depth = depth + self.roi_feet_offset
                point_3d = rs.rs2_deproject_pixel_to_point(self.intrinsics, feet_pixel, depth)

            # Add to tracking data
            tracking_data.append({
                'id': track_id,
                'position': point_3d if point_3d else None,
                'bbox': [x_min, y_min, x_max, y_max]
            })

        # Clean up depth history
        active_ids = {track.track_id for track in tracks if track.is_confirmed()}
        self.depth_history = {k: v for k, v in self.depth_history.items() if k in active_ids}

        if with_images:
            # Create depth colormap for visualization
            depth_colormap = np.asanyarray(self.colorizer.colorize(depth_frame).get_data())
            return tracking_data, color_image, depth_colormap
        return tracking_data, None, None

    def stop(self):
        self.pipeline.stop()