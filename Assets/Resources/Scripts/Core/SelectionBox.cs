using UnityEngine;

public class SelectionBox : MonoBehaviour
{
    [Header("Box Appearance")]
    public Color borderColor;
    public Color backgroundColor;
    public int borderThickness = 3;

    [Header("Selection")]
    [SerializeField] private SelectionManager selectionManager;

    // Prevent tiny mouse movements from becoming box selections.
    [SerializeField] private float minimumDragDistance = 10f;

    private Vector2 startPos;
    private Vector2 endPos;

    private bool isDragging = false;
    private bool hasDragged = false;

    private Camera cam;

    private static Texture2D _whiteTexture;


    private void Start()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (selectionManager == null)
        {
            selectionManager =
                FindFirstObjectByType<SelectionManager>();
        }

        if (_whiteTexture == null)
        {
            _whiteTexture = new Texture2D(1, 1);

            _whiteTexture.SetPixel(
                0,
                0,
                Color.white
            );

            _whiteTexture.Apply();
        }
    }


    private void Update()
    {
        // ==========================================
        // START DRAG
        // ==========================================

        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            endPos = startPos;

            isDragging = true;
            hasDragged = false;
        }


        // ==========================================
        // UPDATE DRAG
        // ==========================================

        if (isDragging)
        {
            endPos = Input.mousePosition;

            float dragDistance =
                Vector2.Distance(startPos, endPos);

            if (dragDistance >= minimumDragDistance)
            {
                hasDragged = true;
            }
        }


        // ==========================================
        // FINISH DRAG
        // ==========================================

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging && hasDragged)
            {
                SelectObjects();
            }

            isDragging = false;
            hasDragged = false;
        }
    }


    private void OnGUI()
    {
        if (!isDragging || !hasDragged)
            return;

        Rect rect =
            GetScreenRect(startPos, endPos);

        DrawScreenRect(
            rect,
            backgroundColor
        );

        DrawScreenRectBorder(
            rect,
            borderThickness,
            borderColor
        );
    }

    private void SelectObjects()
    {
        if (selectionManager == null)
            return;

        Vector2 min =
            Vector2.Min(startPos, endPos);

        Vector2 max =
            Vector2.Max(startPos, endPos);

        bool shiftHeld =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);


        // Normal box selection replaces the current selection.
        // This will also deselect a building if one was selected.
        if (!shiftHeld)
        {
            selectionManager.DeselectAll();
        }


        // Box selection ONLY searches for units.
        UnitController[] units =
            FindObjectsByType<UnitController>(
                FindObjectsSortMode.None
            );


        foreach (UnitController unit in units)
        {
            Vector3 screenPos =
                cam.WorldToScreenPoint(
                    unit.transform.position
                );

            if (screenPos.z < 0f)
                continue;

            bool insideBox =
                screenPos.x >= min.x &&
                screenPos.x <= max.x &&
                screenPos.y >= min.y &&
                screenPos.y <= max.y;

            if (!insideBox)
                continue;

            selectionManager.Select(unit);
        }
    }


    // ==========================================
    // RECTANGLE HELPERS
    // ==========================================

    private static Rect GetScreenRect(
        Vector2 start,
        Vector2 end
    )
    {
        start.y = Screen.height - start.y;
        end.y = Screen.height - end.y;

        Vector2 topLeft =
            Vector2.Min(start, end);

        Vector2 bottomRight =
            Vector2.Max(start, end);

        return Rect.MinMaxRect(
            topLeft.x,
            topLeft.y,
            bottomRight.x,
            bottomRight.y
        );
    }


    private static void DrawScreenRect(
        Rect rect,
        Color color
    )
    {
        GUI.color = color;

        GUI.DrawTexture(
            rect,
            _whiteTexture
        );

        GUI.color = Color.white;
    }


    private static void DrawScreenRectBorder(
        Rect rect,
        float thickness,
        Color color
    )
    {
        // Top
        DrawScreenRect(
            new Rect(
                rect.xMin,
                rect.yMin,
                rect.width,
                thickness
            ),
            color
        );

        // Bottom
        DrawScreenRect(
            new Rect(
                rect.xMin,
                rect.yMax - thickness,
                rect.width,
                thickness
            ),
            color
        );

        // Left
        DrawScreenRect(
            new Rect(
                rect.xMin,
                rect.yMin,
                thickness,
                rect.height
            ),
            color
        );

        // Right
        DrawScreenRect(
            new Rect(
                rect.xMax - thickness,
                rect.yMin,
                thickness,
                rect.height
            ),
            color
        );
    }


    public void SetCamera(Camera newCamera)
    {
        cam = newCamera;
    }
}