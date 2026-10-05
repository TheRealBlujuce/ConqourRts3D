using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{

    [Header("Building Cost")]
    public int goldCost;
    public int lumberCost;
    public int foodCost;

    [Header("Placement Settings")]
    public float gridSize = 1f;

    [Tooltip("The fixed Y level used for all buildings.")]
    public float groundY = 0f;

    public Material canPlaceMat;
    public Material cannotPlaceMat;

    [Tooltip("Units, buildings, resources, trees, etc.")]
    public LayerMask blockingLayers;

    public int threatAmount = 10;

    [Header("References")]
    public GameObject buildingPrefab;
    private SelectionManager selectionManager;  

    private Renderer placerRenderer;
    private BuildingStats playerBase;

    private bool canPlaceHere = false;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        placerRenderer = GetComponentInChildren<Renderer>();

        // Deselect everything when building placement begins.
        selectionManager = FindFirstObjectByType<SelectionManager>();

        if (selectionManager != null)
        {
            selectionManager.SetBuildingPlacementActive(true);
        }

        // Find the player's base.
        BuildingStats[] allBases =
            FindObjectsByType<BuildingStats>(FindObjectsSortMode.None);

        foreach (BuildingStats b in allBases)
        {
            if (b.isPlayerBase)
            {
                playerBase = b;
                break;
            }
        }

        if (playerBase == null)
        {
            Debug.LogError(
                "No player base found with isPlayerBase = true!"
            );
        }

        transform.localRotation = Quaternion.Euler(0f, 128f, 0f);
    }

    private void Update()
    {
        FollowMouse();
        CheckPlacementValidity();

        if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
            return;
        }

        if (canPlaceHere && Input.GetMouseButtonDown(0))
        {
            PlaceBuilding();
        }
    }

    private void CancelPlacement()
    {
        Destroy(gameObject);
    }

    // ==========================================
    // FOLLOW MOUSE
    // ==========================================

    private void FollowMouse()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Create an invisible horizontal plane at ground level.
        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, groundY, 0f)
        );

        if (!groundPlane.Raycast(ray, out float distance))
            return;

        Vector3 hitPoint = ray.GetPoint(distance);

        // Snap X and Z to grid.
        float snappedX =
            Mathf.Round(hitPoint.x / gridSize) * gridSize;

        float snappedZ =
            Mathf.Round(hitPoint.z / gridSize) * gridSize;

        transform.position = new Vector3(
            snappedX,
            groundY,
            snappedZ
        );
    }

    // ==========================================
    // PLACEMENT VALIDITY
    // ==========================================

    private void CheckPlacementValidity()
    {
        if (playerBase == null)
        {
            canPlaceHere = false;
            SetMaterial(false);
            return;
        }

        // ------------------------------------------
        // Town radius
        // ------------------------------------------

        Vector3 placerPosition = transform.position;
        Vector3 basePosition = playerBase.transform.position;

        // Ignore Y when checking town radius.
        placerPosition.y = 0f;
        basePosition.y = 0f;

        // ------------------------------------------
        // Collision check
        // ------------------------------------------

        Collider[] hits = Physics.OverlapBox(
            transform.position,
            transform.localScale / 2f,
            transform.rotation,
            blockingLayers
        );

        if (hits.Length > 0)
        {
            canPlaceHere = false;
            SetMaterial(false);
            return;
        }

        canPlaceHere = true;
        SetMaterial(true);
    }

    // ==========================================
    // MATERIAL
    // ==========================================

    private void SetMaterial(bool canPlace)
    {
        if (placerRenderer == null)
            return;

        placerRenderer.material =
            canPlace ? canPlaceMat : cannotPlaceMat;
    }

    // ==========================================
    // PLACE BUILDING
    // ==========================================

    private void PlaceBuilding()
    {
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("No ResourceManager found!");
            return;
        }

        // Make sure the player can still afford the building.
        if (!ResourceManager.Instance.HasResources(
            goldCost,
            lumberCost,
            foodCost))
        {
            Debug.Log("Not enough resources to build!");
            return;
        }

        // Spend resources only when construction is confirmed.
        ResourceManager.Instance.SpendResources(
            goldCost,
            lumberCost,
            foodCost
        );

        GameObject newBuilding = Instantiate(
            buildingPrefab,
            transform.position,
            transform.rotation
        );

        if (newBuilding.GetComponent<BuildingStats>().buildingType == BuildingType.Tower)
        {
            Tower tower = newBuilding.GetComponent<Tower>();
            tower.SetFaction(GameManager.Instance.playerRace);
            TowerManager.Instance?.RegisterTower();
        }

        InstancedTreeClearer treeClearer = newBuilding.GetComponent<InstancedTreeClearer>();

        if (treeClearer != null)
        {
            treeClearer.ClearTrees();
        }

        ThreatManager.Instance.AddThreat(threatAmount);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (selectionManager != null)
        {
            selectionManager.SetBuildingPlacementActive(false);
        }
    }

    // ==========================================
    // GIZMOS
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            transform.localScale
        );
    }
}