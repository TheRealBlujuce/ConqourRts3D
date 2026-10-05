using UnityEngine;

public enum BuildingType
{
    Base,
    House,
    Storehouse,
    Barracks,
    Altar,
    Tower
}

public class BuildingStats : MonoBehaviour, IDamageable
{
    [Header("Building Type")]
    public BuildingType buildingType;

    [Header("Team")]
    public CombatTeam combatTeam = CombatTeam.Player;

    [Header("Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int armor = 2;
	public int populationProvided = 16;
	public bool providesPopulation;
    public float noResourceRadius = 20f;
    public float resourceRadius = 40f;

	public bool isPlayerBase;

    [Header("Tree Clearing")]
    public float clearRadius = 32f;

   // IDamageable
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int Armor => armor;
    public bool IsDead => currentHealth <= 0;
    public CombatTeam Team => combatTeam;
    private PopulationManager populationManager;
    
    private void Start()
    {
        currentHealth = maxHealth;

        MinimapManager.Instance?.Register(
            transform,
            combatTeam,
            true
        );

        ClearNearbyTrees();

		populationManager = FindFirstObjectByType<PopulationManager>();
		populationManager.RecalculateMaxPopulation();

    }

    private void OnDestroy()
    {
        MinimapManager.Instance?.Unregister(transform);
    }

    public void TakeDamage(int incomingDamage)
    {
        if (IsDead)
            return;

        // Armor directly reduces incoming damage.
        // Armor 1 = -1 damage
        // Armor 2 = -2 damage
        // Armor 3 = -3 damage
        int finalDamage = Mathf.Max(1, incomingDamage - armor);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(
            $"{gameObject.name} took {finalDamage} damage " +
            $"({incomingDamage} incoming, {armor} armor). " +
            $"currentHealth: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (providesPopulation)
        {
            GameManager.Instance.GetPopulationManager().RecalculateMaxPopulation();
        }

        if (buildingType == BuildingType.Tower)
        {
            TowerManager.Instance?.UnregisterTower();
        }
        
        Destroy(gameObject);
    }

    private void ClearNearbyTrees()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, clearRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Tree"))
            {
                Destroy(col.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, clearRadius);

        // Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(transform.position, noResourceRadius);

        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, resourceRadius);
    }
}
