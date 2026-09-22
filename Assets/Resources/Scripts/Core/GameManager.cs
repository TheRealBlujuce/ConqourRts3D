using System.Collections;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Race { Orc, Human, Undead, Elf }
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
	public Race playerRace = Race.Orc; // Default player race
	public Race enemyRace = Race.Human;

	// These are for the FactionSet Class used to quickly debug the 4 races, ensuring models and whatnot are correct between them all.
	[SerializeField] private FactionSet.Faction currentFaction = FactionSet.Faction.Orc;
	[SerializeField] private FactionSet.Faction currentEnemyFaction = FactionSet.Faction.Human;
	[SerializeField] private FactionSet factionSwapper;

	[Header("Dependencies")]
    [SerializeField] private ResourceManager resourceManager; // Reference to the ResourceManager
	[SerializeField] private MapGenerator mapGenerator;
	[SerializeField] private NavUpdater navUpdater;
	[SerializeField] private SelectionBox selectionBox;
	[SerializeField] private CameraController cameraController;
	[SerializeField] private InstancedTreeManager treeManager;
	[SerializeField] private PopulationManager populationManager;


	[SerializeField] private TextMeshProUGUI currentFoodText;
	[SerializeField] private TextMeshProUGUI currentLumberText;
	[SerializeField] private TextMeshProUGUI currentGoldText;
	[SerializeField] private TextMeshProUGUI currentPopulationText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

			// Subscribe to scene loaded event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

		StartCoroutine(GetDependenciesAndGenerateGame());

    }

	private IEnumerator GetDependenciesAndGenerateGame()
	{
		navUpdater = GetComponent<NavUpdater>();
		navUpdater.navSurface = FindFirstObjectByType<NavMeshSurface>();

		treeManager = GetComponent<InstancedTreeManager>();

		mapGenerator = GetComponent<MapGenerator>();
		
		treeManager.RegenerateTrees();
		mapGenerator.GenerateMap();

		navUpdater.UpdateNavMesh();

		selectionBox = GetComponent<SelectionBox>();
		selectionBox.SetCamera(Camera.main);

		cameraController = FindFirstObjectByType<CameraController>();
		BoxCollider[] bounds = FindObjectsByType<BoxCollider>(FindObjectsSortMode.None);
		
		foreach (BoxCollider collider in bounds)
		{
			if (collider.CompareTag("Bounds") == true)
			{
				cameraController.cameraBounds = collider;
			}
		}

		resourceManager = FindFirstObjectByType<ResourceManager>();
		populationManager = FindFirstObjectByType<PopulationManager>();
		
		resourceManager.SetPopManager(populationManager);
		resourceManager.ResetResources();
		populationManager.RecalculateCurrentPopulation();

		factionSwapper = GetComponent<FactionSet>();

		InitializeGameDependencies();

		yield return null;

	}

    public void InitializeGameDependencies()
    {
        //Debug.Log("Initializing Game Dependencies...");

        // Ensure ResourceManager is assigned. If not, try to find it in the scene.
        if (resourceManager == null)
        {
            resourceManager = FindFirstObjectByType<ResourceManager>();
        }

		if (populationManager == null)
        {
            populationManager = FindFirstObjectByType<PopulationManager>();
        }


		currentFoodText = GameObject.Find("FoodText").GetComponent<TextMeshProUGUI>();
		currentGoldText = GameObject.Find("GoldText").GetComponent<TextMeshProUGUI>();
		currentLumberText = GameObject.Find("LumberText").GetComponent<TextMeshProUGUI>();
		currentPopulationText = GameObject.Find("PopulationText").GetComponent<TextMeshProUGUI>();

		resourceManager.foodText = currentFoodText;
		resourceManager.lumberText = currentLumberText;
		resourceManager.goldText = currentGoldText;
		resourceManager.populationText = currentPopulationText;

    }

    public ResourceManager GetResourceManager()
    {
        return resourceManager;
	}

    public void SetPlayerRace(Race newRace)
    {
        playerRace = newRace;
        //Debug.Log($"Player race set to: {playerRace}");
    }

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.RightShift) && Input.GetKeyDown(KeyCode.R))
		{
			// Get the currently active scene and reload it
			Scene currentScene = SceneManager.GetActiveScene();
			SceneManager.LoadScene(currentScene.name);
		}
		
		if (Input.GetKeyDown(KeyCode.N))
		{
			// Get the currently active scene and reload it
			navUpdater.UpdateNavMesh();
		}

		if (Input.GetKeyDown(KeyCode.F))
		{
			// Move to the next faction
			currentFaction++;

			// If we've gone past the final faction,
			// loop back to the first faction
			if ((int)currentFaction >= System.Enum.GetValues(
					typeof(FactionSet.Faction)).Length)
			{
				currentFaction = 0;
			}

			// Tell the faction swapper which faction we're changing to
			factionSwapper.selectedFaction = currentFaction;

			Debug.Log($"Swapping player faction to: {currentFaction}");

			factionSwapper.SwapModels();
		}
	}

	public NavUpdater GetNavUpdater() => navUpdater;
	public PopulationManager GetPopulationManager() => populationManager;

	public FactionSet GetFactionSet() => factionSwapper;
}