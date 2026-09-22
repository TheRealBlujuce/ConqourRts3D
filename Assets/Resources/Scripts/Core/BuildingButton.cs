using UnityEngine;

public class BuildingButton : MonoBehaviour
{
    public enum BuildingButtonType
    {
        House,
        Storehouse,
        Barracks,
        Altar,
        Tower
    }

    [Header("Button Identity")]
    public BuildingButtonType buildingType;

    [Header("Building")]
    public GameObject buildingPlacerPrefab;

    public void SetPlacerPrefab(GameObject prefab)
    {
        buildingPlacerPrefab = prefab;
    }

    public void OnBuildButtonPressed()
    {
        if (buildingPlacerPrefab == null)
            return;

        BuildingPlacer placer =
            buildingPlacerPrefab.GetComponent<BuildingPlacer>();

        if (placer == null)
        {
            Debug.LogError(
                $"{buildingPlacerPrefab.name} has no BuildingPlacer component!"
            );

            return;
        }

        if (!ResourceManager.Instance.HasResources(
            placer.goldCost,
            placer.lumberCost,
            placer.foodCost))
        {
            Debug.Log("Not enough resources to build!");
            return;
        }

        Instantiate(
            buildingPlacerPrefab,
            Vector3.zero,
            Quaternion.identity
        );
    }

}