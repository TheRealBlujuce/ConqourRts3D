using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 15f;
    public float zoomSpeed = 10f;
    public float minZoom = 5f;
    public float maxZoom = 50f;
    public float movementLerpFactor = 5f;
    public float zoomLerpFactor = 5f;

    [Header("Initial Position")]
    public string playerBaseTag = "PlayerBase";
    public Vector3 initialOffset = Vector3.zero;
    private GameObject playerBase;

    [Header("Bounds Settings")]
    [SerializeField] private Terrain terrain;
    private Bounds terrainBounds;

    private Vector3 targetPosition;
    private float targetOrthoSize;
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;

        if (cam == null)
        {
            Debug.LogError("CameraController could not find the Main Camera.");
            return;
        }

        cam.orthographic = true;

        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (terrain == null)
        {
            Debug.LogError(
                "CameraController could not find a Terrain."
            );

            return;
        }

        terrainBounds = GetTerrainBounds();

        playerBase = GameObject.FindGameObjectWithTag(playerBaseTag);

        if (playerBase != null)
        {
            targetPosition = playerBase.transform.position + initialOffset;

            transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        }
        else
        {
            Debug.LogWarning(
                $"Player base with tag '{playerBaseTag}' not found. " +
                $"Using current camera position."
            );

            targetPosition = transform.position;
        }

        targetOrthoSize = minZoom;

        ClampToBounds();
    }

   private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleCenterCamera();

        ClampToBounds();

        // Smooth position
        Vector3 desiredPosition = new Vector3(
            targetPosition.x,
            transform.position.y,
            targetPosition.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * movementLerpFactor
        );

        // Smooth zoom
        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetOrthoSize,
            Time.deltaTime * zoomLerpFactor
        );
    }

    private void HandleCenterCamera()
    {
        if (!Input.GetKeyDown(KeyCode.Space))
            return;

        // Center on player base if one exists.
        if (playerBase != null)
        {
            targetPosition = playerBase.transform.position + initialOffset;

            return;
        }

        // Otherwise center on the terrain.
        targetPosition.x = terrainBounds.center.x;

        targetPosition.z = terrainBounds.center.z;
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 forwardXZ =
            Vector3.ProjectOnPlane(
                transform.forward,
                Vector3.up
            ).normalized;

        Vector3 rightXZ =
            Vector3.ProjectOnPlane(
                transform.right,
                Vector3.up
            ).normalized;

        targetPosition +=
            (forwardXZ * moveZ + rightXZ * moveX)
            * moveSpeed
            * Time.deltaTime;
    }

    private void HandleZoom()
    {
        float scroll =
            Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) <= 0.001f)
            return;

        targetOrthoSize -=
            scroll * zoomSpeed;

        targetOrthoSize = Mathf.Clamp(
            targetOrthoSize,
            minZoom,
            maxZoom
        );
    }

    private void ClampToBounds()
    {
        if (terrain == null || cam == null)
            return;

        Bounds bounds = terrainBounds;

        // Assume the playable ground is at the bottom/center Y
        // of the camera bounds.
        float groundY = terrain.transform.position.y;

        Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));


        // Find where all four camera corners hit the ground.
        Vector3 bottomLeft = GetViewportGroundPoint(new Vector2(0f, 0f), groundPlane);

        Vector3 bottomRight = GetViewportGroundPoint(new Vector2(1f, 0f), groundPlane);

        Vector3 topLeft = GetViewportGroundPoint(new Vector2(0f, 1f), groundPlane);

        Vector3 topRight = GetViewportGroundPoint(new Vector2(1f, 1f), groundPlane);


        // Calculate how far the visible area extends
        // from the camera's X/Z position.
        float minVisibleX = Mathf.Min(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);

        float maxVisibleX = Mathf.Max(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x);

        float minVisibleZ = Mathf.Min(bottomLeft.z, bottomRight.z, topLeft.z, topRight.z);

        float maxVisibleZ = Mathf.Max(bottomLeft.z, bottomRight.z, topLeft.z, topRight.z);


        float leftExtent = transform.position.x - minVisibleX;

        float rightExtent = maxVisibleX - transform.position.x;

        float southExtent = transform.position.z - minVisibleZ;

        float northExtent = maxVisibleZ - transform.position.z;

        // Calculate allowed camera position.
        float minX = bounds.min.x + leftExtent;

        float maxX = bounds.max.x - rightExtent;

        float minZ = bounds.min.z + southExtent;

        float maxZ = bounds.max.z - northExtent;


        // Clamp X
        if (minX > maxX)
        {
            targetPosition.x = bounds.center.x;
        }
        else
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
        }


        // Clamp Z
        if (minZ > maxZ)
        {
            targetPosition.z = bounds.center.z;
        }
        else
        {
            targetPosition.z = Mathf.Clamp( targetPosition.z, minZ, maxZ);
        }
    }
    
    private Vector3 GetViewportGroundPoint(Vector2 viewportPoint, Plane groundPlane)
    {
        Ray ray =
            cam.ViewportPointToRay(
                new Vector3(
                    viewportPoint.x,
                    viewportPoint.y,
                    0f
                )
            );

        if (groundPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }
    private Bounds GetTerrainBounds()
    {
        Vector3 terrainPosition =
            terrain.transform.position;

        Vector3 terrainSize =
            terrain.terrainData.size;

        Vector3 center =
            terrainPosition +
            terrainSize * 0.5f;

        return new Bounds(
            center,
            terrainSize
        );
    }

    public void SetTerrain(Terrain newTerrain)
    {
        terrain = newTerrain;

        if (terrain != null)
        {
            terrainBounds = GetTerrainBounds();
            ClampToBounds();
        }
    }

    public void TeleportToPosition(Vector3 worldPosition)
    {
        targetPosition.x = worldPosition.x;
        targetPosition.z = worldPosition.z;

        ClampToBounds();

        transform.position = new Vector3(
            targetPosition.x,
            transform.position.y,
            targetPosition.z
        );
    }
    
}