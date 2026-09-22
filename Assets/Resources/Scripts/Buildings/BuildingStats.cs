using UnityEngine;
using UnityEngine.UI;

public class BuildingStats : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float armor = 10f;
	public int populationProvided = 16;
	public bool providesPopulation;
    public float noResourceRadius = 20f;
    public float resourceRadius = 40f;

	public bool isPlayerBase;

    [Header("Tree Clearing")]
    public float clearRadius = 32f;

	private PopulationManager populationManager;
    private void Start()
    {
        currentHealth = maxHealth;

        ClearNearbyTrees();

		populationManager = FindFirstObjectByType<PopulationManager>();
		populationManager.RecalculateMaxPopulation();

    }

    private void ClearNearbyTrees()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, clearRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Tree"))
            {
                Destroy(col.gameObject);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, clearRadius);

        // Gizmos.color = Color.red;
        // Gizmos.DrawWireSphere(transform.position, noResourceRadius);

        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, resourceRadius);
    }
}
