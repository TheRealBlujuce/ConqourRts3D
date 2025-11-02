using UnityEngine;

public class BuildingButton : MonoBehaviour
{
    public GameObject buildingPlacerPrefab;
    public int goldCost;
    public int lumberCost;
    public int foodCost;

    private ResourceManager resourceManager;

    private void Start()
    {
        resourceManager = FindFirstObjectByType<ResourceManager>();
    }

    public void OnBuildButtonPressed()
    {
        if (resourceManager == null)
        {
            Debug.LogError("No ResourceManager found in scene!");
            return;
        }

        if (resourceManager.HasResources(goldCost, lumberCost, foodCost))
        {
            // Deduct resources
			resourceManager.SpendResources(goldCost, lumberCost, foodCost);

            // Spawn the building placer
            Instantiate(buildingPlacerPrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            Debug.Log("Not enough resources to build!");
        }
    }
}
