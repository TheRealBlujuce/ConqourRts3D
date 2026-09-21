using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BuildingController : MonoBehaviour, ISelectable
{
    [Header("Selection")]
    public GameObject selectionIndicator;
	private float targetAlpha = 0f;
    public float hoverAlpha = 0.3f;
	public float fadeSpeed = 3f; // Adjust for faster/slower fade
    public Color selectedColor = new Color(255f, 255f, 255f, 1f);
    private SpriteRenderer selectionRenderer;
    public Image healthBar; // Assign in inspector
    private bool isHovered = false;

    [SerializeField] private bool isSelected = false;
    public bool IsSelected => isSelected;
    public SelectableType SelectableType => SelectableType.Building;

    [Header("References")]
    private BuildingStats buildingStats;

    private void Start()
    {
        UpdateHealthBar();
        buildingStats = GetComponent<BuildingStats>();

        if (selectionIndicator != null)
        {
            selectionRenderer = selectionIndicator.GetComponent<SpriteRenderer>();
            selectionRenderer.color = selectedColor;
			selectedColor.a = 0;
            selectionIndicator.SetActive(false);
        }
    }

    public void Select()
    {
        isSelected = true;

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

    public void Deselect()
    {
        isSelected = false;

        // Hide building selection indicator
        // Hide building UI
    }

    public void TakeDamage(float amount)
    {


        float effectiveDamage = Mathf.Max(0, amount - buildingStats.armor);
        buildingStats.currentHealth -= effectiveDamage;
        buildingStats.currentHealth = Mathf.Clamp(buildingStats.currentHealth, 0, buildingStats.maxHealth);

        UpdateHealthBar();

        if (buildingStats.currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = buildingStats.currentHealth / buildingStats.maxHealth;
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }

    private void UpdateSelectionFade()
    {
        if (selectionRenderer == null) return;

        // Determine target alpha
        if (isSelected)
            targetAlpha = 1f;
        else if (isHovered)
            targetAlpha = hoverAlpha;
        else
            targetAlpha = 0f;

        // Smoothly interpolate alpha
        Color currentColor = selectionRenderer.color;
        float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);

        // Apply color
        currentColor.a = newAlpha;
        selectionRenderer.color = currentColor;

        // Manage object active state
        if (newAlpha <= 0.01f)
        {
            if (selectionIndicator.activeSelf)
                selectionIndicator.SetActive(false);
        }
        else
        {
            if (!selectionIndicator.activeSelf)
                selectionIndicator.SetActive(true);
        }
    }

    private void Update()
    {
        UpdateSelectionFade();
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

}
