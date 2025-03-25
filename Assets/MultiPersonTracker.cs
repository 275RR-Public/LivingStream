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
    public int[] center; // e.g., [x, y]
    public float depth;
}

[System.Serializable]
public class TrackingDataWrapper {
    public TrackingData[] data;
}

public class MultiPersonTracker : MonoBehaviour
{
    [Header("UDP Settings")]
    public int port = 5005;

    [Header("Cube Prefab")]
    public GameObject cubePrefab; // Assign a prefab of your cube in the Inspector

    [Header("Conversion Settings")]
    public float scaleFactor = 100.0f; // Conversion from pixel coordinates to Unity units

    private Thread receiveThread;
    private UdpClient udpClient;

    // Dictionary to store cubes by detection ID
    private Dictionary<int, GameObject> detectionCubes = new Dictionary<int, GameObject>();

    // Thread-safe variables to store latest tracking data from UDP
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
            Debug.Log("Error starting UDP listener: " + e.Message);
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string json = Encoding.UTF8.GetString(data);
                Debug.Log("Received UDP data: " + json);
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

        // Process each detection
        foreach (TrackingData td in detectionsCopy)
        {
            if (td.center == null || td.center.Length < 2)
                continue;

            float posX = td.center[0] / scaleFactor;
            float posY = td.center[1] / scaleFactor;
            Vector3 newPos = new Vector3(posX, posY, 0);  // Adjust Z as needed

            // Check if a cube for this detection already exists
            if (!detectionCubes.ContainsKey(td.id))
            {
                // Instantiate a new cube and add it to the dictionary
                GameObject newCube = Instantiate(cubePrefab, newPos, Quaternion.identity);
                detectionCubes.Add(td.id, newCube);
                Debug.Log("Instantiated new cube for detection ID " + td.id);
            }
            else
            {
                // Update position of the existing cube
                detectionCubes[td.id].transform.position = newPos;
                Debug.Log("Updated cube position for detection ID " + td.id);
            }
        }

        // Optional: Remove cubes for detections that are no longer present
        List<int> activeIDs = new List<int>();
        foreach (TrackingData td in detectionsCopy)
            activeIDs.Add(td.id);
        List<int> keys = new List<int>(detectionCubes.Keys);
        foreach (int id in keys)
        {
            if (!activeIDs.Contains(id))
            {
                Destroy(detectionCubes[id]);
                detectionCubes.Remove(id);
                Debug.Log("Destroyed cube for lost detection ID " + id);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null)
            receiveThread.Abort();
        if (udpClient != null)
            udpClient.Close();
    }
}
