using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]
    
    public float baseCornerBuffer = 32f;
    public float resourceBuffer = 32f;
    public float minDistanceFromBase = 64f;
    public float minResourceDistanceFromBase = 64f;
    public float minDistanceBetweenObjects = 3f;
	public int startingWorkerCount = 8;

    [Header("Prefabs")]
    public GameObject basePrefab;
    public GameObject workerPrefab;
    public GameObject treePrefab;
    public GameObject goldPrefab;
    public GameObject berryBushPrefab;
    public GameObject goldClusterPrefab;
    public GameObject berrybushClusterPrefab;

    [Header("Resource Counts")]
    public int totalGold = 20;
    public int totalBerries = 30;

    [Header("Starting Resources Near Base")]
    public int startingGoldNearBase = 3;
    public int startingBerriesNearBase = 4;
    public float startingResourceDistanceFromBase = 80f;

	[Header("References")]
	public Terrain terrain;

    private Vector3 basePosition;
    private Vector3 terrainSize;
    private Vector3 terrainPosition;

    // Use a LayerMask to specify which layers to check for collisions
    public LayerMask spawnLayerMask;

    private NavUpdater navUpdater;

    public void GenerateMap()
    {

		// Get terrain details and then generate the map
        if (terrain == null)
        {
            Debug.LogError("Terrain not assigned. Please assign the Terrain component in the Inspector.");
            return;
        }
        
        terrainSize = terrain.terrainData.size;
        terrainPosition = terrain.transform.position;

        if (navUpdater == null)
        {
            // Assuming GameManager.Instance is correctly set up
            if (GameManager.Instance != null)
            {
                navUpdater = GameManager.Instance.GetNavUpdater();
            }
            else
            {
                Debug.LogError("GameManager instance not found. NavMesh will not be updated.");
            }
        }

        // 1. Spawning the base safely in a corner
        SpawnBase();

		// 2. Spawning workers
        SpawnWorkers();

        // 3. Spawning resources, avoiding the base area
        SpawnResourcesAroundBase(goldClusterPrefab, startingGoldNearBase);
        SpawnResourcesAroundBase(berrybushClusterPrefab, startingBerriesNearBase);

		// 4. Spawn some extra random resources
		SpawnRandomResources(goldClusterPrefab, totalGold);
		SpawnRandomResources(berrybushClusterPrefab, totalBerries);

        if (navUpdater != null)
        {
            navUpdater.UpdateNavMesh();
        }
    }

    private void SpawnBase()
    {
        Vector3[] corners = new Vector3[]
        {
            new Vector3(baseCornerBuffer, 0, baseCornerBuffer),
            new Vector3(terrainSize.x - baseCornerBuffer, 0, baseCornerBuffer),
            new Vector3(baseCornerBuffer, 0, terrainSize.z - baseCornerBuffer),
            new Vector3(terrainSize.x - baseCornerBuffer, 0, terrainSize.z - baseCornerBuffer)
        };

        Vector3 randomCorner = corners[Random.Range(0, corners.Length)];
        
        Vector3 finalBasePos = randomCorner + terrainPosition;
        finalBasePos.y = terrain.SampleHeight(finalBasePos) + terrainPosition.y;
        
        basePosition = finalBasePos;
		Quaternion baseRotation = Quaternion.Euler(0f, 128f, 0f);
        GameObject playerBase = Instantiate(basePrefab, basePosition, baseRotation);
        playerBase.layer = LayerMask.NameToLayer("Building");
    }

	private void SpawnResourcesAroundBase(GameObject prefab, int count)
	{
		int spawnedCount = 0;
		int safetyCounter = 0;

		while (spawnedCount < count && safetyCounter < count * 500)
		{
			safetyCounter++;

			// Stage 1: Try strict rules
			Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(startingResourceDistanceFromBase + 4f, startingResourceDistanceFromBase + 8f);
			Vector3 pos = basePosition + new Vector3(circle.x, 0, circle.y);

			pos.x = Mathf.Clamp(pos.x, terrainPosition.x + resourceBuffer, terrainPosition.x + terrainSize.x - resourceBuffer);
			pos.z = Mathf.Clamp(pos.z, terrainPosition.z + resourceBuffer, terrainPosition.z + terrainSize.z - resourceBuffer);
			pos.y = terrain.SampleHeight(pos) + terrainPosition.y;

			// This check ensures resources don't spawn on top of each other or the base.
			// The 'spawnLayerMask' must include layers for both resources and the base.
			bool canPlace = IsInsideTerrain(pos) && !Physics.CheckSphere(pos, minDistanceBetweenObjects, spawnLayerMask);


			if (canPlace)
			{
				GameObject resource = Instantiate(prefab, pos, Quaternion.identity);
				resource.layer = LayerMask.NameToLayer("Resource");
				spawnedCount++;
			}
		}

		if (spawnedCount < count)
			Debug.LogWarning($"Only spawned {spawnedCount}/{count} of {prefab.name} near base after {safetyCounter} tries.");
	}

	private void SpawnRandomResources(GameObject prefab, int count)
	{
		float minX = terrain.GetPosition().x + resourceBuffer;
		float maxX = terrain.GetPosition().x + terrain.terrainData.size.x - resourceBuffer;
		float minZ = terrain.GetPosition().z + resourceBuffer;
		float maxZ = terrain.GetPosition().z + terrain.terrainData.size.z - resourceBuffer;

		for (int i = 0; i < count; i++)
		{
			bool spawned = false;
			for (int attempts = 0; attempts < 200; attempts++)
			{
				float randX = Random.Range(minX, maxX);
				float randZ = Random.Range(minZ, maxZ);

				Vector3 pos = new Vector3(randX, 0, randZ);
				pos.y = terrain.SampleHeight(pos) + terrain.GetPosition().y;

				bool canPlace = Vector3.Distance(pos, basePosition) >= minDistanceFromBase &&
								!Physics.CheckSphere(pos, minDistanceBetweenObjects, spawnLayerMask);

				// If all attempts fail, force place
				if (!canPlace && attempts > 150)
					canPlace = true;

				if (canPlace)
				{
					GameObject resource = Instantiate(prefab, pos, Quaternion.identity);
					resource.layer = LayerMask.NameToLayer("Resource");
					spawned = true;
					break;
				}
			}

			if (!spawned)
			{
				// As a last resort, drop it at base center + random small offset
				Vector3 fallbackPos = basePosition + new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
				fallbackPos.y = terrain.SampleHeight(fallbackPos) + terrainPosition.y;
				GameObject resource = Instantiate(prefab, fallbackPos, Quaternion.identity);
				resource.layer = LayerMask.NameToLayer("Resource");
			}
		}
	}
    
	private void SpawnWorkers()
	{
		float spacing = 2f;
		Vector3 rowDirection = Vector3.right; // change direction if needed

		// Adjust centering math based on worker count
		Vector3 startPos = basePosition  - (Vector3.forward * 16f) - (rowDirection * ((startingWorkerCount - 1) * spacing / 2));

		for (int i = 0; i < startingWorkerCount; i++)
		{
			Vector3 pos = startPos + (rowDirection * (i * spacing));
			pos.y = terrain.SampleHeight(pos) + terrainPosition.y;

			GameObject worker = Instantiate(workerPrefab, pos, Quaternion.identity);
			worker.layer = LayerMask.NameToLayer("Player");
			worker.GetComponent<UnitStats>().isPlayerUnit = true;
		}
	}

    public bool IsInsideTerrain(Vector3 pos)
    {
        // Check against the terrain's world position
        return pos.x >= terrainPosition.x && pos.z >= terrainPosition.z && pos.x <= terrainPosition.x + terrainSize.x && pos.z <= terrainPosition.z + terrainSize.z;
    }
}