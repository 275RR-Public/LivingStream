using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class QuitButtonHandler : MonoBehaviour
{
    // Public fields to assign in the Unity Inspector
    public GameObject ocean;                    // Reference to the Ocean GameObject
    public Light directionalLight;              // Reference to the Directional Light
    public GameObject quadLogo;                 // Reference to the QuadLogo GameObject
    public GameObject planeBlack;               // Reference to the PlaneBlack GameObject
    public GameObject sphereRed;                // Reference to the SphereRed GameObject (Added)
    public GameObject sphereBlue;               // Reference to the SphereBlue GameObject (Added)
    public float oceanLowerAmount = 1f;         // Amount to lower the ocean's Y-position
    public float lightIntensityChange = 0.5f;   // Amount to change the light intensity
    public float quadMoveAmount = 2f;           // Amount to move the quad downwards when washing off

    // Private fields to store original values and state
    private Vector3 originalOceanPosition;      // Stores the ocean's initial position
    private float originalLightIntensity;       // Stores the light's initial intensity
    private Vector3 originalQuadPosition;       // Stores the quad's initial position
    private Vector3 originalPlaneBlackPosition; // Stores the PlaneBlack's initial position
    private Vector3 originalCameraPosition;     // Stores the camera's initial position
    private Quaternion originalCameraRotation;  // Stores the camera's initial rotation
    private Vector3 originalSphereRedPosition;  // Stores the SphereRed's initial position (Added)
    private Vector3 originalSphereBluePosition; // Stores the SphereBlue's initial position (Added)
    private bool isTideLowered = false;         // Tracks whether the tide is lowered
    private Coroutine currentTransition;        // Reference to the current transition coroutine
    private AudioSource backgroundMusic;        // Reference to the AudioSource on Main Camera
    private float originalVolume;               // Stores the original volume of the AudioSource
    private GameObject mainCamera;              // Reference to the Main Camera

    void Start()
    {
        // Get the root visual element from the UI Document
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Store original values for ocean, light, QuadLogo, PlaneBlack, camera, spheres, and audio
        if (ocean != null) originalOceanPosition = ocean.transform.position;
        if (directionalLight != null) originalLightIntensity = directionalLight.intensity;
        if (quadLogo != null) originalQuadPosition = quadLogo.transform.position;
        if (planeBlack != null) originalPlaneBlackPosition = planeBlack.transform.position;
        if (sphereRed != null) originalSphereRedPosition = sphereRed.transform.position;
        if (sphereBlue != null) originalSphereBluePosition = sphereBlue.transform.position;
        mainCamera = GameObject.Find("Main Camera");
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;   
            originalCameraRotation = mainCamera.transform.rotation;   
            backgroundMusic = mainCamera.GetComponent<AudioSource>();
            if (backgroundMusic != null) originalVolume = backgroundMusic.volume;
        }

        // Set up the Calibrate Button click event
        var toggleTide = root.Q<Button>("TideButton");
        if (toggleTide != null)
        {
            toggleTide.clicked += () =>
            {
                // Stop any ongoing transition
                if (currentTransition != null)
                {
                    StopCoroutine(currentTransition);
                }

                if (!isTideLowered)
                {
                    // Lowering the tide: calculate target values
                    float targetOceanY = originalOceanPosition.y - oceanLowerAmount;
                    float targetLightIntensity = originalLightIntensity - lightIntensityChange;
                    float targetVolume = 0f;
                    Vector3 targetQuadPosition = originalQuadPosition - new Vector3(0, quadMoveAmount, 0);
                    float targetPlaneBlackY = 0f;
                    Vector3 targetCameraPosition = new Vector3(0, 5, 0);
                    Quaternion targetCameraRotation = Quaternion.Euler(90, 0, 0);
                    Vector3 targetSphereRedPosition = new Vector3(2.5f, 0, 0);
                    Vector3 targetSphereBluePosition = new Vector3(0, 0, 2.5f);

                    // Start transition if all components are present
                    if (ocean != null && directionalLight != null && backgroundMusic != null && 
                        quadLogo != null && planeBlack != null && mainCamera != null && 
                        sphereRed != null && sphereBlue != null) // Updated condition
                    {
                        currentTransition = StartCoroutine(TransitionCoroutine(
                            ocean.transform.position.y, targetOceanY,
                            directionalLight.intensity, targetLightIntensity,
                            backgroundMusic.volume, targetVolume,
                            quadLogo.transform.position, targetQuadPosition,
                            planeBlack.transform.position.y, targetPlaneBlackY,
                            mainCamera.transform.position, targetCameraPosition,
                            mainCamera.transform.rotation, targetCameraRotation,
                            sphereRed.transform.position, targetSphereRedPosition,  
                            sphereBlue.transform.position, targetSphereBluePosition 
                        ));
                    }

                    isTideLowered = true;
                    toggleTide.text = "Demo";
                }
                else
                {
                    // Raising the tide values
                    float targetOceanY = originalOceanPosition.y;
                    float targetLightIntensity = originalLightIntensity;
                    float targetVolume = originalVolume;
                    Vector3 targetQuadPosition = originalQuadPosition;
                    float targetPlaneBlackY = originalPlaneBlackPosition.y;
                    Vector3 targetCameraPosition = originalCameraPosition;
                    Quaternion targetCameraRotation = originalCameraRotation;
                    Vector3 targetSphereRedPosition = originalSphereRedPosition;  
                    Vector3 targetSphereBluePosition = originalSphereBluePosition;

                    // Start transition if all components are present
                    if (ocean != null && directionalLight != null && backgroundMusic != null && 
                        quadLogo != null && planeBlack != null && mainCamera != null && 
                        sphereRed != null && sphereBlue != null) // Updated condition
                    {
                        currentTransition = StartCoroutine(TransitionCoroutine(
                            ocean.transform.position.y, targetOceanY,
                            directionalLight.intensity, targetLightIntensity,
                            backgroundMusic.volume, targetVolume,
                            quadLogo.transform.position, targetQuadPosition,
                            planeBlack.transform.position.y, targetPlaneBlackY,
                            mainCamera.transform.position, targetCameraPosition,
                            mainCamera.transform.rotation, targetCameraRotation,
                            sphereRed.transform.position, targetSphereRedPosition,  
                            sphereBlue.transform.position, targetSphereBluePosition 
                        ));
                    }

                    isTideLowered = false;
                    toggleTide.text = "Calibrate";
                }
            };
        }

        var quitButton = root.Q<Button>("QuitButton");
        if (quitButton != null)
        {
            quitButton.clicked += () =>
            {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            };
        }
    }

    // Coroutine to smoothly transition all elements over 2 seconds
    private IEnumerator TransitionCoroutine(
        float startOceanY, float targetOceanY,
        float startLightIntensity, float targetLightIntensity,
        float startVolume, float targetVolume,
        Vector3 startQuadPosition, Vector3 targetQuadPosition,
        float startPlaneBlackY, float targetPlaneBlackY,
        Vector3 startCameraPosition, Vector3 targetCameraPosition,
        Quaternion startCameraRotation, Quaternion targetCameraRotation,
        Vector3 startSphereRedPosition, Vector3 targetSphereRedPosition,  
        Vector3 startSphereBluePosition, Vector3 targetSphereBluePosition 
    )
    {
        float timer = 0f;
        while (timer < 2f)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / 2f); // Interpolation factor (0 to 1)

            // Update ocean position
            if (ocean != null)
            {
                Vector3 pos = ocean.transform.position;
                pos.y = Mathf.Lerp(startOceanY, targetOceanY, t);
                ocean.transform.position = pos;
            }

            // Update light intensity
            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            }

            // Update audio volume
            if (backgroundMusic != null)
            {
                backgroundMusic.volume = Mathf.Lerp(startVolume, targetVolume, t);
            }

            // Update quad position
            if (quadLogo != null)
            {
                quadLogo.transform.position = Vector3.Lerp(startQuadPosition, targetQuadPosition, t);
            }

            // Update PlaneBlack y-position
            if (planeBlack != null)
            {
                Vector3 pos = planeBlack.transform.position;
                pos.y = Mathf.Lerp(startPlaneBlackY, targetPlaneBlackY, t);
                planeBlack.transform.position = pos;
            }

            // Update Main Camera position and rotation
            if (mainCamera != null)
            {
                mainCamera.transform.position = Vector3.Lerp(startCameraPosition, targetCameraPosition, t);
                mainCamera.transform.rotation = Quaternion.Lerp(startCameraRotation, targetCameraRotation, t);
            }

            // Update SphereRed position (Added)
            if (sphereRed != null)
            {
                sphereRed.transform.position = Vector3.Lerp(startSphereRedPosition, targetSphereRedPosition, t);
            }

            // Update SphereBlue position (Added)
            if (sphereBlue != null)
            {
                sphereBlue.transform.position = Vector3.Lerp(startSphereBluePosition, targetSphereBluePosition, t);
            }

            yield return null; // Wait for the next frame
        }

        // Set final values to ensure precision
        if (ocean != null)
        {
            Vector3 pos = ocean.transform.position;
            pos.y = targetOceanY;
            ocean.transform.position = pos;
        }
        if (directionalLight != null)
        {
            directionalLight.intensity = targetLightIntensity;
        }
        if (backgroundMusic != null)
        {
            backgroundMusic.volume = targetVolume;
        }
        if (quadLogo != null)
        {
            quadLogo.transform.position = targetQuadPosition;
        }
        if (planeBlack != null)
        {
            Vector3 pos = planeBlack.transform.position;
            pos.y = targetPlaneBlackY;
            planeBlack.transform.position = pos;
        }
        if (mainCamera != null)
        {
            mainCamera.transform.position = targetCameraPosition;
            mainCamera.transform.rotation = targetCameraRotation;
        }
        if (sphereRed != null)
        {
            sphereRed.transform.position = targetSphereRedPosition;
        }
        if (sphereBlue != null)
        {
            sphereBlue.transform.position = targetSphereBluePosition;
        }
    }
}