using UnityEngine;
using UnityEngine.UI;

public class UnitButton : MonoBehaviour
{
    public UnitProductionManager spawner; // The building that makes units
    public int unitIndex; // Which unit in the spawner's availableUnits list
    private Button button;

    private ResourceManager resourceManager;
    private PopulationManager populationManager;

    void Start()
    {
        button = GetComponent<Button>();
        resourceManager = ResourceManager.Instance;
        populationManager = FindFirstObjectByType<PopulationManager>();
		spawner = FindFirstObjectByType<UnitProductionManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    void Update()
    {
        if (spawner == null || resourceManager == null || populationManager == null) 
        { 
            spawner = FindFirstObjectByType<UnitProductionManager>();

            if (spawner == null) return;
        }


        UnitProductionManager.UnitData unit = spawner.availableUnits[unitIndex];

        bool hasResources = resourceManager.HasResources(unit.goldCost, unit.lumberCost, unit.foodCost);
        bool hasPopulation = populationManager.currentPopulation + unit.populationCost <= populationManager.maxPopulation;
        bool queueNotFull = spawner.QueueCount < spawner.maxQueueSize;

        // Enable button only if player can actually queue this unit
        button.interactable = hasResources && hasPopulation && queueNotFull;
    }

    private void OnButtonClick()
    {
        if (spawner != null)
        {
            spawner.QueueUnit(unitIndex);
            
        }
    }
}
