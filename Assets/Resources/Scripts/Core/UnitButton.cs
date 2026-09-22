using UnityEngine;
using UnityEngine.UI;

public class UnitButton : MonoBehaviour
{
    public enum UnitButtonType
    {
        Worker,
        BasicMelee,
        BasicRanged,
        BasicSupport,
        Hero
    }

    [Header("Button Identity")]
    public UnitButtonType unitType;

    [Header("Production")]
    public UnitProductionManager spawner;
    public int unitIndex;

    private Button button;
    private ResourceManager resourceManager;
    private PopulationManager populationManager;


    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        resourceManager = ResourceManager.Instance;

        populationManager = FindFirstObjectByType<PopulationManager>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }

    private void Update()
    {
        if (button == null)
            return;

        if (spawner == null ||resourceManager == null ||populationManager == null)
        {
            button.interactable = false;
            return;
        }

        if (unitIndex < 0 ||unitIndex >= spawner.availableUnits.Count)
        {
            button.interactable = false;
            return;
        }

        UnitProductionManager.UnitData unit =spawner.availableUnits[unitIndex];

        bool hasResources = resourceManager.HasResources(unit.goldCost,unit.lumberCost,unit.foodCost);

        bool hasPopulation = populationManager.currentPopulation + unit.populationCost <= populationManager.maxPopulation;

        bool queueNotFull = spawner.QueueCount < spawner.maxQueueSize;

        button.interactable = hasResources && hasPopulation && queueNotFull;
    }

    private void OnButtonClick()
    {
        if (spawner == null)
            return;

        spawner.QueueUnit(unitIndex);
    }

    public void SetSpawner(UnitProductionManager newSpawner, int newUnitIndex)
    {
        spawner = newSpawner;
        unitIndex = newUnitIndex;
    }
}