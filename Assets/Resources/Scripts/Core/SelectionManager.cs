using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;

    private readonly List<ISelectable> selectedObjects = new List<ISelectable>();
    
    public event System.Action OnSelectionChanged;
    private bool buildingPlacementActive = false;

    private void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    private void Update()
    {
        HandleSelectionInput();
    }

    private void HandleSelectionInput()
    {
        if (buildingPlacementActive)
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        // ==========================================
        // IGNORE UI CLICKS
        // ==========================================

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        Ray ray =
            mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            ISelectable selectable =
                hit.collider.GetComponent<ISelectable>();

            if (selectable == null)
            {
                selectable =
                    hit.collider.GetComponentInParent<ISelectable>();
            }

            if (selectable != null)
            {
                // Normal click replaces current selection.
                if (!shiftHeld)
                {
                    DeselectAll();
                }

                // Shift-click selected object removes it.
                if (shiftHeld && selectable.IsSelected)
                {
                    Deselect(selectable);
                }
                else
                {
                    Select(selectable);
                }

                return;
            }
        }

        // Empty world-space click clears selection.
        if (!shiftHeld)
        {
            DeselectAll();
        }
    }

    public void Select(ISelectable selectable)
    {
        if (selectable == null)
            return;

        if (selectedObjects.Contains(selectable))
            return;

        // ==========================================
        // DON'T MIX UNITS AND BUILDINGS
        // ==========================================

        if (selectedObjects.Count > 0)
        {
            SelectableType currentType =
                selectedObjects[0].SelectableType;

            if (currentType != selectable.SelectableType)
            {
                DeselectAll();
            }
        }

        selectedObjects.Add(selectable);

        selectable.Select();

        OnSelectionChanged?.Invoke();
    }

    public void Deselect(ISelectable selectable)
    {
        if (selectable == null)
            return;

        if (!selectedObjects.Contains(selectable))
            return;

        selectedObjects.Remove(selectable);

        selectable.Deselect();

        OnSelectionChanged?.Invoke();
    }

    public void DeselectAll()
    {
        foreach (ISelectable selectable in selectedObjects)
        {
            selectable?.Deselect();
        }

        selectedObjects.Clear();

        OnSelectionChanged?.Invoke();
    }

    public IReadOnlyList<ISelectable> GetSelectedObjects()
    {
        return selectedObjects;
    }

    public void SetBuildingPlacementActive(bool active)
    {
        buildingPlacementActive = active;

        if (active)
        {
            DeselectAll();
        }
    }
}