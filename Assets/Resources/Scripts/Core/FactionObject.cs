using UnityEngine;

public class FactionObject : MonoBehaviour
{
    public ObjectType objectType;

    public enum ObjectType
    {
        Base,
        House,
        Lumberyard,
        Worker,
        BasicMelee
    }
}