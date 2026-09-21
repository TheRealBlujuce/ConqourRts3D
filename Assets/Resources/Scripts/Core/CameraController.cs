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

    [Header("Bounds Settings")]
    [SerializeField] public BoxCollider cameraBounds;

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

        GameObject playerBase =
            GameObject.FindGameObjectWithTag(playerBaseTag);

        if (playerBase != null)
        {
            targetPosition =
                playerBase.transform.position + initialOffset;

            transform.position = new Vector3(
                targetPosition.x,
                transform.position.y,
                targetPosition.z
            );
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
        if (cameraBounds == null)
            return;

        Bounds bounds = cameraBounds.bounds;

        float tiltRad =
            Mathf.Deg2Rad * transform.eulerAngles.x;

        // Orthographic camera dimensions
        float halfViewHeight = targetOrthoSize;
        float halfViewWidth =
            targetOrthoSize * cam.aspect;

        // Camera footprint on the ground
        float xExtent = halfViewWidth;

        float zExtent =
            Mathf.Abs(Mathf.Sin(tiltRad))
            * halfViewHeight;


        // Keep the visible camera area inside the cube.
        float minX =
            bounds.min.x + xExtent;

        float maxX =
            bounds.max.x - xExtent;

        float minZ =
            bounds.min.z + zExtent;

        float maxZ =
            bounds.max.z - zExtent;


        // Protect against the camera view becoming
        // larger than the bounds.
        if (minX > maxX)
        {
            targetPosition.x =
                bounds.center.x;
        }
        else
        {
            targetPosition.x =
                Mathf.Clamp(
                    targetPosition.x,
                    minX,
                    maxX
                );
        }


        if (minZ > maxZ)
        {
            targetPosition.z =
                bounds.center.z;
        }
        else
        {
            targetPosition.z =
                Mathf.Clamp(
                    targetPosition.z,
                    minZ,
                    maxZ
                );
        }
    }

    public void SetCameraBounds(
        BoxCollider newBounds
    )
    {
        cameraBounds = newBounds;

        ClampToBounds();
    }
}