using UnityEngine;
using UnityEngine.UI;

public class TargetDummy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    [Range(1, 3)]
    [SerializeField] private int armor = 1;

    [Header("Health Bar")]
    [SerializeField] private Image healthBarFill;

    [Header("Team")]
    public CombatTeam combatTeam = CombatTeam.Player;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int Armor => armor;

    public bool IsDead => currentHealth <= 0;
    public CombatTeam Team => combatTeam;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }


    public void TakeDamage(int incomingDamage)
    {
        if (IsDead)
            return;

        int finalDamage = CalculateDamage(incomingDamage);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(
            $"{gameObject.name} took {finalDamage} damage. " +
            $"Health: {currentHealth}/{maxHealth}"
        );

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }


    private int CalculateDamage(int incomingDamage)
    {
        int damageReduction = 0;

        switch (armor)
        {
            case 1:
                damageReduction = 1;
                break;

            case 2:
                damageReduction = 2;
                break;

            case 3:
                damageReduction = 3;
                break;
        }

        return Mathf.Max(1, incomingDamage - damageReduction);
    }


    private void UpdateHealthBar()
    {
        if (healthBarFill == null)
            return;

        healthBarFill.fillAmount =
            (float)currentHealth / maxHealth;
    }


    private void Die()
    {
        Debug.Log($"{gameObject.name} destroyed!");

        Destroy(gameObject);
    }
}