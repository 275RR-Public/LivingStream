import cv2
from tracker import Tracker
from ui import UI
from network import Network

# Initialize components
ui = UI()               # simple UI using openCV
tracker = Tracker()     # person detection and tracking
network = Network()     # send tracking data to Unity

# Main application loop
while True:
    current_mode = ui.get_mode()
    if current_mode == "home":
        frame = ui.create_home_screen()
        cv2.imshow(ui.window_name, frame)
    elif current_mode == "testing":
        tracking_data, color_image, depth_colormap = tracker.process_frame(with_images=True)
        if color_image is not None:
            ui.display_tracking_frame(color_image, depth_colormap, tracking_data)
        network.send_tracking_data(tracking_data)
    elif current_mode == "live":
        tracking_data, _, _ = tracker.process_frame(with_images=False)
        frame = ui.create_live_screen()
        cv2.imshow(ui.window_name, frame)
        network.send_tracking_data(tracking_data)
    elif current_mode == "exit":
        break

    # Handle keyboard input - "q" for back/exit
    key = cv2.waitKey(1) & 0xFF
    if key == ord('q'):
        ui.set_mode("home" if current_mode != "home" else "exit")

# Cleanup
tracker.stop()
network.close()
cv2.destroyAllWindows()