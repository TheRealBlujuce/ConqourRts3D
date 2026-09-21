using UnityEngine;

public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    int Armor { get; }

    CombatTeam Team { get; }

    bool IsDead { get; }

    void TakeDamage(int damage);
}