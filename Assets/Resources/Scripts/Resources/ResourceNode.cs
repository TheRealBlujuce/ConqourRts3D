using UnityEngine;
using TMPro;

public class ResourceNode : MonoBehaviour
{
    public enum ResourceType { Food, Gold, Lumber }

    [Header("Resource Settings")]
    public ResourceType resourceType;
    public int maxResources = 100;
    private int remainingResources;
    public bool isStartingNode;

    [Header("Harvest Settings")]
    public int resourcePerHarvest = 10;

    [Header("Selection Visual")]
    public GameObject selectionIndicator;
    public float fadeSpeed = 3f; 
    public Color selectedColor = Color.white; 
    [SerializeField] public TextMeshProUGUI resourceCountText;

    private Canvas resourceCanvas;
    private CanvasGroup uiGroup;
    private SpriteRenderer selection;
    private float targetAlpha = 0f;
    private bool isSelected = false;
    private bool isHovered = false;

    private NavUpdater navUpdater;
    private MeshRenderer[] meshRenderers;
    private Camera mainCam;

    private void OnEnable()
    {
        if (NodeRegistry.Instance != null)
        {
            NodeRegistry.Instance.RegisterResourceNode(this);
        }
    }

    private void OnDisable()
    {
        if (NodeRegistry.Instance != null)
        {
            NodeRegistry.Instance.UnregisterResourceNode(this);
        }
    }

    private void Start()
    {
        remainingResources = maxResources;
        navUpdater = FindFirstObjectByType<NavUpdater>();
        mainCam = Camera.main;

        meshRenderers = GetComponentsInChildren<MeshRenderer>(true);

        if (selectionIndicator != null)
        {
            selection = selectionIndicator.GetComponent<SpriteRenderer>();
            uiGroup = selection.GetComponent<CanvasGroup>();
            if (uiGroup != null) uiGroup.alpha = 0f;

            selectedColor.a = 0f; 
            selection.color = selectedColor;
            selectionIndicator.SetActive(false);
        }
    }

    private void Update()
    {
        HandleSelectionFade();
        HandleResourceUI();
        //HandleVisibilityCulling();
    }

    private void HandleSelectionFade()
    {
        if (isHovered) targetAlpha = 1f;
        else targetAlpha = 0f;

        Color currentColor = selection.color;
        float newAlpha = Mathf.Lerp(currentColor.a, targetAlpha, fadeSpeed * Time.deltaTime);
        currentColor.a = newAlpha;

        if (uiGroup != null) uiGroup.alpha = newAlpha;
        selection.color = currentColor;

        if (newAlpha <= 0.01f) selectionIndicator.SetActive(false);
        else if (!selectionIndicator.activeSelf) selectionIndicator.SetActive(true);
    }

    private void HandleResourceUI()
    {
        if (resourceCountText != null)
        {
            resourceCountText.text = remainingResources.ToString();
        }
        else
        {
            resourceCountText = GetComponentInChildren<TextMeshProUGUI>();
            if (resourceCountText != null)
            {
                resourceCanvas = resourceCountText.GetComponentInParent<Canvas>();
                resourceCanvas.worldCamera = Camera.main;
                resourceCanvas.gameObject.SetActive(false);
            }
        }

        if (resourceCanvas != null && (resourceType == ResourceType.Food || resourceType == ResourceType.Gold))
        {
            resourceCanvas.gameObject.SetActive(true);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (isSelected && selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
            if (uiGroup != null) uiGroup.alpha = 1f;
            selectedColor.a = 1f;
            selection.color = selectedColor;
        }
    }

    private void OnMouseEnter() => isHovered = true;
    private void OnMouseExit() => isHovered = false;

    public int Harvest()
    {
        if (remainingResources <= 0) return 0;

        int harvested = Mathf.Min(resourcePerHarvest, remainingResources);
        remainingResources -= harvested;

        if (remainingResources <= 0) OnDepleted();
        return harvested;
    }

    private void OnDepleted()
    {
        navUpdater.UpdateNavMeshInArea();
        Destroy(gameObject);
    }

    public ResourceType GetResourceType() => resourceType;
    public int GetRemainingResources() => remainingResources;

    private void OnTriggerEnter(Collider collision)
    {   
        if (!isStartingNode)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Building"))
                Destroy(gameObject);

            if (gameObject.name.Contains("Tree") &&
                collision.gameObject.layer == LayerMask.NameToLayer("Resource"))
                Destroy(gameObject);
        }
    }
}
