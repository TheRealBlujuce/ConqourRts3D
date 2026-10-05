using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public enum UnitState { Idle, Moving, Attacking, Gathering }

[RequireComponent(typeof(NavMeshAgent), typeof(UnitStats))]
public class UnitController : MonoBehaviour, ISelectable
{
    [SerializeField] private NavMeshAgent agent;
    private Camera mainCamera;
    private UnitStats unitStats;
    private UnitGatherer unitGatherer; // New reference to UnitGatherer

    [SerializeField] private UnitState currentState = UnitState.Idle;

    // selection of units
    [Header("Selection")]
    public GameObject selectionIndicator;
    public Image healthBar;
    public Image healthBarBack;
    public float fadeSpeed = 3f;
    public float hoverAlpha = 0.3f;
    private SpriteRenderer selectionRenderer;
    private float targetAlpha = 0f;
    private bool isHovered = false;
    [SerializeField] private bool isSelected = false;
    public bool IsSelected => isSelected;
    public SelectableType SelectableType => SelectableType.Unit;

    // unit settings
    [Header("Unit Settings")]
    public float rotationAmount = 0.5f;

    [Header("Movement")]
    [SerializeField] private float destinationTolerance = 0.15f;
    [SerializeField] private float minimumMoveDistance = 0.1f;

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
    [SerializeField, Range(0f, 180f)] private float enemySearchAngle = 120f;
    private readonly Collider[] enemySearchResults = new Collider[32];
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

        agent.obstacleAvoidanceType =
            ObstacleAvoidanceType.MedQualityObstacleAvoidance;

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

        CheckMovementArrival();

        RotateTowardsMovementDirection();
        UpdateState();

        UpdateHealthBar();
        UpdateSelectionFade();

    }

    private void UpdateSelectionFade()
    {
        if (selectionRenderer == null)
            return;

        // Determine target alpha
        if (isSelected)
            targetAlpha = 1f;
        else if (isHovered)
            targetAlpha = hoverAlpha;
        else
            targetAlpha = 0f;

        // ==========================================
        // SELECTION INDICATOR FADE
        // ==========================================

        Color selectionColor = selectionRenderer.color;

        float newAlpha = Mathf.Lerp(selectionColor.a,targetAlpha,fadeSpeed * Time.deltaTime);

        selectionColor.a = newAlpha;
        selectionRenderer.color = selectionColor;

        // ==========================================
        // HEALTH BAR FADE
        // ==========================================

        if (healthBar != null && healthBarBack != null)
        {
            Color healthColor = healthBar.color;
            Color backHealthColor = healthBarBack.color;

            healthColor.a = newAlpha;
            healthBar.color = healthColor;

            backHealthColor.a = newAlpha;
            healthBarBack.color = backHealthColor;
        }

        // ==========================================
        // SELECTION INDICATOR VISIBILITY
        // ==========================================

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

    private void UpdateHealthBar()
    {
        if (healthBar == null || unitStats == null)
            return;

        healthBar.fillAmount = (float)unitStats.health / unitStats.maxHealth;
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
                            MoveTo(hit.point);
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
                    MoveTo(hit.point);
                    SetState(UnitState.Moving);
                }
				else
				{
                    MoveTo(hit.point);
                    SetState(UnitState.Moving);
				}
            }
        }
    }

    #region Movement


        public void MoveTo(Vector3 destination)
        {
            if (!canMove)
                return;

            // Make sure the requested position is actually on the NavMesh.
            if (!NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                1.5f,
                NavMesh.AllAreas))
            {
                return;
            }

            float distanceSqr =
                (transform.position - hit.position).sqrMagnitude;

            if (distanceSqr <= minimumMoveDistance * minimumMoveDistance)
            {
                StopMoving();
                return;
            }

            agent.isStopped = false;

            // IMPORTANT:
            // Tell the NavMeshAgent to move.
            // Do NOT call MoveTo() again here.
            agent.SetDestination(hit.position);

            SetState(UnitState.Moving);
        }

        public void StopMoving()
        {
            if (agent.hasPath)
            {
                agent.ResetPath();
            }

            agent.isStopped = true;

            if (currentState == UnitState.Moving)
            {
                SetState(UnitState.Idle);
            }
        }
        private void CheckMovementArrival()
        {
            if (currentState != UnitState.Moving)
                return;

            // Gathering/combat systems control their own movement.
            if (unitMilitary != null && unitMilitary.IsAttacking())
                return;

            if (isAttackMoving)
                return;

            if (HasReachedDestination())
            {
                StopMoving();
            }
        }
        private bool HasReachedDestination()
        {
            if (agent.pathPending)
                return false;

            if (!agent.hasPath)
                return true;

            float arrivalDistance = Mathf.Max(agent.stoppingDistance, destinationTolerance);

            if (agent.remainingDistance > arrivalDistance)
                return false;

            if (agent.velocity.sqrMagnitude > 0.05f)
                return false;

            return true;
        }


    #endregion

    #region Attack Movement

        private void HandleAttackMove()
        {
            if (!canMove || unitMilitary == null)
                return;

            // ==========================================
            // PLAYER INPUT
            // Only selected units can RECEIVE the command.
            // ==========================================

            if (isSelected)
            {
                if (Input.GetKeyDown(KeyCode.R))
                {
                    // Don't trigger attack move with Shift + R.
                    if (!Input.GetKey(KeyCode.LeftShift) &&
                        !Input.GetKey(KeyCode.RightShift))
                    {
                        isAttackMoveTargeting = true;

                        // Debug.Log(
                        //     $"{gameObject.name}: Choose attack move destination."
                        // );
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

                    return;
                }
            }

            // ==========================================
            // ATTACK MOVE AI
            //
            // IMPORTANT:
            // Everything below here runs regardless of
            // whether the unit is currently selected.
            // ==========================================

            if (!isAttackMoving)
                return;

            // ==========================================
            // CURRENTLY FIGHTING
            // ==========================================

            if (unitMilitary.IsAttacking())
                return;

            // ==========================================
            // SEARCH FOR ENEMIES WHILE MOVING
            // ==========================================

            enemySearchTimer -= Time.deltaTime;

            if (enemySearchTimer <= 0f)
            {
                enemySearchTimer = enemySearchInterval;

                GameObject enemy = FindNearestEnemy();

                if (enemy != null)
                {
                    // We found an enemy while attack moving.
                    // Stop the attack-move command permanently.
                    isAttackMoving = false;

                    // Abandon the original destination.
                    if (agent.hasPath)
                    {
                        agent.ResetPath();
                    }

                    // Combat now owns the unit.
                    unitMilitary.StartAttacking(enemy);

                    SetState(UnitState.Attacking);

                    return;
                }
            }

            // ==========================================
            // DESTINATION REACHED
            // ==========================================

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance)
            {
                agent.ResetPath();

                // DO NOT disable isAttackMoving.
                // The unit should continue scanning for enemies
                // after reaching the destination.
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
            MoveTo(attackMoveDestination);

            SetState(UnitState.Moving);
        }

        private void CancelAttackMove()
        {
            isAttackMoving = false;
            isAttackMoveTargeting = false;
        }

        private GameObject FindNearestEnemy()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, enemySearchRadius, enemySearchResults);

            GameObject nearestEnemy = null;
            float nearestDistance = Mathf.Infinity;

            Vector3 destinationDirection = attackMoveDestination - transform.position;
            destinationDirection.y = 0f;

            if (destinationDirection.sqrMagnitude > 0.01f)
                destinationDirection.Normalize();

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = enemySearchResults[i];

                if (hit == null)
                    continue;

                IDamageable damageable = hit.GetComponent<IDamageable>();

                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null)
                    continue;

                MonoBehaviour targetBehaviour = damageable as MonoBehaviour;

                if (targetBehaviour == null)
                    continue;

                GameObject targetObject = targetBehaviour.gameObject;

                if (targetObject == gameObject)
                    continue;

                if (damageable.IsDead)
                    continue;

                if (damageable.Team == unitStats.Team)
                    continue;

                if (damageable.Team == CombatTeam.Neutral)
                    continue;

                Vector3 enemyDirection = targetObject.transform.position - transform.position;
                enemyDirection.y = 0f;

                if (enemyDirection.sqrMagnitude <= 0.01f)
                    continue;

                float angle = Vector3.Angle(destinationDirection, enemyDirection);

                if (angle > enemySearchAngle * 0.5f)
                    continue;

                float distance = enemyDirection.sqrMagnitude;

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
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationAmount * Time.deltaTime);
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
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationAmount* Time.deltaTime);
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
        DropOffPoint nearest = null;
        float closestDist = Mathf.Infinity;

        foreach (DropOffPoint d in  NodeRegistry.Instance.dropOffPoints)
        {
            if (d == null) continue;

            float dist = (d.transform.position - transform.position).sqrMagnitude;
            
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
