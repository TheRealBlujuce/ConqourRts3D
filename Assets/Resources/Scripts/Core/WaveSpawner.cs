using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnEntry
    {
        [Header("Enemy")]
        public GameObject enemyPrefab;

        [Header("Availability")]
        [Min(1)]
        public int minimumWave = 1;

        [Header("Spawn Weight")]
        [Min(0)]
        public int spawnWeight = 1;
    }

    [Header("Wave Settings")]
    [SerializeField] private int currentWave = 0;
    [SerializeField] private int unitsPerWaveMultiplier = 3;

    [Tooltip("Delay between individual enemy spawns.")]
    [SerializeField] private float spawnDelay = 0.15f;

    [Tooltip("How often we check whether all enemies are dead.")]
    [SerializeField] private float enemyCheckInterval = 0.5f;

    [Header("Spawn Bounds")]
    [Tooltip("Terrain defining the playable map bounds.")]
    [SerializeField] private Terrain mapTerrain;

    [Tooltip("How far inside the terrain edge enemies should spawn.")]
    [SerializeField] private float edgePadding = 1f;

    [Header("Enemy Units")]
    [SerializeField] private List<EnemySpawnEntry> enemyTypes = new();

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Victory")]
    [SerializeField] private int waveGoal = 10;
    [SerializeField] private string playerBaseTag = "PlayerBase";

private bool gameWon = false;

    private bool waveActive;
    private bool spawningWave;

    private void Start()
    {
        UpdateWaveUI();
    }

    private void Update()
    {
        // Temporary wave-start input.
        // Shift + W
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W))
        {
            TryStartNextWave();
        }
    }

    public bool TryStartNextWave()
    {
        if (waveActive || spawningWave)
        {
            return false;
        }

        if (AreEnemiesAlive())
        {
            return false;
        }

        StartCoroutine(StartWave());
        return true;
    }

    private IEnumerator StartWave()
    {
        spawningWave = true;

        currentWave++;

        UpdateWaveUI();

        int enemyCount = unitsPerWaveMultiplier * currentWave;

        Debug.Log(
            $"Starting Wave {currentWave}. " +
            $"Spawning {enemyCount} enemies."
        );

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy();

            if (spawnDelay > 0f)
            {
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        spawningWave = false;
        waveActive = true;

        StartCoroutine(WaitForWaveCompletion());
    }


    private void SpawnEnemy()
    {
        GameObject prefab = GetEnemyPrefab();

        if (prefab == null)
        {
            Debug.LogWarning(
                $"WaveSpawner could not find an enemy prefab " +
                $"available for Wave {currentWave}."
            );

            return;
        }

        Vector3 spawnPosition = GetRandomEdgePosition();

        Instantiate(prefab, spawnPosition,Quaternion.identity);
    }

    private GameObject GetEnemyPrefab()
    {
        List<EnemySpawnEntry> availableEnemies = new();

        int totalWeight = 0;

        foreach (EnemySpawnEntry enemy in enemyTypes)
        {
            if (enemy.enemyPrefab == null)
                continue;

            if (currentWave < enemy.minimumWave)
                continue;

            if (enemy.spawnWeight <= 0)
                continue;

            availableEnemies.Add(enemy);
            totalWeight += enemy.spawnWeight;
        }

        if (availableEnemies.Count == 0)
            return null;


        int randomWeight = Random.Range(0, totalWeight);

        int currentWeight = 0;

        foreach (EnemySpawnEntry enemy in availableEnemies)
        {
            currentWeight += enemy.spawnWeight;

            if (randomWeight < currentWeight)
            {
                return enemy.enemyPrefab;
            }
        }

        return availableEnemies[0].enemyPrefab;
    }

    private Vector3 GetRandomEdgePosition()
    {
        if (mapTerrain == null)
        {
            Debug.LogError("WaveSpawner has no Terrain assigned.");
            return transform.position;
        }

        TerrainData terrainData = mapTerrain.terrainData;
        Vector3 terrainPosition = mapTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        float minX = terrainPosition.x + edgePadding;
        float maxX = terrainPosition.x + terrainSize.x - edgePadding;

        float minZ = terrainPosition.z + edgePadding;
        float maxZ = terrainPosition.z + terrainSize.z - edgePadding;

        // Pick one of the four terrain edges.
        int edge = Random.Range(0, 4);

        float x;
        float z;

        switch (edge)
        {
            // North
            case 0:
                x = Random.Range(minX, maxX);
                z = maxZ;
                break;

            // South
            case 1:
                x = Random.Range(minX, maxX);
                z = minZ;
                break;

            // East
            case 2:
                x = maxX;
                z = Random.Range(minZ, maxZ);
                break;

            // West
            default:
                x = minX;
                z = Random.Range(minZ, maxZ);
                break;
        }

        // Get the actual terrain height at this X/Z position.
        float y = mapTerrain.SampleHeight(new Vector3(x, 0f, z))
                + terrainPosition.y;

        return new Vector3(x, y, z);
    }

    private IEnumerator WaitForWaveCompletion()
    {
        // Give spawned enemies a frame to initialize.
        yield return null;

        while (AreEnemiesAlive())
        {
            yield return new WaitForSeconds(enemyCheckInterval);
        }

        CompleteWave();
    }

    private bool AreEnemiesAlive()
    {
        UnitStats[] units = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);

        foreach (UnitStats unit in units)
        {
            if (unit == null)
                continue;

            // CHANGE THIS LINE if your UnitStats team variable
            // has a different name/type.
            if (unit.combatTeam == CombatTeam.Enemy)
            {
                return true;
            }
        }

        return false;
    }

    private void CheckForVictory()
    {
        if (gameWon)
            return;

        if (currentWave < waveGoal)
            return;

        if (AreEnemiesAlive())
            return;

        GameObject playerBase = GameObject.FindGameObjectWithTag(playerBaseTag);

        if (playerBase == null)
            return;

        gameWon = true;

        Debug.Log($"Victory! Player survived {currentWave} waves.");

        // Hook victory UI / GameManager into this later.
    }

    private void CompleteWave()
    {
        waveActive = false;

        Debug.Log($"Wave {currentWave} complete!");

        CheckForVictory();
    }

    private void UpdateWaveUI()
    {
        if (waveText == null)
            return;

        waveText.text = $"Wave {currentWave}";
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    public bool IsWaveActive()
    {
        return waveActive || spawningWave;
    }

    public Terrain MapTerrain
    {
        get => mapTerrain;
        set => mapTerrain = value;
    }

    public bool AreEnemiesRemaining()
    {
        return AreEnemiesAlive();
    }

    public List<EnemySpawnEntry> GetEnemyTypes() => enemyTypes;
}