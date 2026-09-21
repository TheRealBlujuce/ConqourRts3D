using UnityEngine;

public class FactionSet : MonoBehaviour
{
    public enum Faction
    {
        Orc,
        Human,
        Undead,
        Elf
    }

    [Header("Faction")]
    public Faction selectedFaction;


    // =====================================================
    // ORC
    // =====================================================

    [Header("Orc References")]
    public GameObject playerBase_Orc;
    public GameObject playerHouse_Orc;
    public GameObject playerLumberyard_Orc;
    public GameObject playerWorker_Orc;
    public GameObject playerBasicMelee_Orc;


    // =====================================================
    // HUMAN
    // =====================================================

    [Header("Human References")]
    public GameObject playerBase_Human;
    public GameObject playerHouse_Human;
    public GameObject playerLumberyard_Human;
    public GameObject playerWorker_Human;
    public GameObject playerBasicMelee_Human;


    // =====================================================
    // UNDEAD
    // =====================================================

    [Header("Undead References")]
    public GameObject playerBase_Undead;
    public GameObject playerHouse_Undead;
    public GameObject playerLumberyard_Undead;
    public GameObject playerWorker_Undead;
    public GameObject playerBasicMelee_Undead;


    // =====================================================
    // ELF
    // =====================================================

    [Header("Elf References")]
    public GameObject playerBase_Elf;
    public GameObject playerHouse_Elf;
    public GameObject playerLumberyard_Elf;
    public GameObject playerWorker_Elf;
    public GameObject playerBasicMelee_Elf;


    // =====================================================
    // SWAP EVERYTHING
    // =====================================================

 public void SwapModels()
    {
        // Find every faction object currently in the scene
        FactionObject[] factionObjects =
            FindObjectsByType<FactionObject>(FindObjectsSortMode.None);

        Debug.Log($"Found {factionObjects.Length} faction objects.");

        foreach (FactionObject factionObject in factionObjects)
        {
            GameObject replacementPrefab =
                GetFactionPrefab(factionObject.objectType);

            // If this faction doesn't have a prefab assigned for this
            // object type, leave the existing object alone.
            if (replacementPrefab == null)
            {
                Debug.LogWarning(
                    $"No {selectedFaction} prefab assigned for " +
                    $"{factionObject.objectType}. Keeping existing object."
                );

                continue;
            }

            ReplaceObject(
                factionObject.gameObject,
                replacementPrefab
            );
        }
    }


    // =====================================================
    // FIND CORRECT PREFAB
    // =====================================================

    private GameObject GetFactionPrefab(FactionObject.ObjectType objectType)
    {
        switch (selectedFaction)
        {
            // -------------------------------------------------
            // ORC
            // -------------------------------------------------

            case Faction.Orc:

                switch (objectType)
                {
                    case FactionObject.ObjectType.Base:
                        return playerBase_Orc;

                    case FactionObject.ObjectType.House:
                        return playerHouse_Orc;

                    case FactionObject.ObjectType.Lumberyard:
                        return playerLumberyard_Orc;

                    case FactionObject.ObjectType.Worker:
                        return playerWorker_Orc;

                    case FactionObject.ObjectType.BasicMelee:
                        return playerBasicMelee_Orc;
                }

                break;


            // -------------------------------------------------
            // HUMAN
            // -------------------------------------------------

            case Faction.Human:

                switch (objectType)
                {
                    case FactionObject.ObjectType.Base:
                        return playerBase_Human;

                    case FactionObject.ObjectType.House:
                        return playerHouse_Human;

                    case FactionObject.ObjectType.Lumberyard:
                        return playerLumberyard_Human;

                    case FactionObject.ObjectType.Worker:
                        return playerWorker_Human;

                    case FactionObject.ObjectType.BasicMelee:
                        return playerBasicMelee_Human;
                }

                break;


            // -------------------------------------------------
            // UNDEAD
            // -------------------------------------------------

            case Faction.Undead:

                switch (objectType)
                {
                    case FactionObject.ObjectType.Base:
                        return playerBase_Undead;

                    case FactionObject.ObjectType.House:
                        return playerHouse_Undead;

                    case FactionObject.ObjectType.Lumberyard:
                        return playerLumberyard_Undead;

                    case FactionObject.ObjectType.Worker:
                        return playerWorker_Undead;

                    case FactionObject.ObjectType.BasicMelee:
                        return playerBasicMelee_Undead;
                }

                break;


            // -------------------------------------------------
            // ELF
            // -------------------------------------------------

            case Faction.Elf:

                switch (objectType)
                {
                    case FactionObject.ObjectType.Base:
                        return playerBase_Elf;

                    case FactionObject.ObjectType.House:
                        return playerHouse_Elf;

                    case FactionObject.ObjectType.Lumberyard:
                        return playerLumberyard_Elf;

                    case FactionObject.ObjectType.Worker:
                        return playerWorker_Elf;

                    case FactionObject.ObjectType.BasicMelee:
                        return playerBasicMelee_Elf;
                }

                break;
        }


        return null;
    }


    // =====================================================
    // REPLACE OBJECT
    // =====================================================

    private void ReplaceObject(
        GameObject currentObject,
        GameObject replacementPrefab
    )
    {
        Vector3 position = currentObject.transform.position;
        Quaternion rotation = currentObject.transform.rotation;
        Transform parent = currentObject.transform.parent;


        // Create replacement
        Instantiate(
            replacementPrefab,
            position,
            rotation,
            parent
        );


        // Destroy old version
        Destroy(currentObject);
    }
}