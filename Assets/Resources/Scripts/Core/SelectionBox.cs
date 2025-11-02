using UnityEngine;

public class SelectionBox : MonoBehaviour
{
	public Color borderColor;
	public Color backgroundColor;
	public int borderThickness = 3;

    private Vector2 startPos;
    private Vector2 endPos;
    private bool isDragging = false;
    private Camera cam;
	
    private static Texture2D _whiteTexture;

    private void Start()
    {
        cam = Camera.main;
        if (_whiteTexture == null)
        {
            _whiteTexture = new Texture2D(1, 1);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            startPos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            SelectUnits();
        }

        if (isDragging)
        {
            endPos = Input.mousePosition;
        }
    }

    private void OnGUI()
    {
        if (isDragging)
        {
            Rect rect = GetScreenRect(startPos, endPos);
            DrawScreenRect(rect, backgroundColor);       // Fill
            DrawScreenRectBorder(rect, borderThickness, borderColor); // Border
        }
    }

    void SelectUnits()
    {
        Vector2 min = Vector2.Min(startPos, endPos);
        Vector2 max = Vector2.Max(startPos, endPos);

        foreach (UnitController unit in FindObjectsByType<UnitController>(FindObjectsSortMode.None))
        {
            Vector2 screenPos = cam.WorldToScreenPoint(unit.transform.position);
            if (screenPos.x > min.x && screenPos.x < max.x &&
                screenPos.y > min.y && screenPos.y < max.y)
            {
                unit.SetSelected(true);
            }
            //else
            //{
            //    unit.SetSelected(false);
            //}
        }
    }

    // --- Helpers ---
    static Rect GetScreenRect(Vector2 start, Vector2 end)
    {
        start.y = Screen.height - start.y;
        end.y = Screen.height - end.y;
        Vector2 topLeft = Vector2.Min(start, end);
        Vector2 bottomRight = Vector2.Max(start, end);
        return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
    }

    static void DrawScreenRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, _whiteTexture);
        GUI.color = Color.white;
    }

    static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
    {
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);                     // Top
        DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);         // Bottom
        DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);                    // Left
        DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);        // Right
    }

	public void SetCamera(Camera newCamera)
	{
		cam = newCamera;
	}
}
