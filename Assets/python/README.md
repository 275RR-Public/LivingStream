- usb3.0 or better cable and port required (if camera delay or blackouts notice, double check)
- windows10 might need to enable camera in privacy settings (search "camera privacy")
- install python 3.11.9
- create virtual env
- pip install pyrealsense2 opencv-python numpy ultralytics deep-sort-realtime
- pip install takes several minutes (be patient)
- on first run, AI model will download (internet access required)

- tracking can degrade in low light and high reflectivity
- to improve tracking try reduce reflective areas and increasing ambient light
- tracking at an angle is better than top-down (better track and larger FOV)

- yolo for detection of people
- deepsort for tracking
- udp for sending (x,y,z=depth) of tracked object