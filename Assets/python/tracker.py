import cv2
import numpy as np
import pyrealsense2 as rs
from deep_sort_realtime.deepsort_tracker import DeepSort
from ultralytics import YOLO
import statistics
import socket
import json

udp_ip = "127.0.0.1"   # Unity's IP (change if needed)
udp_port = 5005        # Unity's receiving port
udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Initialize RealSense pipeline
pipeline = rs.pipeline()
config = rs.config()
config.enable_stream(rs.stream.depth, 640, 480, rs.format.z16, 30)
config.enable_stream(rs.stream.color, 640, 480, rs.format.bgr8, 30)

# Start the pipeline
profile = pipeline.start(config)

# Align depth to color
align = rs.align(rs.stream.color)

# Create colorizer for depth visualization
colorizer = rs.colorizer()

# Load YOLOv8n model
model = YOLO('yolo11n.pt')

# Initialize DeepSORT tracker
tracker = DeepSort(max_age=30)

# Depth history for smoothing (dictionary to store depth values for each track)
depth_history = {}
max_history = 10  # Number of frames to keep in history for smoothing

try:
    while True:
        # Wait for frames
        frames = pipeline.wait_for_frames()
        aligned_frames = align.process(frames)
        
        # Get depth and color frames
        depth_frame = aligned_frames.get_depth_frame()
        color_frame = aligned_frames.get_color_frame()

        if not depth_frame or not color_frame:
            continue

        # Convert to numpy arrays
        color_image = np.asanyarray(color_frame.get_data())
        depth_image = np.asanyarray(depth_frame.get_data())
        
        # Create depth colormap for visualization
        depth_colormap = np.asanyarray(colorizer.colorize(depth_frame).get_data())

        # Detect people with YOLOv8
        results = model(color_image)
        detections = results[0].boxes.data.cpu().numpy()

        # Filter for 'person' class with confidence > 0.5
        person_detections = []
        for det in detections:
            if det[5] == 0 and det[4] > 0.5:  # Class 0 is 'person', confidence > 0.5
                x_min, y_min, x_max, y_max = map(int, det[:4])
                person_detections.append(([x_min, y_min, x_max, y_max], det[4]))

        # Update tracker with filtered detections
        tracks = tracker.update_tracks(person_detections, frame=color_image)

        # List to store tracking info for UDP transmission
        tracking_info = []
        
        # Process each track
        for track in tracks:
            if not track.is_confirmed():
                continue
                
            track_id = track.track_id
            bbox = track.to_tlbr()  # [x_min, y_min, x_max, y_max]
            
            # Convert to integers and clip to frame boundaries
            x_min = max(0, int(bbox[0]))
            y_min = max(0, int(bbox[1]))
            x_max = min(color_frame.get_width() - 1, int(bbox[2]))
            y_max = min(color_frame.get_height() - 1, int(bbox[3]))
            
            # Skip if box is too small
            if x_max - x_min < 10 or y_max - y_min < 10:
                continue
                
            # Sample multiple points in the bounding box for depth
            sample_points = []
            depths = []
            
            # Create a depth ROI (Region of Interest) for the torso area
            torso_top = y_min + int((y_max - y_min) * 0.3)  # Top of torso (30% down from top)
            torso_bottom = y_min + int((y_max - y_min) * 0.7)  # Bottom of torso (70% down from top)
            
            # Determine sampling density based on box size
            step = max(5, int((x_max - x_min) / 10))  # At least 10 samples across width
            
            # Use numpy array for depth - much faster to process
            roi_height = torso_bottom - torso_top
            roi_width = x_max - x_min
            
            # Ensure ROI boundaries are valid
            if roi_height <= 0 or roi_width <= 0:
                continue
                
            # Sample multiple points within the ROI
            for y in range(torso_top, torso_bottom, step):
                for x in range(x_min + step, x_max - step, step):
                    if 0 <= x < depth_frame.get_width() and 0 <= y < depth_frame.get_height():
                        # Get distance in meters
                        depth_val = depth_frame.get_distance(x, y)
                        if 0.5 < depth_val < 5:  # Valid depth range
                            depths.append(depth_val)
                            sample_points.append((x, y))
            
            # Calculate depth if we have samples
            if depths:
                # Remove outliers before calculating median
                # Sort depths and remove top and bottom 10% if we have enough samples
                if len(depths) > 10:
                    depths.sort()
                    cut_off = len(depths) // 10
                    depths = depths[cut_off:-cut_off]
                
                current_depth = statistics.median(depths)
                
                # Initialize history if this is a new track
                if track_id not in depth_history:
                    depth_history[track_id] = []
                
                # Add current depth to history
                depth_history[track_id].append(current_depth)
                
                # Keep only the most recent values
                if len(depth_history[track_id]) > max_history:
                    depth_history[track_id].pop(0)
                
                # Calculate smoothed depth from history
                smoothed_depth = statistics.median(depth_history[track_id])
                
                # Draw sample points used for depth calculation
                for pt in sample_points:
                    cv2.circle(color_image, pt, 1, (0, 100, 255), -1)
                
                # Draw centroid
                centroid = (x_min + (x_max - x_min) // 2, y_min + (y_max - y_min) // 2)
                cv2.circle(color_image, centroid, 5, (0, 0, 255), -1)
                
                # Draw bounding box and text
                cv2.rectangle(color_image, (x_min, y_min), (x_max, y_max), (0, 255, 0), 2)
                cv2.putText(color_image, f"ID: {track_id}", 
                            (x_min, y_min - 30), cv2.FONT_HERSHEY_SIMPLEX, 
                            0.75, (0, 255, 0), 2)
                cv2.putText(color_image, f"Depth: {smoothed_depth:.2f}m", 
                            (x_min, y_min - 10), cv2.FONT_HERSHEY_SIMPLEX, 
                            0.75, (0, 255, 0), 2)
                
                # Draw number of samples used
                cv2.putText(color_image, f"Samples: {len(sample_points)}", 
                            (x_min, y_max + 20), cv2.FONT_HERSHEY_SIMPLEX, 
                            0.6, (255, 0, 0), 2)
                
                # Add tracking data for UDP transmission
                tracking_info.append({
                    'id': track_id,
                    'center': [centroid[0], centroid[1]],
                    'depth': smoothed_depth
                })                
                
            else:
                # No valid depth points found
                cv2.rectangle(color_image, (x_min, y_min), (x_max, y_max), (0, 0, 255), 2)
                cv2.putText(color_image, f"ID: {track_id} No depth", 
                            (x_min, y_min - 10), cv2.FONT_HERSHEY_SIMPLEX, 
                            0.75, (0, 0, 255), 2)

        # Clean up old tracks from history dictionary
        active_ids = {track.track_id for track in tracks if track.is_confirmed()}
        depth_history = {k: v for k, v in depth_history.items() if k in active_ids}

        # Send data to Unity over UDP
        udp_message = json.dumps(tracking_info)
        try:
            udp_sock.sendto(udp_message.encode("utf-8"), (udp_ip, udp_port))
            print("Sent UDP data:", udp_message)
        except Exception as e:
            print("UDP send error:", e)
        # Display result
        # Create a horizontal stack of color image and depth colormap for visualization
        depth_colormap_resized = cv2.resize(depth_colormap, (color_image.shape[1], color_image.shape[0]))
        display_image = np.hstack((color_image, depth_colormap_resized))
        cv2.imshow("Tracking with Depth", display_image)
        
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

finally:
    pipeline.stop()
    cv2.destroyAllWindows()
    udp_sock.close()