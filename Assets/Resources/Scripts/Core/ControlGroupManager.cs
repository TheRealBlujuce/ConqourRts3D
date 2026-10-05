using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ControlGroupManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SelectionManager selectionManager;
    [SerializeField] private CameraController cameraController;

    [Header("Settings")]
    [SerializeField] private float doubleTapTime = 0.3f;

    [Header("Control Group UI")]
    [SerializeField] private string controlGroupsContainerName = "Control Groups";

    private readonly Dictionary<int, GameObject> controlGroupUIObjects = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, TextMeshProUGUI> controlGroupAmountTexts = new Dictionary<int, TextMeshProUGUI>();
    private bool controlGroupUIFound = false;

    private readonly Dictionary<int, List<ISelectable>> controlGroups = new Dictionary<int, List<ISelectable>>();

    private int lastPressedGroup = -1;
    private float lastGroupPressTime = -999f;

    private void Awake()
    {
        // Create groups 1-9.
        for (int i = 1; i <= 10; i++)
        {
            controlGroups[i] = new List<ISelectable>();
        }

        if (selectionManager == null)
        {
            selectionManager =
                FindFirstObjectByType<SelectionManager>();
        }

        if (cameraController == null)
        {
            cameraController =
                FindFirstObjectByType<CameraController>();
        }
        
    }

    private void Update()
    {
        ValidateControlGroupUI();

        if (!controlGroupUIFound)
            FindControlGroupUI();

        for (int i = 1; i <= 10; i++)
        {
            if (!GetNumberKeyDown(i))
                continue;

            bool shiftHeld = Input.GetKey(KeyCode.LeftShift);

            // ==========================================
            // CTRL + NUMBER
            // Assign current selection to group.
            // ==========================================

            if (shiftHeld)
            {
                AssignControlGroup(i);
                return;
            }

            // ==========================================
            // NUMBER
            // Select group.
            // ==========================================

            SelectControlGroup(i);

            return;
        }
    }

    // ==========================================
    // ASSIGN CONTROL GROUP
    // ==========================================

    private void AssignControlGroup(int groupNumber)
    {
        if (selectionManager == null)
            return;

        IReadOnlyList<ISelectable> selected =
            selectionManager.GetSelectedObjects();

        if (selected.Count == 0)
            return;

        List<ISelectable> group =
            controlGroups[groupNumber];

        // Ctrl + Number REPLACES the existing group.
        group.Clear();

        SelectableType groupType =
            selected[0].SelectableType;

        foreach (ISelectable selectable in selected)
        {
            if (selectable == null)
                continue;

            // Extra safety:
            // units and buildings cannot share a group.
            if (selectable.SelectableType != groupType)
                continue;

            group.Add(selectable);
        }

        UpdateAllControlGroupUI();

        Debug.Log(
            $"Control Group {groupNumber}: " +
            $"assigned {group.Count} {groupType}(s)."
        );
    }

    // ==========================================
    // SELECT CONTROL GROUP
    // ==========================================

    private void SelectControlGroup(int groupNumber)
    {
        if (selectionManager == null)
            return;

        CleanControlGroup(groupNumber);

        List<ISelectable> group =
            controlGroups[groupNumber];

        if (group.Count == 0)
            return;

        // Check whether this is a double-tap.
        bool doubleTapped =
            lastPressedGroup == groupNumber &&
            Time.unscaledTime - lastGroupPressTime
                <= doubleTapTime;

        // Clear current selection.
        selectionManager.DeselectAll();

        // Select every object in this group.
        foreach (ISelectable selectable in group)
        {
            if (!IsSelectableAlive(selectable))
                continue;

            selectionManager.Select(selectable);
        }

        // Double-tapping centers the camera.
        if (doubleTapped)
        {
            CenterCameraOnGroup(group);
        }

        lastPressedGroup = groupNumber;
        lastGroupPressTime = Time.unscaledTime;
    }

    // ==========================================
    // CENTER CAMERA
    // ==========================================

    private void CenterCameraOnGroup(
        List<ISelectable> group)
    {
        if (cameraController == null)
            return;

        Vector3 center = Vector3.zero;
        int validCount = 0;

        foreach (ISelectable selectable in group)
        {
            if (!IsSelectableAlive(selectable))
                continue;

            MonoBehaviour behaviour =
                selectable as MonoBehaviour;

            if (behaviour == null)
                continue;

            center += behaviour.transform.position;
            validCount++;
        }

        if (validCount == 0)
            return;

        center /= validCount;

        cameraController.TeleportToPosition(center);
    }

    // ==========================================
    // CLEAN DESTROYED OBJECTS
    // ==========================================

    private void CleanControlGroup(int groupNumber)
    {
        List<ISelectable> group = controlGroups[groupNumber];
        bool changed = false;

        for (int i = group.Count - 1; i >= 0; i--)
        {
            if (!IsSelectableAlive(group[i]))
            {
                group.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            UpdateControlGroupUI(groupNumber);
        }
    }

    private void UpdateControlGroupUI(int groupNumber)
    {
        if (!controlGroupUIFound)
            FindControlGroupUI();

        if (!controlGroups.TryGetValue(groupNumber, out List<ISelectable> group))
            return;

        if (!controlGroupUIObjects.TryGetValue(groupNumber, out GameObject groupObject))
            return;

        int count = group.Count;

        groupObject.SetActive(count > 0);

        if (count == 0)
            return;

        if (controlGroupAmountTexts.TryGetValue(groupNumber, out TextMeshProUGUI amountText))
            amountText.text = count.ToString();
    }

    private void FindControlGroupUI()
    {
        controlGroupUIObjects.Clear();
        controlGroupAmountTexts.Clear();
        controlGroupUIFound = false;

        GameObject container = GameObject.Find(controlGroupsContainerName);

        if (container == null)
            return;

        for (int i = 1; i <= 10; i++)
        {
            string keyboardNumber = i == 10 ? "0" : i.ToString();
            Transform groupTransform = container.transform.Find($"group {keyboardNumber}");

            if (groupTransform == null)
                continue;

            TextMeshProUGUI[] texts = groupTransform.GetComponentsInChildren<TextMeshProUGUI>(true);

            if (texts.Length < 2)
            {
                Debug.LogWarning($"Control Group UI '{groupTransform.name}' does not contain two TextMeshProUGUI objects.");
                continue;
            }

            controlGroupUIObjects[i] = groupTransform.gameObject;
            controlGroupAmountTexts[i] = texts[1];
        }

        controlGroupUIFound = controlGroupUIObjects.Count > 0;

        if (controlGroupUIFound)
            UpdateAllControlGroupUI();
    }

    private void UpdateAllControlGroupUI()
    {
        for (int i = 1; i <= 10; i++)
        {
            UpdateControlGroupUI(i);
        }
    }

    private bool IsSelectableAlive(
        ISelectable selectable)
    {
        if (selectable == null)
            return false;

        // Unity destroyed objects are a little weird when
        // stored through an interface, so check the
        // underlying MonoBehaviour as well.
        MonoBehaviour behaviour =
            selectable as MonoBehaviour;

        return behaviour != null;
    }

    // ==========================================
    // INPUT
    // ==========================================

    private bool GetNumberKeyDown(int number)
    {
        switch (number)
        {
            case 1:
                return Input.GetKeyDown(KeyCode.Alpha1);

            case 2:
                return Input.GetKeyDown(KeyCode.Alpha2);

            case 3:
                return Input.GetKeyDown(KeyCode.Alpha3);

            case 4:
                return Input.GetKeyDown(KeyCode.Alpha4);

            case 5:
                return Input.GetKeyDown(KeyCode.Alpha5);

            case 6:
                return Input.GetKeyDown(KeyCode.Alpha6);

            case 7:
                return Input.GetKeyDown(KeyCode.Alpha7);

            case 8:
                return Input.GetKeyDown(KeyCode.Alpha8);

            case 9:
                return Input.GetKeyDown(KeyCode.Alpha9);
        }

        return false;
    }

    public void SetCameraController(CameraController newCamController)
    {
        cameraController = newCamController;
    }

    public void SetSelectionManager(SelectionManager newSelectionManager)
    {
        selectionManager = newSelectionManager;
    }

    private void ValidateControlGroupUI()
    {
        if (!controlGroupUIFound)
            return;

        foreach (TextMeshProUGUI text in controlGroupAmountTexts.Values)
        {
            if (text == null)
            {
                controlGroupAmountTexts.Clear();
                controlGroupUIFound = false;
                return;
            }
        }
    }
}

[System.Serializable]
public class ControlGroupUI
{
    public int groupNumber;
    public TextMeshProUGUI unitAmountText;
}