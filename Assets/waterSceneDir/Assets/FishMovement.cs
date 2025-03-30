using UnityEngine;
using System.Collections;

public class FishMovement : MonoBehaviour
{
    public float speed = 2f;  // Movement speed
    public int directionX = 1;  
    public int directionZ = 1;  
    public float minX = -15f;
    public float maxX = 15f;   
    public float minZ = -15f;  
    public float maxZ = 15f;   
    public float fadeDuration = 1f; // Time to fade out/in

    public float swayAmplitude = 0.5f;  // How much the body sways (left/right)
    public float swayFrequency = 2f;  // How fast the fish sways
    
    private float timeOffset; 
    private Renderer fishRenderer;
    private bool isFading = false;

    void Start()
    {
        fishRenderer = GetComponent<Renderer>();
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        transform.position = new Vector3(randomX, transform.position.y, randomZ);
        timeOffset = Random.Range(0f, Mathf.PI * 2);
    }

    void Update()
    {
        if (isFading) return; // Prevent movement during fade

        transform.position += new Vector3(directionX * speed * Time.deltaTime, 0, directionZ * speed * Time.deltaTime);

        float swayOffset = Mathf.Sin(Time.time * swayFrequency + timeOffset) * swayAmplitude;
        transform.position += transform.right * swayOffset * Time.deltaTime;

        float swayAngle = Mathf.Sin(Time.time * swayFrequency + timeOffset) * 10f;
        transform.rotation = Quaternion.Euler(0, (directionX == 1 ? 0 : 180) + swayAngle, 0);

        // **Boundary Check for X axis only (for respawn)**
        if (transform.position.x <= minX || transform.position.x >= maxX)
        {
            StartCoroutine(FadeOutAndRespawn()); // Trigger fade out and respawn on X axis hit
        }

        // **Bounce on Z axis (top/bottom borders)**
        if (transform.position.z <= minZ || transform.position.z >= maxZ)
        {
            directionZ *= -1;  // Reverse Z direction (bounce)
        }
    }

    IEnumerator FadeOutAndRespawn()
    {
        isFading = true;
        float t = 0;
        Material material = fishRenderer.material;

        // Fade out the fish
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            Color color = material.color;
            color.a = Mathf.Lerp(1, 0, t / fadeDuration);
            material.color = color;
            yield return null;
        }

        // Ensure FishSpawner instance is valid before calling it
        if (FishSpawner.Instance != null)
        {
            FishSpawner.Instance.SpawnNewFish(transform.position, -directionX, directionZ);  // Reverse X direction, keep Z direction same
        }
        else
        {
            Debug.LogError("❌ FishSpawner.Instance is NULL! Make sure a FishSpawner exists in the scene.");
        }

        // Destroy the current fish after fading out
        Destroy(gameObject);
    }

    public IEnumerator FadeIn()
    {
        float t = 0;
        Material material = GetComponent<Renderer>().material;
        Color color = material.color;
        color.a = 0;
        material.color = color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0, 1, t / fadeDuration);
            material.color = color;
            yield return null;
        }
    }
}
