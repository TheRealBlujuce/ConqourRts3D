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
	public float townRadius = 64f;

	public bool isPlayerBase;

    [Header("UI")]
    public Image healthBar; // Assign in inspector

    [Header("Tree Clearing")]
    public float clearRadius = 32f;

    [Header("Selection Visual")]
    public GameObject selectionIndicator;
	private float targetAlpha = 0f;
	public float fadeSpeed = 3f; // Adjust for faster/slower fade
    public Color selectedColor = new Color(255f, 255f, 255f, 1f);

	private SpriteRenderer selection;

    private bool isSelected = false;
    private bool isHovered = false;

	private PopulationManager populationManager;
    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();

        ClearNearbyTrees();

		populationManager = FindFirstObjectByType<PopulationManager>();
		populationManager.RecalculateMaxPopulation();

        if (selectionIndicator != null)
        {
            selection = selectionIndicator.GetComponent<SpriteRenderer>();
            selection.color = selectedColor;
			selectedColor.a = 0;
            selectionIndicator.SetActive(false);
        }
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

	private void Update()
	{
		if (selection == null) return;

		if (isSelected)
			targetAlpha = 1f;
		else if (isHovered)
			targetAlpha = 0.5f;
		else
			targetAlpha = 0f;

		Color currentColor = selection.color;
		float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);

		currentColor.a = newAlpha;
		selection.color = currentColor;

		if (newAlpha <= 0.01f) // small threshold instead of zero for float safety
		{
			selectionIndicator.SetActive(false);
		}
		else if (!selectionIndicator.activeSelf)
		{
			selectionIndicator.SetActive(true);
		}
	}
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // If selected, force indicator active and full alpha immediately
        if (isSelected)
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(true);
                selectedColor.a = 1f;
            }
        }
    }

    // Mouse hover detection (requires collider)
    private void OnMouseEnter()
    {
        isHovered = true;

        // If not selected, activate indicator to start fade-in
        if (!isSelected && selectionIndicator != null && !selectionIndicator.activeSelf)
        {
            selectionIndicator.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        isHovered = false;
    }

    public void TakeDamage(float amount)
    {
        float effectiveDamage = Mathf.Max(0, amount - armor);
        currentHealth -= effectiveDamage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, clearRadius);
    }
}
