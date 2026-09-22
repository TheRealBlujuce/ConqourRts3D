using System.Collections.Generic;
using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Universal Building Buttons")]
    [SerializeField] private List<BuildingButton> buildingButtons = new List<BuildingButton>();
    private UnitButton[] unitButtons;

    [Header("Faction Building Placers")]
    [SerializeField] private List<FactionBuildingPlacers> factionBuildingPlacers = new List<FactionBuildingPlacers>();

    [Header("Delete Button")]
    [SerializeField] private GameObject deleteButton;

    [Header("Button Container")]
    [SerializeField] private Transform buttonsContainer;

    private void Awake()
    {
        if (selectionManager == null)
        {
            selectionManager = FindFirstObjectByType<SelectionManager>();
        }

        if (buttonsContainer == null)
        {
            Transform container = transform.Find("Button Container");

            if (container != null)
            {
                buttonsContainer = container;
            }
        }

        if (buttonsContainer != null)
        {
            unitButtons = buttonsContainer.GetComponentsInChildren<UnitButton>(true);
        }
        else
        {
            Debug.LogError("ButtonManager could not find Button Container!");

            unitButtons = new UnitButton[0];
        }
    }

    private void Start()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged += RefreshButtons;
        }

        RefreshButtons();
    }

    // ==========================================
    // REFRESH UI
    // ==========================================

    private void RefreshButtons()
    {
        if (selectionManager == null)
            return;

        IReadOnlyList<ISelectable> selectedObjects = selectionManager.GetSelectedObjects();

        // Nothing selected:
        // Building buttons ON
        // Unit buttons OFF
        // Delete OFF
        if (selectedObjects.Count == 0)
        {
            HideUnitButtons();
            HideDeleteButton();
            ShowDefaultBuildingButtons();
            return;
        }

        ISelectable selected = selectedObjects[0];

        // Unit selected:
        // Building buttons OFF
        // Unit buttons OFF
        // Delete ON
        if (selected.SelectableType == SelectableType.Unit)
        {
            HideBuildingButtons();
            HideUnitButtons();
            ShowDeleteButton();
            return;
        }

        // Building selected:
        // Building buttons OFF
        // Appropriate unit buttons ON
        // Delete ON
        if (selected.SelectableType == SelectableType.Building)
        {
            HideBuildingButtons();

            MonoBehaviour behaviour = selected as MonoBehaviour;

            if (behaviour != null)
            {
                BuildingController building = behaviour.GetComponent<BuildingController>();

                if (building != null)
                {
                    ShowUnitButtons(building);
                }
                else
                {
                    HideUnitButtons();
                }
            }
            else
            {
                HideUnitButtons();
            }

            ShowDeleteButton();
            return;
        }

        // Failsafe
        HideUnitButtons();
        HideBuildingButtons();
        HideDeleteButton();
    }

    // ==========================================
    // DEFAULT BUILDING BUTTONS
    // ==========================================

    private void ShowDefaultBuildingButtons()
    {
        Race playerRace = GameManager.Instance.playerRace;

        foreach (BuildingButton button in buildingButtons)
        {
            if (button == null)
                continue;

            GameObject placerPrefab = GetBuildingPlacerPrefab(playerRace, button.buildingType);

            if (placerPrefab == null)
            {
                SetButtonActive(button.gameObject, false);
                continue;
            }

            button.SetPlacerPrefab(placerPrefab);

            SetButtonActive(button.gameObject, true);
        }
    }

    private void HideBuildingButtons()
    {
        foreach (BuildingButton button in buildingButtons)
        {
            if (button == null)
                continue;

            SetButtonActive(button.gameObject, false);
        }
    }

    private GameObject GetBuildingPlacerPrefab(Race race, BuildingButton.BuildingButtonType buildingType)
    {
        FactionBuildingPlacers faction = factionBuildingPlacers.Find(x => x.race == race);

        if (faction == null)
        {
            Debug.LogWarning($"No building placers configured for {race}.");
            return null;
        }

        switch (buildingType)
        {
            case BuildingButton.BuildingButtonType.House:
                return faction.house;

            case BuildingButton.BuildingButtonType.Storehouse:
                return faction.storehouse;

            case BuildingButton.BuildingButtonType.Barracks:
                return faction.barracks;

            case BuildingButton.BuildingButtonType.Altar:
                return faction.altar;

            case BuildingButton.BuildingButtonType.Tower:
                return faction.tower;

            default:
                return null;
        }
    }

    // ==========================================
    // UNIT BUTTONS
    // ==========================================

    private void ShowUnitButtons(BuildingController building)
    {
        HideUnitButtons();

        UnitProductionManager productionManager = building.GetComponent<UnitProductionManager>();

        if (productionManager == null)
            return;

        for (int i = 0; i < building.unitButtons.Count; i++)
        {
            UnitButton.UnitButtonType requestedType = building.unitButtons[i];

            UnitButton button = FindUnitButton(requestedType);

            if (button == null)
            {
                Debug.LogWarning($"No universal UnitButton found for {requestedType}.");

                continue;
            }

            if (i >= productionManager.availableUnits.Count)
            {
                Debug.LogWarning(
                    $"{building.name} has a button for " +
                    $"{requestedType}, but no matching " +
                    $"availableUnits entry at index {i}."
                );

                continue;
            }

            button.SetSpawner(productionManager, i);

            SetButtonActive(button.gameObject, true);
        }
    }
    
    private UnitButton FindUnitButton(UnitButton.UnitButtonType type)
    {
        foreach (UnitButton button in unitButtons)
        {
            if (button == null)
                continue;

            if (button.unitType == type)
            {
                return button;
            }
        }

        return null;
    }

    private void HideUnitButtons()
    {
        foreach (UnitButton button in unitButtons)
        {
            if (button == null)
                continue;

            SetButtonActive(button.gameObject, false);
        }
    }

    // ==========================================
    // DELETE
    // ==========================================

    private void ShowDeleteButton()
    {
        SetButtonActive(deleteButton, true);
    }

    private void HideDeleteButton()
    {
        SetButtonActive(deleteButton, false);
    }

    private void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.OnSelectionChanged -= RefreshButtons;
        }
    }

    private void SetButtonActive(GameObject button, bool active)
    {
        if (button != null && button.activeSelf != active)
        {
            button.SetActive(active);
        }
    }
}

[System.Serializable]
public class FactionBuildingPlacers
{
    public Race race;

    public GameObject house;
    public GameObject storehouse;
    public GameObject barracks;
    public GameObject altar;
    public GameObject tower;
}

