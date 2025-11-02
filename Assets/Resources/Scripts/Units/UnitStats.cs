using UnityEngine;

public enum UnitType
{
    Worker,
    Archer,
    Warrior
}

public class UnitStats : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public int health = 100;
    public int damage = 10;
    public int armor = 0;
	public int populationCost = 1;
    public UnitType unitType;
	public bool isPlayerUnit;
}
