using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public enum UnitState { Idle, Moving, Attacking, Gathering, Hunting }

[RequireComponent(typeof(NavMeshAgent), typeof(UnitStats))]
public class UnitController : MonoBehaviour, ISelectable
{
    [SerializeField] private NavMeshAgent agent;
    private Camera mainCamera;
    private UnitStats unitStats;
    private UnitGatherer unitGatherer; // New reference to UnitGatherer

    [SerializeField] private UnitState currentState = UnitState.Idle;

    [Header("Selection")]
    public GameObject selectionIndicator;
    public float fadeSpeed = 3f;
    public float hoverAlpha = 0.3f;
    private SpriteRenderer selectionRenderer;
    private float targetAlpha = 0f;
    private bool isHovered = false;
    [SerializeField] private bool isSelected = false;
    public bool IsSelected => isSelected;
    public SelectableType SelectableType => SelectableType.Unit;

    [Header("Unit Settings")]
    public float rotationAmount = 0.5f;

    [Header("Gathering Settings")]
    public float minDistanceToResourceNode = 1.5f;
    public float minDistanceToDropOff = 1.5f;
    [SerializeField] private ResourceNode currentNode;

    [Header("References")]
    [SerializeField] private Animator unitAnimator;

    private UnitMilitary unitMilitary;

    [Header("Attack Move")]
    [SerializeField] private bool isAttackMoveTargeting = false;
    [SerializeField] private bool isAttackMoving = false;
    [SerializeField] private float enemySearchRadius = 8f;
    [SerializeField] private float enemySearchInterval = 0.25f;

    private float enemySearchTimer = 0f;
    private Vector3 attackMoveDestination;

    public bool canMove = true;
    public GameObject currentTarget;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        unitStats = GetComponent<UnitStats>();
        unitGatherer = GetComponent<UnitGatherer>();
        unitMilitary = GetComponent<UnitMilitary>();
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
        // HandleSelectionInput();
        HandleMovement();
        HandleAttackMove();

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

    private void HandleMovement()
    {
        if (!isSelected || !canMove)
            return;

        // Attack Move owns the next right-click.
        if (isAttackMoveTargeting)
            return;

        if (Input.GetMouseButtonDown(1)) // Right-click
        {

            CancelAttackMove();

            if (unitMilitary != null && unitMilitary.IsAttacking())
            {
                unitMilitary.StopAttacking();
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // ----------------------------------------
                // CHECK FOR ATTACKABLE TARGET
                // ----------------------------------------

                IDamageable damageable = hit.collider.GetComponent<IDamageable>();

                if (damageable == null)
                {
                    damageable =
                        hit.collider.GetComponentInParent<IDamageable>();
                }


                // ==========================================
                // RIGHT-CLICK ENEMY
                // ==========================================

                if (damageable != null && unitMilitary != null)
                {
                    MonoBehaviour targetBehaviour =
                        damageable as MonoBehaviour;

                    if (targetBehaviour != null)
                    {
                        GameObject targetObject =
                            targetBehaviour.gameObject;


                        // Only attack hostile teams
                        if (targetObject != gameObject &&
                            damageable.Team != unitStats.Team &&
                            damageable.Team != CombatTeam.Neutral)
                        {
                            CancelAttackMove();

                            unitMilitary.StartAttacking(
                                targetObject
                            );

                            return;
                        }
                    }
                }

                // GATHERING
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

    #region Attack Movement

        private void HandleAttackMove()
        {
            if (!isSelected || !canMove || unitMilitary == null)
                return;


            // ==========================================
            // ENTER ATTACK MOVE TARGETING MODE
            // ==========================================

            if (Input.GetKeyDown(KeyCode.R))
            {
                // Don't trigger attack move when using Shift + R
                if (!Input.GetKey(KeyCode.LeftShift) &&
                    !Input.GetKey(KeyCode.RightShift))
                {
                    isAttackMoveTargeting = true;

                    Debug.Log($"{gameObject.name}: Choose attack move destination.");
                }
            }


            // ==========================================
            // WAIT FOR RIGHT CLICK DESTINATION
            // ==========================================

            if (isAttackMoveTargeting)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    Ray ray =
                        mainCamera.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        StartAttackMove(hit.point);

                        isAttackMoveTargeting = false;
                    }
                }

                // Don't do normal attack-move processing yet.
                return;
            }


            // ==========================================
            // NOT CURRENTLY ATTACK MOVING
            // ==========================================

            if (!isAttackMoving)
                return;


            // ==========================================
            // CURRENTLY FIGHTING
            // ==========================================

            if (unitMilitary.IsAttacking())
                return;


            // ==========================================
            // SEARCH FOR ENEMIES
            // ==========================================

            enemySearchTimer -= Time.deltaTime;

            if (enemySearchTimer <= 0f)
            {
                enemySearchTimer = enemySearchInterval;

                GameObject enemy = FindNearestEnemy();

                if (enemy != null)
                {
                    unitMilitary.StartAttacking(enemy);

                    return;
                }
            }


            // ==========================================
            // RESUME ATTACK MOVE
            // ==========================================

            if (!agent.hasPath && !agent.pathPending)
            {
                agent.isStopped = false;
                agent.SetDestination(attackMoveDestination);

                SetState(UnitState.Moving);
            }


            // ==========================================
            // DESTINATION REACHED
            // ==========================================

            if (!agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance)
            {
                isAttackMoving = false;

                SetState(UnitState.Idle);
            }
        }

        private void StartAttackMove(Vector3 destination)
        {
            // Stop whatever we were previously attacking.
            if (unitMilitary.IsAttacking())
            {
                unitMilitary.StopAttacking();
            }

            // Workers stop gathering.
            if (unitGatherer != null)
            {
                unitGatherer.StopGathering();
            }

            isAttackMoving = true;

            attackMoveDestination = destination;

            enemySearchTimer = 0f;

            agent.isStopped = false;
            agent.SetDestination(attackMoveDestination);

            SetState(UnitState.Moving);
        }

        private void CancelAttackMove()
        {
            isAttackMoving = false;
            isAttackMoveTargeting = false;
        }

        private GameObject FindNearestEnemy()
        {
            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                enemySearchRadius
            );

            GameObject nearestEnemy = null;
            float nearestDistance = Mathf.Infinity;

            foreach (Collider hit in hits)
            {
                IDamageable damageable =
                    hit.GetComponent<IDamageable>();

                if (damageable == null)
                {
                    damageable =
                        hit.GetComponentInParent<IDamageable>();
                }

                if (damageable == null)
                    continue;


                MonoBehaviour targetBehaviour =
                    damageable as MonoBehaviour;

                if (targetBehaviour == null)
                    continue;


                GameObject targetObject =
                    targetBehaviour.gameObject;


                // Don't detect ourselves
                if (targetObject == gameObject)
                    continue;


                // Ignore dead targets
                if (damageable.IsDead)
                    continue;


                // Ignore friendly units/buildings
                if (damageable.Team == unitStats.Team)
                    continue;


                // Ignore neutral objects
                if (damageable.Team == CombatTeam.Neutral)
                    continue;


                float distance = Vector3.Distance(
                    transform.position,
                    targetObject.transform.position
                );


                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = targetObject;
                }
            }


            return nearestEnemy;
        }

    #endregion


    private void RotateTowardsMovementDirection()
    {
        if (unitStats.unitType == UnitType.Worker){
            ResourceNode node = unitGatherer.GetCurrentNode();

            if (currentState == UnitState.Gathering && node != null)
            {
                Vector3 directionToNode = node.transform.position - transform.position;
                directionToNode.y = 0;

                if (directionToNode.sqrMagnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(directionToNode);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationAmount * Time.deltaTime);
                }
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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationAmount* Time.deltaTime);
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

    public void Select()
    {
        isSelected = true;

        if (selectionIndicator != null &&
            selectionRenderer != null)
        {
            Color c = selectionRenderer.color;
            c.a = 1f;

            selectionRenderer.color = c;
            selectionIndicator.SetActive(true);
        }
    }

    public void Deselect()
    {
        isSelected = false;
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
