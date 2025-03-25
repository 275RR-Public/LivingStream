// using UnityEngine;
// using System;
// using System.Collections.Generic;
// using System.Net;
// using System.Net.Sockets;
// using System.Text;
// using System.Threading;

// [System.Serializable]
// public class TrackingData {
//     public int id;
//     public int[] center; // Example: [x, y] in sensor pixels
//     public float depth;
// }

// [System.Serializable]
// public class TrackingDataWrapper {
//     public TrackingData[] data;
// }

// public class FishSceneInteraction : MonoBehaviour
// {
//     [Header("UDP Settings")]
//     public int port = 5005; // Must match the Python script

//     [Header("Sensor to Scene Mapping")]
//     [Tooltip("Sensor resolution width (pixels)")]
//     public float sensorWidth = 640f;
//     [Tooltip("Sensor resolution height (pixels)")]
//     public float sensorHeight = 480f;
//     [Tooltip("Scene width (Unity units) corresponding to sensor width")]
//     public float sceneWidth = 10f;
//     [Tooltip("Scene height (Unity units) corresponding to sensor height")]
//     public float sceneHeight = 7.5f;
//     [Tooltip("Offset for the bottom-left corner of the scene in Unity world coordinates")]
//     public Vector2 sceneOrigin = Vector2.zero;

//     [Header("Fish and Effects")]
//     [Tooltip("List of fish GameObjects in the scene")]
//     public List<GameObject> fishObjects;
//     [Tooltip("Prefab for water splash effect")]
//     public GameObject waterSplashPrefab;
//     [Tooltip("Minimum distance for a fish to be affected")]
//     public float influenceRadius = 2.0f;
//     [Tooltip("Speed at which fish move away")]
//     public float pushSpeed = 5f;

//     // UDP listener fields
//     private Thread receiveThread;
//     private UdpClient udpClient;

//     // Thread-safe container for latest detections
//     private List<TrackingData> latestDetections = new List<TrackingData>();
//     private readonly object dataLock = new object();

//     void Start()
//     {
//         StartUDPListener();
//     }

//     void StartUDPListener()
//     {
//         try {
//             udpClient = new UdpClient(port);
//             receiveThread = new Thread(new ThreadStart(ReceiveData));
//             receiveThread.IsBackground = true;
//             receiveThread.Start();
//             Debug.Log("UDP listener started on port " + port);
//         }
//         catch(Exception e) {
//             Debug.Log("Error starting UDP listener: " + e.Message);
//         }
//     }

//     void ReceiveData()
//     {
//         IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
//         while (true)
//         {
//             try {
//                 byte[] data = udpClient.Receive(ref remoteEP);
//                 string json = Encoding.UTF8.GetString(data);
//                 Debug.Log("Received UDP data: " + json);
                
//                 // Expecting JSON in the format: 
//                 // [ { "id": 0, "center": [320,240], "depth": 1.23 }, ... ]
//                 string wrappedJson = "{\"data\":" + json + "}";
//                 TrackingDataWrapper wrapper = JsonUtility.FromJson<TrackingDataWrapper>(wrappedJson);
//                 if (wrapper != null && wrapper.data != null)
//                 {
//                     lock (dataLock)
//                     {
//                         latestDetections.Clear();
//                         latestDetections.AddRange(wrapper.data);
//                     }
//                 }
//             }
//             catch (SocketException se) {
//                 Debug.Log("SocketException: " + se.Message);
//             }
//             catch (ThreadAbortException) {
//                 Debug.Log("Receive thread aborted.");
//                 return;
//             }
//             catch (Exception ex) {
//                 Debug.Log("Exception in ReceiveData: " + ex.Message);
//             }
//         }
//     }

//     void Update()
//     {
//         List<TrackingData> detectionsCopy;
//         lock (dataLock)
//         {
//             detectionsCopy = new List<TrackingData>(latestDetections);
//         }

//         // Process each detection
//         foreach (TrackingData td in detectionsCopy)
//         {
//             if (td.center == null || td.center.Length < 2)
//                 continue;
            
//             // Convert sensor coordinates (pixel values) to scene coordinates (Unity units)
//             Vector2 scenePos = SensorToSceneCoordinates(td.center[0], td.center[1]);
            
//             // Trigger water splash effect at the detection point
//             TriggerWaterSplash(scenePos);

//             // For each fish, if it is close to the detection, move it away
//             foreach (GameObject fish in fishObjects)
//             {
//                 Vector2 fishPos = new Vector2(fish.transform.position.x, fish.transform.position.y);
//                 float distance = Vector2.Distance(fishPos, scenePos);
//                 if (distance < influenceRadius)
//                 {
//                     // Calculate push direction (fish moves away from the detection)
//                     Vector2 pushDir = (fishPos - scenePos).normalized;
//                     fish.transform.position += new Vector3(pushDir.x, pushDir.y, 0) * pushSpeed * Time.deltaTime;
//                 }
//             }
//         }
//     }

//     Vector2 SensorToSceneCoordinates(float sensorX, float sensorY)
//     {
//         // Convert sensor pixel coordinates (origin top-left) to normalized coordinates [0,1]
//         float normalizedX = sensorX / sensorWidth;
//         float normalizedY = sensorY / sensorHeight;
        
//         // Invert Y if sensor's origin is top-left but scene's origin is bottom-left
//         normalizedY = 1 - normalizedY;
        
//         // Map normalized coordinates to scene dimensions and apply scene origin offset
//         float sceneX = normalizedX * sceneWidth + sceneOrigin.x;
//         float sceneY = normalizedY * sceneHeight + sceneOrigin.y;
//         return new Vector2(sceneX, sceneY);
//     }

//     void TriggerWaterSplash(Vector2 position)
//     {
//         if (waterSplashPrefab != null)
//         {
//             // Instantiate water splash effect at the given scene position
//             Instantiate(waterSplashPrefab, new Vector3(position.x, position.y, 0), Quaternion.identity);
//         }
//     }

//     void OnApplicationQuit()
//     {
//         if (receiveThread != null)
//             receiveThread.Abort();
//         if (udpClient != null)
//             udpClient.Close();
//     }
// }
