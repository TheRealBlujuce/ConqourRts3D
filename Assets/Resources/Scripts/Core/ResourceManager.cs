using UnityEngine;
using TMPro;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Starting Resources")]
    public int startingGold = 0;
    public int startingLumber = 0;
    public int startingFood = 0;

    [Header("Current Resources (Read Only)")]
    [SerializeField] private int gold;
    [SerializeField] private int lumber;
    [SerializeField] private int food;

    [Header("UI References")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI lumberText;
    public TextMeshProUGUI foodText;
	public TextMeshProUGUI populationText;
    private PopulationManager popManager;
	
	private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        InitializeResources();
    }

    private void InitializeResources()
    {
        gold = startingGold;
        lumber = startingLumber;
        food = startingFood;

        UpdateResourceUI();
    }

    public void AddResource(ResourceNode.ResourceType type, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Tried to add negative amount of {type}. Use SpendResource instead.");
            return;
        }

        switch (type)
        {
            case ResourceNode.ResourceType.Gold: gold += amount; break;
            case ResourceNode.ResourceType.Lumber: lumber += amount; break;
            case ResourceNode.ResourceType.Food: food += amount; break;
        }

        UpdateResourceUI();
    }

    public bool SpendResource(ResourceNode.ResourceType type, int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning($"Tried to spend negative amount of {type}. Use AddResource instead.");
            return false;
        }

        switch (type)
        {
            case ResourceNode.ResourceType.Gold:
                if (gold < amount) return false;
                gold -= amount;
                break;
            case ResourceNode.ResourceType.Lumber:
                if (lumber < amount) return false;
                lumber -= amount;
                break;
            case ResourceNode.ResourceType.Food:
                if (food < amount) return false;
                food -= amount;
                break;
        }

        UpdateResourceUI();
        return true;
    }

    public int GetResourceAmount(ResourceNode.ResourceType type)
    {
        return type switch
        {
            ResourceNode.ResourceType.Gold => gold,
            ResourceNode.ResourceType.Lumber => lumber,
            ResourceNode.ResourceType.Food => food,
            _ => 0
        };
    }

    public bool HasResources(int goldCost, int lumberCost, int foodCost)
    {
        return gold >= goldCost && lumber >= lumberCost && food >= foodCost;
    }

    public void SpendResources(int goldCost, int lumberCost, int foodCost)
    {
        gold -= goldCost;
        lumber -= lumberCost;
        food -= foodCost;

        UpdateResourceUI();
    }

    private void UpdateResourceUI()
    {
        if (goldText != null) goldText.text = $"Gold x {gold.ToString("D3")}";
        if (lumberText != null) lumberText.text = $"Lumber x {lumber.ToString("D3")}";
        if (foodText != null) foodText.text = $"Food x {food.ToString("D3")}";
        if (populationText != null && popManager != null) 
		{ 
			populationText.text = $"Pop x " + $"{popManager.currentPopulation.ToString("D3")} / {popManager.maxPopulation.ToString("D3")}";
		}
			
	}

	public void ResetResources()
	{
		gold = startingGold;
		food = startingFood;
		lumber = startingLumber;
		
		UpdateResourceUI() ;
	}
	public void SetPopManager(PopulationManager popManager) => this.popManager = popManager;

}
