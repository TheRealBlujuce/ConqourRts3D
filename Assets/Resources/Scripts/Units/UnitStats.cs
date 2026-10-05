using UnityEngine;

public enum UnitType
{
    Worker,
    BasicMelee,
    BasicRanged,
    BasicSupport,
    Hero
}

public class UnitStats : MonoBehaviour, IDamageable
{
    public float moveSpeed = 3.5f;

    [Header("Combat Stats")]
    public int maxHealth = 100;
    public int health = 100;
    public int damage = 10;

    [Range(0, 3)]
    public int armor = 0;

    [Header("Unit Settings")]
    public int populationCost = 1;
    public UnitType unitType;
    public bool isPlayerUnit;
    public int threatAmount = 2;

    [Header("Team")]
    public CombatTeam combatTeam = CombatTeam.Player;

    // IDamageable
    public int CurrentHealth => health;
    public int MaxHealth => maxHealth;
    public int Armor => armor;
    public bool IsDead => health <= 0;
    public CombatTeam Team => combatTeam;

    private void Start()
    {
        MinimapManager.Instance?.Register(
            transform,
            combatTeam,
            false
        );
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

        health -= finalDamage;
        health = Mathf.Max(health, 0);

        // Debug.Log(
        //     $"{gameObject.name} took {finalDamage} damage " +
        //     $"({incomingDamage} incoming, {armor} armor). " +
        //     $"Health: {health}/{maxHealth}"
        // );

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance.GetPopulationManager().RemovePopulation(populationCost);
        GameManager.Instance.GetThreatManager().RemoveThreat(threatAmount);
        Destroy(gameObject);
    }

    public void NotifyAttacker(GameObject attacker)
    {
        EnemyUnitController enemyController = GetComponent<EnemyUnitController>();

        if (enemyController != null)
        {
            enemyController.NotifyAttacked(attacker);
        }
    }
}