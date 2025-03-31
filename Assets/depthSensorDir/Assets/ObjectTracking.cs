using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;

// Define a serializable class for individual tracking data
[System.Serializable]
public class TrackData
{
    public int id;
    public float[] position; // Array for [x, y, z] or null
}

// Define a serializable wrapper for the list of tracking data
[System.Serializable]
public class TrackingDataList
{
    public List<TrackData> tracks;
}

public class ObjectTracking : MonoBehaviour
{
    // Assign a cube prefab in the Inspector
    public GameObject trackedBoxPrefab;
    private UdpClient udpClient;
    private Dictionary<int, GameObject> trackedObjects = new Dictionary<int, GameObject>();
    // Thread-safe queue for tracking data
    private ConcurrentQueue<List<TrackData>> trackingDataQueue = new ConcurrentQueue<List<TrackData>>();
    private Dictionary<int, float> lastSeenTimes = new Dictionary<int, float>();
    private float timeoutDuration = 2f;     // Seconds to wait before removing ghost object

    void Start()
    {
        udpClient = new UdpClient(5005); // Must match Python's UDP port
        udpClient.BeginReceive(ReceiveCallback, null);

        // Ensure the prefab is set; log a warning if not assigned in Inspector
        if (trackedBoxPrefab == null)
        {
            Debug.LogWarning("trackedBoxPrefab is not assigned! Creating a default cube.");
            trackedBoxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trackedBoxPrefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        }
    }

    void ReceiveCallback(System.IAsyncResult ar)
    {
        try
        {
            var endpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, 5005);
            byte[] data = udpClient.EndReceive(ar, ref endpoint);
            string message = Encoding.UTF8.GetString(data);
            Debug.Log("Received UDP message: " + message);

            // Wrap the JSON array in a root object to match TrackingDataList
            string wrappedMessage = "{\"tracks\":" + message + "}";
            Debug.Log("Wrapped message: " + wrappedMessage);

            TrackingDataList trackingDataList = JsonUtility.FromJson<TrackingDataList>(wrappedMessage);
            List<TrackData> trackingData = trackingDataList.tracks;
            Debug.Log("Deserialized tracking data count: " + trackingData.Count);

            // Enqueue the tracking data to process on the main thread
            trackingDataQueue.Enqueue(trackingData);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error in ReceiveCallback: " + e.Message);
        }

        // Continue listening for more data
        udpClient.BeginReceive(ReceiveCallback, null);
    }

    void Update()
    {
        // Keep track of all active tracks in scene
        HashSet<int> activeIds = new HashSet<int>();
        // Process all queued tracking data on the main thread
        while (trackingDataQueue.TryDequeue(out List<TrackData> trackingData))
        {
            foreach (var track in trackingData)
            {
                int id = track.id;
                activeIds.Add(id);
                lastSeenTimes[id] = Time.time; // Update last seen time
                Debug.Log($"Processing track ID: {id}, Position: {(track.position != null ? string.Join(", ", track.position) : "null")}");

                if (track.position != null && track.position.Length == 3)
                {
                    Vector3 realSensePos = new Vector3(track.position[0], track.position[1], track.position[2]);
                    Debug.Log($"RealSense position: {realSensePos}");

                    // Map RealSense coordinates to Unity coordinates
                    float tiltAngle = -30f;
                    Vector3 rotatedPos = Quaternion.Euler(tiltAngle, 0, 0) * realSensePos;
                    //float scale = 0.5f; // Scale down the movement
                    //Vector3 offset = new Vector3(0f, 0f, -2f); // Center the boxes
                    //Vector3 unityPos = new Vector3(rotatedPos.x * scale, 0f, rotatedPos.z * scale) + offset;
                    Vector3 unityPos = new Vector3(rotatedPos.x, 0f, rotatedPos.z);
                    Debug.Log($"Unity position: {unityPos}");

                    if (!trackedObjects.ContainsKey(id))
                    {
                        // Instantiate a new box for this tracked person
                        GameObject newBox = Instantiate(trackedBoxPrefab);
                        newBox.name = "TrackedBox_" + id;
                        trackedObjects[id] = newBox;
                        // Create a new material and set its color based on ID
                        Material newMaterial = new Material(Shader.Find("HDRP/Lit"));
                        float hue = (id % 10) / 10f;                        // Vary hue between 0 and 1 based on ID
                        newMaterial.color = Color.HSVToRGB(hue, 1f, 1f);    // Full saturation and value
                        newBox.GetComponent<MeshRenderer>().material = newMaterial;
                        Debug.Log($"Instantiated new box for ID {id} at position {unityPos}");
                    }

                    // Update the box's position on the plane
                    trackedObjects[id].transform.position = unityPos;
                    Debug.Log($"Updated position for ID {id} to {unityPos}");
                }
                else
                {
                    Debug.LogWarning($"Track ID {id} has invalid position data.");
                }
            }
        }

        // add ghost object to remove list if timeout reached
        List<int> idsToRemove = new List<int>();
        foreach (var id in trackedObjects.Keys)
        {
            if (!activeIds.Contains(id) && (Time.time - lastSeenTimes.GetValueOrDefault(id, 0f) > timeoutDuration))
            {
                idsToRemove.Add(id);
            }
        }

        // remove ghost objects
        foreach (var id in idsToRemove)
        {
            Destroy(trackedObjects[id]);
            trackedObjects.Remove(id);
            lastSeenTimes.Remove(id);
            Debug.Log($"Removed tracked object for ID {id}");
        }
    }

    void OnDestroy()
    {
        if (udpClient != null)
            udpClient.Close();
    }
}