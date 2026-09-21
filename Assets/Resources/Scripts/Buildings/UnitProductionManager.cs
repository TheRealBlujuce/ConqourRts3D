using System.Collections.Generic;
using UnityEngine;

public class UnitProductionManager : MonoBehaviour
{
    [System.Serializable]
    public class UnitData
    {
        public string unitName;
        public GameObject prefab;
        public int goldCost;
        public int lumberCost;
        public int foodCost;
        public int populationCost;
        public float buildTime = 3f; // time before spawn
        public int threatAmount;
    }

    public List<UnitData> availableUnits = new List<UnitData>();
    public Transform spawnPoint; // optional (otherwise auto around building)
    public float spawnRadius = 4f;
    public int maxQueueSize = 6;
	public int QueueCount => unitQueue.Count;

    private Queue<UnitData> unitQueue = new Queue<UnitData>();
    private ResourceManager resourceManager;
    private PopulationManager populationManager;

    private float buildTimer = 0f;
    private bool isBuilding = false;

    void Start()
    {
        resourceManager = FindFirstObjectByType<ResourceManager>();
        populationManager = FindFirstObjectByType<PopulationManager>();

        if (spawnPoint == null) spawnPoint = transform;
    }

    void Update()
    {
        if (isBuilding && unitQueue.Count > 0)
        {
            buildTimer -= Time.deltaTime;
            if (buildTimer <= 0f)
            {
                SpawnUnit(unitQueue.Dequeue());
                if (unitQueue.Count > 0)
                {
                    buildTimer = unitQueue.Peek().buildTime;
                }
                else
                {
                    isBuilding = false;
                }
            }
        }
    }

    public bool QueueUnit(int index)
    {
        if (index < 0 || index >= availableUnits.Count) return false;

        UnitData unit = availableUnits[index];
        
        // Check population
        if (populationManager.currentPopulation + unit.populationCost > populationManager.maxPopulation)
        {
            Debug.Log("Not enough population room!");
            return false;
        }

        // Check resources
        if (!resourceManager.HasResources(unit.goldCost, unit.lumberCost, unit.foodCost))
        {
            Debug.Log("Not enough resources!");
            return false;
        }

        // Check queue size
        if (unitQueue.Count >= maxQueueSize)
        {
            Debug.Log("Queue is full!");
            return false;
        }

        // Deduct resources and add to queue
        resourceManager.SpendResources(unit.goldCost, unit.lumberCost, unit.foodCost);
        unitQueue.Enqueue(unit);

        if (!isBuilding)
        {
            buildTimer = unit.buildTime;
            isBuilding = true;
        }
        
        populationManager.AddPopulation(unit.populationCost);
        
        return true;
    }

    private void SpawnUnit(UnitData unit)
    {
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 0f;

        Vector3 spawnPos = spawnPoint.position + randomOffset;
        GameObject newUnit = Instantiate(unit.prefab, spawnPos, Quaternion.identity);
		newUnit.GetComponent<UnitStats>().populationCost = unit.populationCost;
		newUnit.GetComponent<UnitStats>().isPlayerUnit = true;

        // populationManager.AddPopulation(unit.populationCost);
        ThreatManager.Instance.AddThreat(newUnit.GetComponent<UnitStats>().threatAmount);
	}
}
