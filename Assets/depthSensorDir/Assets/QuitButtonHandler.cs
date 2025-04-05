using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class QuitButtonHandler : MonoBehaviour
{
    // Public fields to assign in the Unity Inspector
    public GameObject ocean;                    // Reference to the Ocean GameObject
    public Light directionalLight;              // Reference to the Directional Light
    public GameObject quadLogo;                 // Reference to the QuadLogo GameObject
    public float oceanLowerAmount = 1f;         // Amount to lower the ocean's Y-position
    public float lightIntensityChange = 0.5f;   // Amount to change the light intensity
    public float quadMoveAmount = 2f;           // Amount to move the quad downwards when washing off

    // Private fields to store original values and state
    private Vector3 originalOceanPosition;      // Stores the ocean's initial position
    private float originalLightIntensity;       // Stores the light's initial intensity
    private Vector3 originalQuadPosition;       // Stores the quad's initial position
    private bool isTideLowered = false;         // Tracks whether the tide is lowered
    private Coroutine currentTransition;        // Reference to the current transition coroutine
    private AudioSource backgroundMusic;        // Reference to the AudioSource on Main Camera
    private float originalVolume;               // Stores the original volume of the AudioSource

    void Start()
    {
        // Get the root visual element from the UI Document
        var root = GetComponent<UIDocument>().rootVisualElement;

        // Store original values for ocean, light, and audio
        if (ocean != null) originalOceanPosition = ocean.transform.position;
        if (directionalLight != null) originalLightIntensity = directionalLight.intensity;
        backgroundMusic = GameObject.Find("Main Camera").GetComponent<AudioSource>();
        if (backgroundMusic != null) originalVolume = backgroundMusic.volume;

        // Store the original position of the QuadLogo
        if (quadLogo != null)
        {
            originalQuadPosition = quadLogo.transform.position;
        }
        else
        {
            Debug.LogWarning("QuadLogo is not assigned in the Inspector!");
        }

        // Set up the TideButton click event
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
                    float targetLightIntensity = originalLightIntensity + lightIntensityChange;
                    float targetVolume = 0f; // Mute audio when tide lowers
                    Vector3 targetQuadPosition = originalQuadPosition - new Vector3(0, quadMoveAmount, 0); // Move quad down

                    // Start transition if all components are present
                    if (ocean != null && directionalLight != null && backgroundMusic != null && quadLogo != null)
                    {
                        currentTransition = StartCoroutine(TransitionCoroutine(
                            ocean.transform.position.y, targetOceanY,
                            directionalLight.intensity, targetLightIntensity,
                            backgroundMusic.volume, targetVolume,
                            quadLogo.transform.position, targetQuadPosition
                        ));
                    }

                    isTideLowered = true;
                    toggleTide.text = "Raise Tide";
                }
                else
                {
                    // Raising the tide: restore original values
                    float targetOceanY = originalOceanPosition.y;
                    float targetLightIntensity = originalLightIntensity;
                    float targetVolume = originalVolume; // Restore audio volume
                    Vector3 targetQuadPosition = originalQuadPosition; // Restore quad position

                    // Start transition if all components are present
                    if (ocean != null && directionalLight != null && backgroundMusic != null && quadLogo != null)
                    {
                        currentTransition = StartCoroutine(TransitionCoroutine(
                            ocean.transform.position.y, targetOceanY,
                            directionalLight.intensity, targetLightIntensity,
                            backgroundMusic.volume, targetVolume,
                            quadLogo.transform.position, targetQuadPosition
                        ));
                    }

                    isTideLowered = false;
                    toggleTide.text = "Lower Tide";
                }
            };
        }

        // QuitButton functionality (unchanged)
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
    private IEnumerator TransitionCoroutine(float startOceanY, float targetOceanY, 
                                            float startLightIntensity, float targetLightIntensity, 
                                            float startVolume, float targetVolume, 
                                            Vector3 startQuadPosition, Vector3 targetQuadPosition)
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
    }
}