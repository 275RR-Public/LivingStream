using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class WaterInteraction : MonoBehaviour
{
    public WaterSurface waterSurface;
    public ParticleSystem splashParticles;
    public DecalProjector foamDecalProjector;

    private bool wasInWater = false;
    private WaterSearchParameters searchParams = new WaterSearchParameters();
    private WaterSearchResult searchResult = new WaterSearchResult();

    void Start()
    {
        // Find the effects components in the prefab
        splashParticles = GetComponentInChildren<ParticleSystem>();
        foamDecalProjector = GetComponentInChildren<DecalProjector>();

        if (splashParticles == null) Debug.LogError("SplashParticles not found in prefab!");
        if (foamDecalProjector == null) Debug.LogError("FoamDecalProjector not found in prefab!");
    }
    
    void Update()
    {
        if (waterSurface == null) return;

        Vector3 position = transform.position;

        // Search for water height at the cube’s x, z position
        searchParams.startPositionWS = new Vector3(position.x, 1000, position.z); // Start above water
        searchParams.targetPositionWS = position; // Target the cube’s position
        searchParams.error = 0.01f;               // Precision threshold
        searchParams.maxIterations = 8;           // Max search steps

        if (waterSurface.ProjectPointOnWaterSurface(searchParams, out searchResult))
        {
            float waterHeight = searchResult.projectedPositionWS.y;
            bool isInWater = position.y < waterHeight;

            // Trigger splash when entering water
            if (isInWater && !wasInWater && splashParticles != null)
            {
                splashParticles.transform.position = new Vector3(position.x, waterHeight, position.z);
                splashParticles.Play();
            }

            // Show foam while in water
            if (foamDecalProjector != null)
            {
                if (isInWater)
                {
                    foamDecalProjector.transform.position = new Vector3(position.x, waterHeight + 0.1f, position.z);
                    foamDecalProjector.gameObject.SetActive(true);
                }
                else
                {
                    foamDecalProjector.gameObject.SetActive(false);
                }
            }

            wasInWater = isInWater;
        }
        else
        {
            Debug.LogWarning("Failed to find water surface.");
        }
    }
}