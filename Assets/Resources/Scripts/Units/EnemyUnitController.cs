using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(UnitStats))]
[RequireComponent(typeof(EnemyUnitMilitary))]
public class EnemyUnitController : MonoBehaviour, ISelectable
{
    [Header("References")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator unitAnimator;

    private UnitStats unitStats;
    private EnemyUnitMilitary unitMilitary;

    // ==========================================
    // AI SETTINGS
    // ==========================================

    [Header("AI Settings")]
    [SerializeField] private float targetSearchRadius = 12f;
    [SerializeField] private float targetSearchInterval = 0.25f;
    private readonly Collider[] targetSearchResults = new Collider[32];
    
    private float targetSearchTimer;

    private GameObject playerBase;
    public Transform unitBody;

    private Vector3 currentDestination;
    private bool hasMoveDestination = false;

    [Header("Movement Settings")]
    [SerializeField] private float repathDistance = 1f;

    // ==========================================
    // UNIT ROLE
    // ==========================================

    [Header("Unit Role")]
    public UnitType UnitRole => unitStats.unitType;

    // ==========================================
    // STATE
    // ==========================================

    [SerializeField] private UnitState currentState = UnitState.Idle;

    // ==========================================
    // SELECTION
    // ==========================================

    [Header("Selection")]
    public GameObject selectionIndicator;
    public Image healthBar;
    public Image healthBarBack;

    public float fadeSpeed = 3f;
    public float hoverAlpha = 0.3f;

    private SpriteRenderer selectionRenderer;

    private float targetAlpha;
    private bool isHovered;
    private bool isSelected;

    public bool IsSelected => isSelected;

    public SelectableType SelectableType =>
        SelectableType.Unit;

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        unitStats = GetComponent<UnitStats>();
        unitMilitary = GetComponent<EnemyUnitMilitary>();

        if (selectionIndicator != null)
        {
            selectionRenderer =
                selectionIndicator.GetComponent<SpriteRenderer>();

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

        // Make absolutely sure this is an enemy.
        unitStats.combatTeam = CombatTeam.Enemy;
        unitStats.isPlayerUnit = false;

         // Spread searches across different frames.
        targetSearchTimer = Random.Range(0f, targetSearchInterval);
        
        FindPlayerBase();

        MoveTowardPlayerBase();
    }

    private void Update()
    {
        HandleAI();

        RotateTowardsMovementDirection();

        UpdateAnimator();

        UpdateHealthBar();
        UpdateSelectionFade();
    }

    // ==========================================
    // MAIN AI
    // ==========================================

    private void HandleAI()
    {
        targetSearchTimer -= Time.deltaTime;

        if (targetSearchTimer <= 0f)
        {
            targetSearchTimer = targetSearchInterval;
            SearchForPriorityTarget();
        }

        // Combat owns movement while attacking.
        if (unitMilitary.IsAttacking())
            return;

        // We lost/finished our target.
        // Resume our main objective if we aren't already moving.
        if (!hasMoveDestination || !agent.hasPath)
        {
            MoveTowardPlayerBase();
        }
    }
    // ==========================================
    // PLAYER BASE
    // ==========================================

    private void FindPlayerBase()
    {
        BuildingStats[] buildings =
            FindObjectsByType<BuildingStats>(
                FindObjectsSortMode.None
            );

        foreach (BuildingStats building in buildings)
        {
            if (!building.isPlayerBase)
                continue;

            if (building.Team == unitStats.Team)
                continue;

            if (building.Team == CombatTeam.Neutral)
                continue;

            if (building.IsDead)
                continue;

            playerBase = building.gameObject;
            return;
        }

        playerBase = null;

        Debug.LogWarning(
            $"{name} could not find an enemy PlayerBase."
        );
    }

    private void MoveTowardPlayerBase()
    {
        if (playerBase == null)
        {
            FindPlayerBase();

            if (playerBase == null)
                return;
        }

        IDamageable baseDamageable =
            playerBase.GetComponent<IDamageable>();

        if (baseDamageable == null)
        {
            baseDamageable =
                playerBase.GetComponentInParent<IDamageable>();
        }

        if (baseDamageable == null || baseDamageable.IsDead)
        {
            playerBase = null;
            return;
        }

        GameObject currentTarget =
            unitMilitary.GetTargetGameObject();

        // Don't restart the same attack command.
        if (currentTarget != playerBase)
        {
            unitMilitary.StartAttacking(playerBase);
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (!agent.isOnNavMesh)
            return;

        if (!NavMesh.SamplePosition(
            destination,
            out NavMeshHit hit,
            2f,
            NavMesh.AllAreas))
        {
            return;
        }

        // Don't issue the same path request repeatedly.
        if (hasMoveDestination)
        {
            float differenceSqr =
                (currentDestination - hit.position).sqrMagnitude;

            if (differenceSqr < repathDistance * repathDistance)
                return;
        }

        currentDestination = hit.position;
        hasMoveDestination = true;

        agent.isStopped = false;
        agent.SetDestination(currentDestination);

        SetState(UnitState.Moving);
    }

    private void StopMoving()
    {
        hasMoveDestination = false;

        if (agent.isOnNavMesh && agent.hasPath)
        {
            agent.ResetPath();
        }

        agent.isStopped = true;
    }

    // ==========================================
    // PRIORITY TARGET SEARCH
    // ==========================================

    private void SearchForPriorityTarget()
    {
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, targetSearchRadius, targetSearchResults);

        GameObject bestTarget = null;
        int bestPriority = int.MinValue;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = targetSearchResults[i];
            IDamageable damageable = hit.GetComponent<IDamageable>();

            if (damageable == null)
            {
                damageable = hit.GetComponentInParent<IDamageable>();
            }

            if (damageable == null)
                continue;

            // Ignore dead targets.
            if (damageable.IsDead)
                continue;

            // Ignore anything on our team.
            if (damageable.Team == unitStats.Team)
                continue;

            // Ignore neutral objects.
            if (damageable.Team == CombatTeam.Neutral)
                continue;

            MonoBehaviour targetBehaviour = damageable as MonoBehaviour;

            if (targetBehaviour == null)
                continue;

            GameObject targetObject = targetBehaviour.gameObject;

            if (targetObject == gameObject)
                continue;

            int priority =
                GetTargetPriority(targetObject);

            if (priority <= 0)
                continue;

            float distance = (targetObject.transform.position - transform.position).sqrMagnitude;

            // Higher priority wins.
            // If priority is equal, attack the closest.
            if (priority > bestPriority || (priority == bestPriority && distance < bestDistance))
            {
                bestPriority = priority;
                bestDistance = distance;
                bestTarget = targetObject;
            }
        }

        if (bestTarget == null)
            return;

        GameObject currentTarget = unitMilitary.GetTargetGameObject();

        if (currentTarget != bestTarget)
        {
            unitMilitary.StartAttacking(bestTarget);
        }
    }

    private int GetTargetPriority(GameObject target)
    {
        if (target == null)
            return 0;

        // ==========================================
        // UNIT
        // ==========================================

        UnitStats targetUnit =
            target.GetComponent<UnitStats>();

        if (targetUnit == null)
        {
            targetUnit =
                target.GetComponentInParent<UnitStats>();
        }

        if (targetUnit != null)
        {
            // Never target friendly units.
            if (targetUnit.Team == unitStats.Team)
                return 0;

            if (targetUnit.Team == CombatTeam.Neutral)
                return 0;

            switch (targetUnit.unitType)
            {
                // Workers are juicy targets.
                case UnitType.Worker:
                    return 100;

                // Military units are normally lower priority.
                // Retaliation is handled separately.
                case UnitType.BasicMelee:
                    return 30;

                case UnitType.BasicRanged:
                    return 30;

                case UnitType.BasicSupport:
                    return 30;

                case UnitType.Hero:
                    return 30;

                default:
                    return 10;
            }
        }

        // ==========================================
        // BUILDING
        // ==========================================

        BuildingStats targetBuilding =
            target.GetComponent<BuildingStats>();

        if (targetBuilding == null)
        {
            targetBuilding =
                target.GetComponentInParent<BuildingStats>();
        }

        if (targetBuilding != null)
        {
            // Never target friendly buildings.
            if (targetBuilding.Team == unitStats.Team)
                return 0;

            if (targetBuilding.Team == CombatTeam.Neutral)
                return 0;

            switch (targetBuilding.buildingType)
            {
                case BuildingType.Storehouse:
                    return 100;

                case BuildingType.Tower:
                    return 80;

                case BuildingType.Base:
                    return 50;

                case BuildingType.Barracks:
                    return 40;

                case BuildingType.Altar:
                    return 40;

                case BuildingType.House:
                    return 35;

                default:
                    return 10;
            }
        }

        return 0;
    }

    // ==========================================
    // RETALIATION
    // ==========================================

    public void NotifyAttacked(GameObject attacker)
    {
        if (attacker == null)
            return;

        IDamageable damageable =
            attacker.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable =
                attacker.GetComponentInParent<IDamageable>();
        }

        if (damageable == null ||
            damageable.IsDead ||
            damageable.Team != CombatTeam.Player)
        {
            return;
        }

        // Workers / Storehouses remain more important.
        GameObject currentTarget =
            unitMilitary.GetTargetGameObject();

        if (currentTarget != null)
        {
            int currentPriority =
                GetTargetPriority(currentTarget);

            if (currentPriority >= 100)
                return;
        }

        unitMilitary.StartAttacking(attacker);
    }

    // ==========================================
    // MOVEMENT / ANIMATION
    // ==========================================

    private void RotateTowardsMovementDirection()
    {
        Vector3 velocity = agent.desiredVelocity;

        if (velocity.sqrMagnitude > 0.1f)
        {
            Vector3 direction = velocity.normalized;
            direction.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Keep selection indicator locked to its world orientation.
        if (selectionIndicator != null)
        {
            selectionIndicator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }

    private void UpdateAnimator()
    {
        if (unitAnimator == null)
            return;

        bool moving =
            agent.velocity.sqrMagnitude > 0.1f;

        unitAnimator.SetBool("isMoving", moving);
    }

    public void SetState(UnitState newState)
    {
        currentState = newState;
    }

    // ==========================================
    // SELECTION
    // ==========================================

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

        if (!isSelected &&
            selectionIndicator != null &&
            selectionRenderer != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        isHovered = false;
    }

    private void UpdateHealthBar()
    {
        if (healthBar == null || unitStats == null)
            return;

        healthBar.fillAmount = (float)unitStats.health / unitStats.maxHealth;
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

    // ==========================================
    // GETTERS
    // ==========================================

    public NavMeshAgent GetUnitAgent()
    {
        return agent;
    }

    public Animator GetUnitAnimator()
    {
        return unitAnimator;
    }

    public Transform GetUnitBody()
    {
        return unitBody;
    }
}