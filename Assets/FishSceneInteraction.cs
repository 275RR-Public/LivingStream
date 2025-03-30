using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

[System.Serializable]
public class TrackingData {
    public int id;
    public int[] center; // e.g., [x, y] in sensor pixels
    public float depth;
}

[System.Serializable]
public class TrackingDataWrapper {
    public TrackingData[] data;
}

public class FishSceneInteraction : MonoBehaviour
{
    [Header("UDP Settings")]
    public int port = 5005; // Must match your Python script's send port

    [Header("Sensor to Scene Mapping (for XZ floor)")]
    [Tooltip("Sensor resolution width (pixels), e.g., 640")]
    public float sensorWidth = 640f;
    [Tooltip("Sensor resolution height (pixels), e.g., 480")]
    public float sensorHeight = 480f;
    [Tooltip("Scene width (Unity units) corresponding to sensor width")]
    public float sceneWidth = 10f;
    [Tooltip("Scene height (Unity units) corresponding to sensor height")]
    public float sceneHeight = 7.5f;
    [Tooltip("Offset for the bottom-left corner of the scene (in Unity XZ)")]
    public Vector2 sceneOrigin = Vector2.zero;

    [Header("Fish and Effects")]
    [Tooltip("Prefab for water splash effect (destroyed after a short lifetime)")]
    public GameObject waterSplashPrefab;
    [Tooltip("Minimum distance (in Unity units) for a fish to be affected")]
    public float influenceRadius = 2.0f;
    [Tooltip("Speed at which fish move away")]
    public float pushSpeed = 5f;

    [Header("Tracked Person Boxes")]
    [Tooltip("Prefab for the tracked person's box (one per detection)")]
    public GameObject personBoxPrefab;

    // Dictionary to store person boxes by detection ID
    private Dictionary<int, GameObject> personBoxes = new Dictionary<int, GameObject>();

    // UDP listener fields
    private Thread receiveThread;
    private UdpClient udpClient;

    // Thread-safe container for latest detections
    private List<TrackingData> latestDetections = new List<TrackingData>();
    private readonly object dataLock = new object();

    void Start()
    {
        StartUDPListener();
    }

    void StartUDPListener()
    {
        try {
            udpClient = new UdpClient(port);
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            Debug.Log("UDP listener started on port " + port);
        }
        catch(Exception e) {
            Debug.LogError("Error starting UDP listener: " + e.Message);
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try {
                byte[] data = udpClient.Receive(ref remoteEP);
                string json = Encoding.UTF8.GetString(data);
                Debug.Log("Received UDP data: " + json);

                // Expected JSON: [{ "id": 0, "center": [320,240], "depth": 1.23 }, ...]
                string wrappedJson = "{\"data\":" + json + "}";
                TrackingDataWrapper wrapper = JsonUtility.FromJson<TrackingDataWrapper>(wrappedJson);
                if (wrapper != null && wrapper.data != null)
                {
                    lock (dataLock)
                    {
                        latestDetections.Clear();
                        latestDetections.AddRange(wrapper.data);
                    }
                }
            }
            catch (SocketException se) {
                Debug.Log("SocketException: " + se.Message);
            }
            catch (ThreadAbortException) {
                Debug.Log("Receive thread aborted.");
                return;
            }
            catch (Exception ex) {
                Debug.Log("Exception in ReceiveData: " + ex.Message);
            }
        }
    }

    void Update()
    {
        List<TrackingData> detectionsCopy;
        lock (dataLock)
        {
            detectionsCopy = new List<TrackingData>(latestDetections);
        }

        // If no detections, remove all tracked boxes.
        if (detectionsCopy.Count == 0)
        {
            ClearAllPersonBoxes();
            return;
        }

        // Process each detection.
        foreach (TrackingData td in detectionsCopy)
        {
            if (td.center == null || td.center.Length < 2)
                continue;

            // Convert sensor pixel coordinates to scene (XZ) coordinates.
            Vector2 scenePosXZ = SensorToSceneCoordinates(td.center[0], td.center[1]);
            // Place the tracked object on the floor (y = 0).
            Vector3 newPos = new Vector3(scenePosXZ.x, 0, scenePosXZ.y);

            // Create or update the tracked person box.
            if (!personBoxes.ContainsKey(td.id))
            {
                GameObject box = Instantiate(personBoxPrefab, newPos, Quaternion.identity);
                personBoxes.Add(td.id, box);
                Debug.Log("Instantiated box for detection ID " + td.id);
            }
            else
            {
                personBoxes[td.id].transform.position = newPos;
                Debug.Log("Updated box for detection ID " + td.id);
            }

            // Trigger a water splash effect at the detection point.
            if (waterSplashPrefab != null)
            {
                GameObject splash = Instantiate(waterSplashPrefab, newPos, Quaternion.identity);
                Destroy(splash, 2f); // Destroy splash after 2 seconds.
            }

            // Push fish away from the detection.
            // Instead of relying on a preset list, we find all fish tagged as "Fish".
            GameObject[] fishArray = GameObject.FindGameObjectsWithTag("Fish");
            foreach (GameObject fish in fishArray)
            {
                Vector3 fishPos = fish.transform.position;
                // Use the fish's XZ coordinates.
                Vector2 fishPosXZ = new Vector2(fishPos.x, fishPos.z);
                float distance = Vector2.Distance(fishPosXZ, scenePosXZ);
                if (distance < influenceRadius)
                {
                    Vector2 pushDir = (fishPosXZ - scenePosXZ).normalized;
                    // Move fish in XZ, leaving y unchanged.
                    fish.transform.position += new Vector3(pushDir.x, 0, pushDir.y) * pushSpeed * Time.deltaTime;
                }
            }
        }

        // Remove boxes for detections that are no longer present.
        List<int> activeIDs = new List<int>();
        foreach (var td in detectionsCopy)
            activeIDs.Add(td.id);

        List<int> keys = new List<int>(personBoxes.Keys);
        foreach (int id in keys)
        {
            if (!activeIDs.Contains(id))
            {
                Destroy(personBoxes[id]);
                personBoxes.Remove(id);
                Debug.Log("Destroyed box for lost detection ID " + id);
            }
        }
    }

    // Maps sensor pixel coordinates (origin top-left) to scene XZ coordinates.
    Vector2 SensorToSceneCoordinates(float sensorX, float sensorY)
    {
        // Normalize sensor coordinates.
        float normX = sensorX / sensorWidth;  // 0 = left, 1 = right.
        float normY = sensorY / sensorHeight; // 0 = top, 1 = bottom.
        // Invert Y so that sensor top maps to scene bottom.
        normY = 1 - normY;

        float sceneX = normX * sceneWidth + sceneOrigin.x;
        float sceneZ = normY * sceneHeight + sceneOrigin.y;
        return new Vector2(sceneX, sceneZ);
    }

    // Removes all tracked person boxes.
    void ClearAllPersonBoxes()
    {
        foreach (var kvp in personBoxes)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        personBoxes.Clear();
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null)
            receiveThread.Abort();
        if (udpClient != null)
            udpClient.Close();
    }
}
