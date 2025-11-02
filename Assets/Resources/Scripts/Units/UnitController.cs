using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public enum UnitState { Idle, Moving, Attacking, Gathering, Hunting }

[RequireComponent(typeof(NavMeshAgent), typeof(UnitStats))]
public class UnitController : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    private Camera mainCamera;
    private UnitStats unitStats;
    private UnitGatherer unitGatherer; // New reference to UnitGatherer

    [SerializeField] private bool isSelected = false;
    [SerializeField] private UnitState currentState = UnitState.Idle;

    [Header("Selection Visual")]
    public GameObject selectionIndicator;
    public float fadeSpeed = 3f;
    public float hoverAlpha = 0.3f;

    private SpriteRenderer selectionRenderer;
    private float targetAlpha = 0f;
    private bool isHovered = false;

    [Header("Unit Settings")]
    public float rotationAmount = 0.5f;

    [Header("Gathering Settings")]
    public float minDistanceToResourceNode = 1.5f;
    public float minDistanceToDropOff = 1.5f;
    [SerializeField] private ResourceNode currentNode;

    [Header("References")]
    [SerializeField] private Animator unitAnimator;

    public bool canMove = true;
    public GameObject currentTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        unitStats = GetComponent<UnitStats>();
        unitGatherer = GetComponent<UnitGatherer>();
        mainCamera = Camera.main;

        if (selectionIndicator != null)
        {
            selectionRenderer = selectionIndicator.GetComponent<SpriteRenderer>();
            if (selectionRenderer != null)
            {
                Color c = selectionRenderer.color;
                c.a = 0f;
                selectionRenderer.color = c;
            }
            selectionIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        agent.speed = unitStats.moveSpeed;
    }

    private void Update()
    {
        HandleSelectionInput();
        HandleMovement();
        RotateTowardsMovementDirection();
        UpdateState();
        UpdateSelectionFade();
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

    private void HandleSelectionInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitController clickedUnit = hit.collider.GetComponent<UnitController>();
                bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                if (clickedUnit != null)
                {
                    if (shiftHeld)
                        clickedUnit.SetSelected(true);
                    else
                    {
                        DeselectAllUnits();
                        clickedUnit.SetSelected(true);
                    }
                }
                else if (!shiftHeld)
                    DeselectAllUnits();
            }
            else if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
                DeselectAllUnits();
        }
    }

    private void HandleMovement()
    {
        if (!isSelected || !canMove) return;

        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                UnitGatherer gatherer = GetComponent<UnitGatherer>();

                ResourceNode clickedNode = hit.collider.GetComponent<ResourceNode>() ?? hit.collider.GetComponentInParent<ResourceNode>();
                DropOffPoint clickedDropOff = hit.collider.GetComponent<DropOffPoint>() ?? hit.collider.GetComponentInParent<DropOffPoint>();

                if (unitStats.unitType == UnitType.Worker && gatherer != null)
                {
                    // Right-click DropOffPoint
                    if (clickedDropOff != null)
                    {
                        if (gatherer.GetCurrentCarriedResources() > 0)
                        {
                            gatherer.MoveToDropOff(clickedDropOff);
                        }
                        else
                        {
                            gatherer.StopGathering();
                            agent.SetDestination(hit.point);
                            SetState(UnitState.Moving);
                        }
                        return;
                    }

                    // Right-click ResourceNode
                    if (clickedNode != null)
                    {
                        DropOffPoint dropOff = FindNearestDropOff();
                        if (dropOff != null)
                        {
                            currentNode = clickedNode;
                            gatherer.StartGathering(currentNode, dropOff);
                        }
                        return;
                    }

                    // Right-click ground
                    gatherer.StopGathering();
                    agent.SetDestination(hit.point);
                    SetState(UnitState.Moving);
                }
				else
				{
                    agent.SetDestination(hit.point);
                    SetState(UnitState.Moving);
				}
            }
        }
    }

    private void RotateTowardsMovementDirection()
    {
        ResourceNode node = unitGatherer.GetCurrentNode();

        if (currentState == UnitState.Gathering && node != null)
        {
            Vector3 directionToNode = node.transform.position - transform.position;
            directionToNode.y = 0;

            if (directionToNode.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToNode);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationAmount);
            }
        }
        else
        {
            Vector3 velocity = agent.desiredVelocity;

            if (velocity.sqrMagnitude > 0.1f)
            {
                Vector3 direction = velocity.normalized;
                direction.y = 0;

                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationAmount);
            }
        }

        if (selectionIndicator != null)
            selectionIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private void UpdateState()
    {
        if (unitAnimator != null)
        {
            bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
            unitAnimator.SetBool("isMoving", isMoving);
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selected && selectionIndicator != null && selectionRenderer != null)
        {
            Color c = selectionRenderer.color;
            c.a = 1f;
            selectionRenderer.color = c;
            selectionIndicator.SetActive(true);
        }
    }

    private void OnMouseEnter()
    {
        isHovered = true;
        if (!isSelected && selectionIndicator != null && selectionRenderer != null)
            selectionIndicator.SetActive(true);
    }

    private void OnMouseExit()
    {
        isHovered = false;
    }

    public void SetState(UnitState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

    private void DeselectAllUnits()
    {
        foreach (UnitController unit in FindObjectsByType<UnitController>(FindObjectsSortMode.None))
            unit.SetSelected(false);
    }

    private DropOffPoint FindNearestDropOff()
    {
        DropOffPoint[] dropOffs = FindObjectsByType<DropOffPoint>(FindObjectsSortMode.None);
        DropOffPoint nearest = null;
        float closestDist = Mathf.Infinity;

        foreach (DropOffPoint d in dropOffs)
        {
            float dist = Vector3.Distance(transform.position, d.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = d;
            }
        }
        return nearest;
    }

    public NavMeshAgent GetUnitAgent() => agent;
    public float GetMinDistanceToResource() => minDistanceToResourceNode;
    public float GetMinDistanceToDropOff() => minDistanceToDropOff;
    public Animator GetUnitAnimator() => unitAnimator;
}
