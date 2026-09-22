using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map Settings")]

    public float baseCornerBuffer = 32f;
    public float resourceBuffer = 32f;
    public float minDistanceFromBase = 64f;
    public float maxDistanceFromBase = 150f;
    public float minResourceDistanceFromBase = 64f;
    public float minDistanceBetweenObjects = 3f;
    public int startingWorkerCount = 8;

    [Header("Prefabs")]
    private GameObject basePrefab;
    private GameObject workerPrefab;
    public GameObject treePrefab;
    public GameObject goldPrefab;

    [Header("Resource Counts")]
    public int totalGold = 20;
    public int totalBerries = 30;

    [Header("Starting Resources Near Base")]
    public int startingGoldNearBase = 3;
    public int startingBerriesNearBase = 4;

    // Editable directly in the Inspector.
    // Starting resources will NEVER spawn closer than this distance
    // to the player's base.
    public float startingResourceMinDistanceFromBase = 32f;

    // Maximum distance at which starting resources can spawn.
    public float startingResourceMaxDistanceFromBase = 80f;

    [Header("References")]
    public Terrain terrain;
    private GameManager gameManager;

    private Vector3 basePosition;
    private Vector3 terrainSize;
    private Vector3 terrainPosition;

    // Use a LayerMask to specify which layers to check for collisions
    public LayerMask spawnLayerMask;

    private NavUpdater navUpdater;

    public void GenerateMap()
    {
        gameManager = GameManager.Instance;

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
            if (GameManager.Instance != null)
            {
                navUpdater = GameManager.Instance.GetNavUpdater();
            }
            else
            {
                Debug.LogError("GameManager instance not found. NavMesh will not be updated.");
            }
        }
        
        GetFactionPrefabs();
        
        // 1. Spawn the base in the CENTER of the map
        SpawnBase();

        // 2. Spawn workers around the base
        SpawnWorkers();

        // 3. Spawn starting resources around the base
        SpawnResourcesAroundBase(goldPrefab, startingGoldNearBase);

        // 4. Spawn extra random resources
        SpawnRandomResources(goldPrefab, totalGold);

        if (navUpdater != null)
        {
            navUpdater.UpdateNavMesh();
        }
    }

    private void SpawnBase()
    {
        
        // Calculate the exact center of the terrain.
        Vector3 centerPosition = terrainPosition + new Vector3(
            terrainSize.x * 0.5f,
            0f,
            terrainSize.z * 0.5f
        );

        // Snap the base to the terrain height.
        centerPosition.y = terrain.SampleHeight(centerPosition) + terrainPosition.y;

        basePosition = centerPosition;

        Quaternion baseRotation = Quaternion.Euler(0f, 128f, 0f);

        GameObject playerBase = Instantiate(
            basePrefab,
            basePosition,
            baseRotation
        );

        playerBase.layer = LayerMask.NameToLayer("Building");
    }

    private void SpawnWorkers()
    {
        
        float spacing = 2f;
        Vector3 rowDirection = Vector3.right;

        Vector3 startPos =
            basePosition -
            (Vector3.forward * 16f) -
            (rowDirection *
             ((startingWorkerCount - 1) * spacing / 2));

        for (int i = 0; i < startingWorkerCount; i++)
        {
            Vector3 pos =
                startPos +
                (rowDirection * (i * spacing));

            pos.y =
                terrain.SampleHeight(pos) +
                terrainPosition.y;

            GameObject worker = Instantiate(
                workerPrefab,
                pos,
                Quaternion.identity
            );

            worker.layer = LayerMask.NameToLayer("Player");

            worker.GetComponent<UnitStats>().isPlayerUnit = true;
        }
    }

    private void GetFactionPrefabs()
    {
        Race currentPlayerRace = gameManager.playerRace;

        var factionSet = gameManager.GetFactionSet();

        if (factionSet == null)
        {
            Debug.LogError(
                $"GameManager returned no FactionSet for {currentPlayerRace}!"
            );

            return;
        }

        switch (currentPlayerRace)
        {
            case Race.Human:
                basePrefab = factionSet.playerBase_Human;
                workerPrefab = factionSet.playerWorker_Human;
                break;

            case Race.Orc:
                basePrefab = factionSet.playerBase_Orc;
                workerPrefab = factionSet.playerWorker_Orc;
                break;

            case Race.Elf:
                basePrefab = factionSet.playerBase_Elf;
                workerPrefab = factionSet.playerWorker_Elf;
                break;

            case Race.Undead:
                basePrefab = factionSet.playerBase_Undead;
                workerPrefab = factionSet.playerWorker_Undead;
                break;

            default:
                Debug.LogError(
                    $"Unsupported player race: {currentPlayerRace}"
                );
                break;
        }

        Debug.Log(
            $"Map faction: {currentPlayerRace} | " +
            $"Base: {(basePrefab != null ? basePrefab.name : "NULL")} | " +
            $"Worker: {(workerPrefab != null ? workerPrefab.name : "NULL")}"
        );
    }

 	private void SpawnResourcesAroundBase(GameObject prefab, int count)
	{
		int spawnedCount = 0;
		int safetyCounter = 0;

		float minDistance = Mathf.Max(
			0f,
			startingResourceMinDistanceFromBase
		);

		float maxDistance = Mathf.Max(
			minDistance,
			startingResourceMaxDistanceFromBase
		);

		while (spawnedCount < count && safetyCounter < count * 10000)
		{
			safetyCounter++;

			// Pick a random direction on the X/Z plane.
			Vector2 randomDirection = Random.insideUnitCircle.normalized;

			if (randomDirection == Vector2.zero)
				continue;

			// Pick a random distance between the minimum and maximum.
			float distance = Random.Range(minDistance, maxDistance);

			Vector3 pos = basePosition + new Vector3(
				randomDirection.x * distance,
				0f,
				randomDirection.y * distance
			);

			// ---------------------------------------------------------
			// DO NOT CLAMP THE POSITION.
			//
			// If the candidate is outside the terrain, reject it and
			// try another random position.
			// ---------------------------------------------------------

			if (!IsInsideTerrain(pos))
				continue;

			// Apply terrain height AFTER determining X/Z position.
			pos.y = terrain.SampleHeight(pos) + terrainPosition.y;

			// Measure distance horizontally only.
			float horizontalDistance = Vector2.Distance(
				new Vector2(pos.x, pos.z),
				new Vector2(basePosition.x, basePosition.z)
			);

			// Must be inside the exact starting-resource distance range.
			bool farEnoughFromBase =
				horizontalDistance >= startingResourceMinDistanceFromBase;

			bool closeEnoughToBase =
				horizontalDistance <= startingResourceMaxDistanceFromBase;

			if (!farEnoughFromBase || !closeEnoughToBase)
				continue;

			// Make sure this location isn't occupied by another
			// resource/building/etc. included in spawnLayerMask.
			bool notColliding = !Physics.CheckSphere(
				pos,
				minDistanceBetweenObjects,
				spawnLayerMask
			);

			if (!notColliding)
				continue;

			// Everything passed, so actually spawn the resource.
			GameObject resource = Instantiate(
				prefab,
				pos,
				Quaternion.identity
			);

			resource.layer = LayerMask.NameToLayer("Resource");

			spawnedCount++;
		}

		if (spawnedCount < count)
		{
			Debug.LogWarning(
				$"Only spawned {spawnedCount}/{count} of {prefab.name} " +
				$"after {safetyCounter} attempts. " +
				$"Starting resource range: " +
				$"{startingResourceMinDistanceFromBase} - " +
				$"{startingResourceMaxDistanceFromBase} units."
			);
		}
	}

    private void SpawnRandomResources(GameObject prefab, int count)
    {
        float minX = terrain.GetPosition().x + resourceBuffer;
        float maxX = terrain.GetPosition().x +
                     terrain.terrainData.size.x -
                     resourceBuffer;

        float minZ = terrain.GetPosition().z + resourceBuffer;
        float maxZ = terrain.GetPosition().z +
                     terrain.terrainData.size.z -
                     resourceBuffer;

        for (int i = 0; i < count; i++)
        {
            bool spawned = false;

            for (int attempts = 0; attempts < 200; attempts++)
            {
                float randX = Random.Range(minX, maxX);
                float randZ = Random.Range(minZ, maxZ);

                Vector3 pos = new Vector3(
                    randX,
                    0f,
                    randZ
                );

                pos.y = terrain.SampleHeight(pos) +
                        terrain.GetPosition().y;

                bool farEnoughFromBase =
                    Vector3.Distance(pos, basePosition) >=
                    minDistanceFromBase;

                bool notColliding =
                    !Physics.CheckSphere(
                        pos,
                        minDistanceBetweenObjects,
                        spawnLayerMask
                    );

                bool canPlace =
                    farEnoughFromBase &&
                    notColliding;

                // If all attempts fail, force place.
                if (!canPlace && attempts > 150)
                {
                    canPlace = true;
                }

                if (canPlace)
                {
                    GameObject resource = Instantiate(
                        prefab,
                        pos,
                        Quaternion.identity
                    );

                    resource.layer = LayerMask.NameToLayer("Resource");

                    spawned = true;
                    break;
                }
            }

            if (!spawned)
            {
                // Last resort fallback.
                Vector3 fallbackPos =
                    basePosition +
                    new Vector3(
                        Random.Range(-5f, 5f),
                        0f,
                        Random.Range(-5f, 5f)
                    );

                fallbackPos.y =
                    terrain.SampleHeight(fallbackPos) +
                    terrainPosition.y;

                GameObject resource = Instantiate(
                    prefab,
                    fallbackPos,
                    Quaternion.identity
                );

                resource.layer = LayerMask.NameToLayer("Resource");
            }
        }
    }

    public bool IsInsideTerrain(Vector3 pos)
    {
        return
            pos.x >= terrainPosition.x &&
            pos.z >= terrainPosition.z &&
            pos.x <= terrainPosition.x + terrainSize.x &&
            pos.z <= terrainPosition.z + terrainSize.z;
    }
}