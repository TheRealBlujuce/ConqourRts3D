using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MinimapManager : MonoBehaviour, IPointerClickHandler
{
    public static MinimapManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private RectTransform minimapRect;

    [Header("Marker Prefab")]
    [SerializeField] private GameObject markerPrefab;

    [Header("Marker Settings")]
    [SerializeField] private Vector2 unitMarkerSize = new Vector2(5f, 5f);
    [SerializeField] private Vector2 buildingMarkerSize = new Vector2(8f, 8f);
    
    [Header("Marker Containers")]
    [SerializeField] private RectTransform playerUnitsContainer;
    [SerializeField] private RectTransform enemyUnitsContainer;
    [SerializeField] private RectTransform playerBuildingsContainer;

    [Header("Colors")]
    [SerializeField] private Color playerColor = Color.blue;
    [SerializeField] private Color enemyColor = Color.red;

    [Header("Camera")]
    [SerializeField] private CameraController cameraController;

    [Header("Camera View")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform cameraViewRect;
    [SerializeField] private float cameraViewWidthPadding = 0f;
    [SerializeField] private float cameraViewHeightPadding = 10f;
    
    private readonly List<MinimapMarker> markers = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetTerrain(Terrain newTerrain)
    {
        terrain = newTerrain;
    }

    public void Register(Transform target, CombatTeam team, bool isBuilding)
    {
        if (target == null || markerPrefab == null)
            return;

        // ==========================================
        // DETERMINE MARKER CONTAINER
        // ==========================================

        RectTransform markerContainer;

        if (isBuilding)
        {
            markerContainer = playerBuildingsContainer;
        }
        else if (team == CombatTeam.Player)
        {
            markerContainer = playerUnitsContainer;
        }
        else if (team == CombatTeam.Enemy)
        {
            markerContainer = enemyUnitsContainer;
        }
        else
        {
            // Don't display neutral/unsupported teams.
            return;
        }

        if (markerContainer == null)
        {
            Debug.LogWarning(
                $"Minimap marker container is missing for {target.name}."
            );

            return;
        }

        // ==========================================
        // CREATE MARKER
        // ==========================================

        GameObject markerObject = Instantiate(markerPrefab, markerContainer);

        RectTransform markerRect = markerObject.GetComponent<RectTransform>();

        Image markerImage = markerObject.GetComponent<Image>();

        if (markerRect == null || markerImage == null)
        {
            Debug.LogError(
                "Minimap marker prefab needs a RectTransform and Image."
            );

            Destroy(markerObject);
            return;
        }

        // ==========================================
        // SIZE
        // ==========================================

        markerRect.sizeDelta = isBuilding ? buildingMarkerSize : unitMarkerSize;

        // ==========================================
        // COLOR
        // ==========================================

        markerImage.color = team == CombatTeam.Player ? playerColor : enemyColor;

        // ==========================================
        // REGISTER
        // ==========================================

        MinimapMarker marker = new MinimapMarker
        {
            target = target,
            rectTransform = markerRect,
            team = team,
            isBuilding = isBuilding
        };

        markers.Add(marker);

        UpdateMarker(marker);
    }

    public void Unregister(Transform target)
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            if (markers[i].target != target)
                continue;

            if (markers[i].rectTransform != null)
                Destroy(markers[i].rectTransform.gameObject);

            markers.RemoveAt(i);
        }
    }

    private void LateUpdate()
    {
        for (int i = markers.Count - 1; i >= 0; i--)
        {
            MinimapMarker marker = markers[i];

            if (marker.target == null)
            {
                if (marker.rectTransform != null)
                    Destroy(marker.rectTransform.gameObject);

                markers.RemoveAt(i);
                continue;
            }

            UpdateMarker(marker);
        }

        UpdateCameraView();
    }

    private void UpdateMarker(MinimapMarker marker)
    {
        if (terrain == null || minimapRect == null)
            return;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        Vector3 worldPosition = marker.target.position;

        // Convert world coordinates to 0-1 terrain coordinates.
        float normalizedX =
            (worldPosition.x - terrainPosition.x) / terrainSize.x;

        float normalizedZ =
            (worldPosition.z - terrainPosition.z) / terrainSize.z;

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        // Convert normalized coordinates to minimap coordinates.
        float minimapX =
            (normalizedX - 0.5f) * minimapRect.rect.width;

        float minimapY =
            (normalizedZ - 0.5f) * minimapRect.rect.height;

        marker.rectTransform.anchoredPosition = WorldToMinimapPosition(marker.target.position);
    }

    private void UpdateCameraView()
    {
        if (mainCamera == null || terrain == null || minimapRect == null || cameraViewRect == null)
        {
            return;
        }

        float groundY = terrain.transform.position.y;

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));

        // Find where the four corners of the camera
        // intersect the ground.
        Vector3 bottomLeft = GetCameraGroundPoint(new Vector2(0f, 0f), groundPlane);

        Vector3 bottomRight = GetCameraGroundPoint(new Vector2(1f, 0f), groundPlane);

        Vector3 topLeft = GetCameraGroundPoint(new Vector2(0f, 1f), groundPlane);

        Vector3 topRight = GetCameraGroundPoint(new Vector2(1f, 1f), groundPlane);

        // Find the world-space bounds of the visible area.
        float minX = Mathf.Min(
            bottomLeft.x,
            bottomRight.x,
            topLeft.x,
            topRight.x
        );

        float maxX = Mathf.Max(
            bottomLeft.x,
            bottomRight.x,
            topLeft.x,
            topRight.x
        );

        float minZ = Mathf.Min(
            bottomLeft.z,
            bottomRight.z,
            topLeft.z,
            topRight.z
        );

        float maxZ = Mathf.Max(
            bottomLeft.z,
            bottomRight.z,
            topLeft.z,
            topRight.z
        );

        Vector2 minMapPosition = WorldToMinimapPosition(new Vector3(minX, 0f, minZ));

        Vector2 maxMapPosition = WorldToMinimapPosition(new Vector3(maxX, 0f, maxZ));

        // Position rectangle in center of visible area.
        cameraViewRect.anchoredPosition = (minMapPosition + maxMapPosition) * 0.5f;

        // Size rectangle to match visible area.
        float viewWidth = Mathf.Abs(maxMapPosition.x - minMapPosition.x);

        float viewHeight = Mathf.Abs(maxMapPosition.y - minMapPosition.y);

        cameraViewRect.sizeDelta = new Vector2( viewWidth + cameraViewWidthPadding, viewHeight + cameraViewHeightPadding);
    }

    private Vector3 GetCameraGroundPoint(Vector2 viewportPoint, Plane groundPlane)
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(viewportPoint.x, viewportPoint.y, 0f ));

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return mainCamera.transform.position;
    }

    private Vector2 WorldToMinimapPosition(Vector3 worldPosition)
    {
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float normalizedX = (worldPosition.x - terrainPosition.x) / terrainSize.x;

        float normalizedZ = (worldPosition.z - terrainPosition.z) / terrainSize.z;

        normalizedX = Mathf.Clamp01(normalizedX);
        normalizedZ = Mathf.Clamp01(normalizedZ);

        float minimapX = (normalizedX - 0.5f) * minimapRect.rect.width;

        float minimapY = (normalizedZ - 0.5f) * minimapRect.rect.height;

        return new Vector2(minimapX, minimapY);
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (terrain == null || minimapRect == null || cameraController == null)
            return;

        // Convert mouse position into local minimap coordinates.
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                minimapRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return;
        }

        Rect rect = minimapRect.rect;

        // Convert minimap position into 0-1 coordinates.
        float normalizedX = Mathf.InverseLerp(
            rect.xMin,
            rect.xMax,
            localPoint.x
        );

        float normalizedZ = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            localPoint.y
        );

        // Convert 0-1 coordinates into terrain world coordinates.
        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float worldX =
            terrainPosition.x + normalizedX * terrainSize.x;

        float worldZ =
            terrainPosition.z + normalizedZ * terrainSize.z;

        cameraController.TeleportToPosition(
            new Vector3(worldX, 0f, worldZ)
        );
    }

    public void SetCameraController(CameraController newCameraController)
    {
        cameraController = newCameraController;
    }

    public void SetMainCamera(Camera newCamera)
    {
        mainCamera = newCamera;
    }

    private class MinimapMarker
    {
        public Transform target;
        public RectTransform rectTransform;
        public CombatTeam team;
        public bool isBuilding;
    }
}