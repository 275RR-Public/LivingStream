// using UnityEngine;

// public class FishMovement : MonoBehaviour
// {
//     public float speed = 2f;  // Adjust speed as needed
//     private int directionX = 1;  // 1 = right, -1 = left
//     private int directionZ = 1;  // 1 = forward, -1 = backward
//     public float minX = -15f;  // Left boundary of the water surface (adjust as needed)
//     public float maxX = 15f;   // Right boundary of the water surface (adjust as needed)
//     public float minZ = -15f;  // Front boundary (adjust as needed)
//     public float maxZ = 15f;   // Back boundary (adjust as needed)

//     void Start()
//     {
//         // Initialize at a random position within the water surface boundaries
//         float randomX = Random.Range(minX, maxX);
//         float randomZ = Random.Range(minZ, maxZ);
//         transform.position = new Vector3(randomX, transform.position.y, randomZ);
//     }

//     void Update()
//     {
//         // Move fish along both X and Z axes
//         transform.position += new Vector3(directionX * speed * Time.deltaTime, 0, directionZ * speed * Time.deltaTime);

//         // Check if the fish hits the X boundaries
//         if (transform.position.x <= minX || transform.position.x >= maxX)
//         {
//             directionX *= -1;  // Flip direction on the X-axis

//             // Rotate the fish by 180 degrees to flip direction on X-axis
//             if (directionX == 1)  // Fish moving right
//             {
//                 transform.rotation = Quaternion.Euler(0, 0, 0); // Reset to original facing
//             }
//             else  // Fish moving left
//             {
//                 transform.rotation = Quaternion.Euler(0, 180, 0); // Flip to face left
//             }
//         }

//         // Check if the fish hits the Z boundaries
//         if (transform.position.z <= minZ || transform.position.z >= maxZ)
//         {
//             directionZ *= -1;  // Flip direction on the Z-axis
//         }
//     }
// }