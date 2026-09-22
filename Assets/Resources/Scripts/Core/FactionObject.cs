using UnityEngine;

public class FactionObject : MonoBehaviour
{
    public ObjectType objectType;

    public enum ObjectType
    {
        Base,
        House,
        Storehouse,
        Barracks,
        Altar,
        Worker,
        BasicMelee,
        BasicRanged,
        BasicSupport,
        Hero
    }
}