import cv2
import numpy as np

class UI:
    def __init__(self):
        self.window_name = "LivingStream Object Tracking"
        self.mode = "home"  # Possible modes: "home", "testing", "live", "exit"
        cv2.namedWindow(self.window_name)
        cv2.setMouseCallback(self.window_name, self.mouse_callback)

    def mouse_callback(self, event, x, y, flags, param):
        """Handle button clicks to change mode."""
        if event == cv2.EVENT_LBUTTONDOWN:
            if self.mode == "home":
                # Home screen buttons
                if 860 <= x <= 1160 and 400 <= y <= 500:  # Test Mode button
                    self.mode = "testing"
                elif 860 <= x <= 1160 and 550 <= y <= 650:  # Live Mode button
                    self.mode = "live"
                elif 860 <= x <= 1160 and 700 <= y <= 800:  # Exit button
                    self.mode = "exit"
            elif self.mode in ["testing", "live"]:
                # Back button on testing and live screens
                if 860 <= x <= 1060 and 900 <= y <= 1000:
                    self.mode = "home"

    def create_home_screen(self):
        # window 1920x1080
        frame = np.full((1080, 1920, 3), (100, 100, 100), dtype=np.uint8)
        # Title at the top, centered horizontally
        cv2.putText(frame, "Detection and Tracking", (760, 250), cv2.FONT_HERSHEY_SIMPLEX, 2, (0, 0, 0), 3)
        
        # Test button below title, centered horizontally
        cv2.rectangle(frame, (860, 400), (1160, 500), (64, 64, 64), -1)     # 300x100 button
        cv2.putText(frame, "Test Mode", (900, 460), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
        
        # Live Mode button, centered
        cv2.rectangle(frame, (860, 550), (1160, 650), (64, 64, 64), -1)     # 300x100 button   
        cv2.putText(frame, "Live Mode", (900, 610), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
        
        # Exit button, centered
        cv2.rectangle(frame, (860, 700), (1160, 800), (64, 64, 64), -1)     # 300x100 button
        cv2.putText(frame, "Exit", (940, 760), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
        
        return frame
    
    def create_live_screen(self):
        frame = np.full((1080, 1920, 3), (100, 100, 100), dtype=np.uint8)
        
        # Text centered vertically and horizontally
        cv2.putText(frame, "Sending data to Unity...", (660, 540), cv2.FONT_HERSHEY_SIMPLEX, 2, (0, 0, 0), 3)
        
        # Back button, centered horizontally, near bottom
        cv2.rectangle(frame, (860, 900), (1060, 1000), (64, 64, 64), -1)
        cv2.putText(frame, "Back", (900, 960), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)
        
        return frame

    def display_tracking_frame(self, color_image, depth_colormap, tracking_data):
        """Draw tracking info on color_image and display with depth_colormap."""
        for track in tracking_data:
            x_min, y_min, x_max, y_max = track['bbox']
            track_id = track['id']
            position = track['position']  # 3D point [x, y, z] or None

            # Define feet pixel as bottom center of the bounding box
            feet_pixel = (x_min + (x_max - x_min) // 2, y_max)  # cv2 expects tuple

            if position is not None:
                # Use the z-component of position as depth (distance from camera in meters)
                depth = position[2]
                cv2.circle(color_image, feet_pixel, 5, (0, 0, 255), -1)  # Red dot at feet
                cv2.rectangle(color_image, (x_min, y_min), (x_max, y_max), (0, 255, 0), 2)  # Green bbox
                cv2.putText(color_image, f"ID: {track_id}", (x_min, y_min - 30),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 255, 0), 2)
                cv2.putText(color_image, f"Depth: {depth:.2f}m", (x_min, y_min - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 255, 0), 2)
            else:
                cv2.rectangle(color_image, (x_min, y_min), (x_max, y_max), (0, 0, 255), 2)  # Red bbox
                cv2.putText(color_image, f"ID: {track_id} No depth", (x_min, y_min - 10),
                            cv2.FONT_HERSHEY_SIMPLEX, 0.75, (0, 0, 255), 2)

        # Add Back button to the testing screen
        cv2.rectangle(color_image, (860, 900), (1060, 1000), (64, 64, 64), -1)  # Dark gray button
        cv2.putText(color_image, "Back", (900, 960), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 255), 2)

        # Stack images horizontally
        display_image = np.hstack((color_image, depth_colormap))
        cv2.imshow(self.window_name, display_image)

    def get_mode(self):
        return self.mode

    def set_mode(self, new_mode):
        self.mode = new_mode