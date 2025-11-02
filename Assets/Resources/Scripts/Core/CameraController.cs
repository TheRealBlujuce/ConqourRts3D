using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15f; // pan speed
    public float zoomSpeed = 10f; // orthographic zoom speed
    public float minZoom = 5f;    // smallest orthographic size
    public float maxZoom = 50f;   // largest orthographic size
    public float movementLerpFactor = 5f;
    public float zoomLerpFactor = 5f;

    [Header("Initial Position")]
    public string playerBaseTag = "PlayerBase";
    public Vector3 initialOffset = new Vector3(0, 0, 0);

    [Header("Bounds Settings")]
    public Terrain terrain; // assign in inspector (auto-found if null)

    private Vector3 targetPosition;
    private float targetOrthoSize;
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        cam.orthographic = true; // force orthographic

        if (terrain == null)
            terrain = Terrain.activeTerrain;

        GameObject playerBase = GameObject.FindGameObjectWithTag(playerBaseTag);
        if (playerBase != null)
        {
            targetPosition = playerBase.transform.position + initialOffset;
            transform.position = targetPosition;
        }
        else
        {
            Debug.LogWarning($"Player base with tag '{playerBaseTag}' not found. Using current camera position.");
            targetPosition = transform.position;
        }

        targetOrthoSize = minZoom;
    }

    void Update()
    {
        HandleMovement();
        HandleZoom();
        ClampToTerrainBounds();

        // Smooth position
        transform.position = Vector3.Lerp(
            transform.position,
            new Vector3(targetPosition.x, 0f, targetPosition.z),
            Time.deltaTime * movementLerpFactor
        );

        // Smooth zoom
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthoSize, Time.deltaTime * zoomLerpFactor);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forwardXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 rightXZ = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        targetPosition += (forwardXZ * moveZ + rightXZ * moveX) * moveSpeed * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetOrthoSize -= scroll * zoomSpeed;
            targetOrthoSize = Mathf.Clamp(targetOrthoSize, minZoom, maxZoom);
        }
    }

    void ClampToTerrainBounds()
    {
        if (terrain == null) return;

        Vector3 terrainPos = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;

        float tiltRad = Mathf.Deg2Rad * transform.eulerAngles.x;

        // Project orthographic size onto X and Z directions
        float halfViewHeight = targetOrthoSize;
        float halfViewWidth = targetOrthoSize * cam.aspect;

        // How much of the height extends forward/back due to tilt
        float zExtent = Mathf.Sin(tiltRad) * halfViewHeight;
        float yCompensatedHeight = Mathf.Cos(tiltRad) * halfViewHeight; // vertical component — not needed for clamp but helps visualization

        // X extent is purely horizontal width
        float xExtent = halfViewWidth;

        float minX = terrainPos.x + xExtent;
        float maxX = terrainPos.x + terrainSize.x - xExtent;

        float minZ = terrainPos.z + zExtent;
        float maxZ = terrainPos.z + terrainSize.z - zExtent;

        targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
    }
}
