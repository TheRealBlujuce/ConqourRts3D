using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    public int currentPopulation = 0;
    public int maxPopulation = 0;

    private void Start()
    {
        RecalculateMaxPopulation();
    }

    public void AddPopulation(int amount)
    {
        currentPopulation += amount;
    }

    public void RemovePopulation(int amount)
    {
        currentPopulation = Mathf.Max(0, currentPopulation - amount);
    }

    public void RecalculateMaxPopulation()
    {
        maxPopulation = 0;
        BuildingStats[] buildings = FindObjectsByType<BuildingStats>(FindObjectsSortMode.None);

        foreach (var building in buildings)
        {
            if (building.providesPopulation)
            {
                maxPopulation += building.populationProvided;
            }
        }
    }

	public void RecalculateCurrentPopulation()
    {
        currentPopulation = 0;
        UnitStats[] units = FindObjectsByType<UnitStats>(FindObjectsSortMode.None);

        foreach (var unit in units)
        {
            if (unit.isPlayerUnit) // check that it belongs to the player
            {
                currentPopulation += unit.populationCost;
            }
        }
    }
}
