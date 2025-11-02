using UnityEngine;

public class BuildingPlacer : MonoBehaviour
{
    [Header("Placement Settings")]
    public float gridSize = 1f;
    public Material canPlaceMat;
    public Material cannotPlaceMat;
    public LayerMask blockingLayers; // Units, buildings, resources

    [Header("References")]
    public GameObject buildingPrefab;
    private Renderer placerRenderer;

    private BuildingStats playerBase;
    private bool canPlaceHere = false;

    private void Start()
    {
        placerRenderer = GetComponentInChildren<Renderer>();

        // Find the player's base (the one with isPlayerBase = true)
        BuildingStats[] allBases = FindObjectsByType<BuildingStats>(FindObjectsSortMode.None);
        foreach (var b in allBases)
        {
            if (b.isPlayerBase)
            {
                playerBase = b;
                break;
            }
        }

        if (playerBase == null)
        {
            Debug.LogError("No player base found with isPlayerBase = true!");
        }

		transform.localRotation = Quaternion.Euler(0f, 128f, 0f);
    }

    private void Update()
    {
        FollowMouse();

        CheckPlacementValidity();

        if (canPlaceHere && Input.GetMouseButtonDown(0))
        {
            PlaceBuilding();
        }
    }

    private void FollowMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0))
        {
            Vector3 snapPos = new Vector3(
                Mathf.Round(hit.point.x / gridSize) * gridSize,
                hit.point.y,
                Mathf.Round(hit.point.z / gridSize) * gridSize
            );
            transform.position = snapPos;
        }
    }

    private void CheckPlacementValidity()
    {
        if (playerBase == null) return;

        // Check radius from player base
        float dist = Vector3.Distance(transform.position, playerBase.transform.position);
        if (dist > playerBase.townRadius)
        {
            SetMaterial(false);
            canPlaceHere = false;
            return;
        }

        // Check for collisions with units, buildings, resources
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            transform.localScale / 2f,
            Quaternion.identity,
            blockingLayers
        );

        if (hits.Length > 0)
        {
            SetMaterial(false);
            canPlaceHere = false;
        }
        else
        {
            SetMaterial(true);
            canPlaceHere = true;
        }
    }

    private void SetMaterial(bool canPlace)
    {
        if (placerRenderer != null)
        {
            placerRenderer.material = canPlace ? canPlaceMat : cannotPlaceMat;
        }
    }

    private void PlaceBuilding()
    {
        Instantiate(buildingPrefab, transform.position, transform.localRotation);
        Destroy(gameObject); // Destroy placer after placement
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}

