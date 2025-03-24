using UnityEngine;

public class FishMovement : MonoBehaviour
{
    public float speed = 2f;  // Movement speed
    private int directionX = 1;  
    private int directionZ = 1;  
    public float minX = -15f;
    public float maxX = 15f;   
    public float minZ = -15f;  
    public float maxZ = 15f;   

    public float swayAmplitude = 0.5f;  // How much the body sways (left/right)
    public float swayFrequency = 2f;  // How fast the fish sways

    private float timeOffset; 

    void Start()
    {
        // Set initial random position within boundaries
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        transform.position = new Vector3(randomX, transform.position.y, randomZ);

        // Random time offset to prevent synchronized movement
        timeOffset = Random.Range(0f, Mathf.PI * 2);
    }

    void Update()
    {
        // Move forward
        transform.position += new Vector3(directionX * speed * Time.deltaTime, 0, directionZ * speed * Time.deltaTime);

        // **🐍 Side-to-Side Swaying Movement**
        float swayOffset = Mathf.Sin(Time.time * swayFrequency + timeOffset) * swayAmplitude;
        transform.position += transform.right * swayOffset * Time.deltaTime;  // Move slightly left/right

        // **🐟 Adjust Rotation Slightly to Match Sway**
        float swayAngle = Mathf.Sin(Time.time * swayFrequency + timeOffset) * 10f; // Rotation for realism
        transform.rotation = Quaternion.Euler(0, (directionX == 1 ? 0 : 180) + swayAngle, 0);

        // **Boundary Check**
        if (transform.position.x <= minX || transform.position.x >= maxX)
        {
            directionX *= -1;  // Flip X direction
        }

        if (transform.position.z <= minZ || transform.position.z >= maxZ)
        {
            directionZ *= -1;  // Flip Z direction
        }
    }
}
