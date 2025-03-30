using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    public static FishSpawner Instance;
    public GameObject fishPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SpawnNewFish(Vector3 position, int newDirectionX, int newDirectionZ)
    {
        if (fishPrefab == null)
        {
            Debug.LogError("❌ FishPrefab is not assigned in FishSpawner!");
            return;
        }

        Debug.Log("Spawning new fish...");

        GameObject newFish = Instantiate(fishPrefab, position, Quaternion.identity);
        FishMovement fishMovement = newFish.GetComponent<FishMovement>();

        if (fishMovement != null)
        {
            fishMovement.directionX = newDirectionX;
            fishMovement.directionZ = newDirectionZ;
            fishMovement.StartCoroutine(fishMovement.FadeIn());
        }
        else
        {
            Debug.LogError("❌ FishMovement script not found on new fish!");
        }

        Debug.Log($"New fish spawned at {position}, moving in direction ({newDirectionX}, {newDirectionZ})");
    }
}
