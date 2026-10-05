using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyUnitController))]
[RequireComponent(typeof(UnitStats))]
public class EnemyUnitMilitary : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private float meleeRange = 2f;
    [SerializeField] private float rangedRange = 8f;
    [SerializeField] private float supportRange = 6f;
    [SerializeField] private float heroRange = 2.5f;

    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseRepathInterval = 0.2f;
    [SerializeField] private float chaseRepathDistance = 0.5f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 720f;

    private Collider targetCollider;

    private IDamageable target;
    private Transform targetTransform;

    private EnemyUnitController controller;
    private UnitStats stats;
    private NavMeshAgent agent;

    private bool isAttacking;

    private float attackTimer;
    private float chaseRepathTimer;

    private Vector3 lastChasePosition;
    private bool hasChasePosition;

    private void Awake()
    {
        controller = GetComponent<EnemyUnitController>();
        stats = GetComponent<UnitStats>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!isAttacking)
            return;

        // ==========================================
        // VALIDATE TARGET
        // ==========================================

        if (targetTransform == null || target == null || target.IsDead)
        {
            StopAttacking();
            return;
        }

        float attackRange = GetAttackRange();

        float distanceSqr = GetDistanceToTargetSqr();

        float attackRangeSqr = attackRange * attackRange;

        // ==========================================
        // CHASE
        // ==========================================

        if (distanceSqr > attackRangeSqr)
        {
            ChaseTarget();

            controller.SetState(UnitState.Moving);

            return;
        }

        // ==========================================
        // IN ATTACK RANGE
        // ==========================================

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        controller.SetState(UnitState.Attacking);

        FaceTarget();

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            PerformRoleAction();

            attackTimer -= attackCooldown;
        }
    }

    // ==========================================
    // CHASING
    // ==========================================

    private void ChaseTarget()
    {
        if (!agent.isOnNavMesh)
            return;

        agent.isStopped = false;

        chaseRepathTimer -= Time.deltaTime;

        // Already have a valid path and it isn't
        // time to reconsider it yet.
        if (chaseRepathTimer > 0f && agent.hasPath)
        {
            return;
        }

        Vector3 targetPosition = GetTargetPosition();

        // If our target hasn't moved enough,
        // keep following our existing path.
        if (hasChasePosition && agent.hasPath)
        {
            float movementSqr = (targetPosition - lastChasePosition).sqrMagnitude;

            float requiredMovementSqr = chaseRepathDistance * chaseRepathDistance;

            if (movementSqr < requiredMovementSqr)
            {
                chaseRepathTimer = chaseRepathInterval;

                return;
            }
        }

        lastChasePosition = targetPosition;
        hasChasePosition = true;

        chaseRepathTimer = chaseRepathInterval;

        agent.SetDestination(targetPosition);
    }

    // ==========================================
    // START ATTACK
    // ==========================================

    public void StartAttacking(GameObject newTarget)
    {
        if (newTarget == null)
            return;

        IDamageable damageable = newTarget.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable = newTarget.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
            return;

        if (damageable.IsDead)
            return;

        if (damageable.Team == stats.Team)
            return;

        if (damageable.Team == CombatTeam.Neutral)
            return;

        MonoBehaviour targetBehaviour = damageable as MonoBehaviour;

        if (targetBehaviour == null)
            return;

        GameObject actualTarget = targetBehaviour.gameObject;

        if (actualTarget == gameObject)
            return;

        // ==========================================
        // ALREADY ATTACKING THIS TARGET
        // ==========================================

        if (isAttacking && targetTransform == targetBehaviour.transform)
        {
            return;
        }

        // ==========================================
        // NEW TARGET
        // ==========================================

        target = damageable;
        targetTransform = targetBehaviour.transform;

        targetCollider = targetBehaviour.GetComponent<Collider>();

        if (targetCollider == null)
        {
            targetCollider = targetBehaviour.GetComponentInChildren<Collider>();
        }

        isAttacking = true;

        // Allows immediate first attack once in range.
        attackTimer = attackCooldown;

        // Force an immediate initial path calculation.
        chaseRepathTimer = 0f;
        hasChasePosition = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;

            Vector3 targetPosition = GetTargetPosition();

            lastChasePosition = targetPosition;

            hasChasePosition = true;

            agent.SetDestination(targetPosition);
        }
    }

    // ==========================================
    // STOP ATTACK
    // ==========================================

    public void StopAttacking()
    {
        if (!isAttacking)
            return;

        isAttacking = false;

        target = null;
        targetTransform = null;
        targetCollider = null;

        attackTimer = 0f;

        chaseRepathTimer = 0f;
        hasChasePosition = false;

        // IMPORTANT:
        // Don't ResetPath here.
        //
        // The EnemyUnitController can immediately
        // give us our next objective.
        if (agent != null &&
            agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        Animator animator = controller.GetUnitAnimator();

        if (animator != null)
        {
            animator.ResetTrigger("isAttacking");
        }

        controller.SetState(UnitState.Idle);
    }

    // ==========================================
    // DISTANCE
    // ==========================================

    private float GetDistanceToTargetSqr()
    {
        Vector3 targetPosition = GetTargetPosition();

        return (targetPosition - transform.position).sqrMagnitude;
    }

    // ==========================================
    // ROLE LOGIC
    // ==========================================

    private float GetAttackRange()
    {
        switch (controller.UnitRole)
        {
            case UnitType.BasicMelee:
                return meleeRange;

            case UnitType.BasicRanged:
                return rangedRange;

            case UnitType.BasicSupport:
                return supportRange;

            case UnitType.Hero:
                return heroRange;

            default:
                return meleeRange;
        }
    }

    private void PerformRoleAction()
    {
        switch (controller.UnitRole)
        {
            case UnitType.BasicMelee:
                PerformMeleeAttack();
                break;

            case UnitType.BasicRanged:
                PerformRangedAttack();
                break;

            case UnitType.BasicSupport:
                PerformSupportAction();
                break;

            case UnitType.Hero:
                PerformHeroAttack();
                break;
        }
    }

    // ==========================================
    // MELEE
    // ==========================================

    private void PerformMeleeAttack()
    {
        PerformDirectAttack();
    }

    // ==========================================
    // RANGED
    // ==========================================

    private void PerformRangedAttack()
    {
        // Projectile later.
        PerformDirectAttack();
    }

    // ==========================================
    // SUPPORT
    // ==========================================

    private void PerformSupportAction()
    {
        // Support behavior later.
        PerformDirectAttack();
    }

    // ==========================================
    // HERO
    // ==========================================

    private void PerformHeroAttack()
    {
        // Hero abilities later.
        PerformDirectAttack();
    }

    // ==========================================
    // DAMAGE
    // ==========================================

    private void PerformDirectAttack()
    {
        if (target == null || target.IsDead)
        {
            return;
        }

        Animator animator =controller.GetUnitAnimator();

        if (animator != null)
        {
            animator.SetTrigger("isAttacking");
        }

        MonoBehaviour targetBehaviour = target as MonoBehaviour;

        if (targetBehaviour != null)
        {
            UnitStats targetStats = targetBehaviour.GetComponent<UnitStats>();

            if (targetStats != null)
            {
                targetStats.NotifyAttacker(gameObject);
            }
        }

        target.TakeDamage(stats.damage);

        if (target == null || target.IsDead)
        {
            StopAttacking();
        }
    }

    // ==========================================
    // ROTATION
    // ==========================================

    private void FaceTarget()
    {
        if (targetTransform == null)
            return;

        Vector3 direction = targetTransform.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    // ==========================================
    // GETTERS
    // ==========================================

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public IDamageable GetTarget()
    {
        return target;
    }

    public GameObject GetTargetGameObject()
    {
        if (targetTransform == null)
            return null;

        return targetTransform.gameObject;
    }

    private Vector3 GetTargetPosition()
    {
        if (targetCollider != null)
        {
            return targetCollider.ClosestPoint(transform.position);
        }

        if (targetTransform != null)
        {
            return targetTransform.position;
        }

        return transform.position;
    }
}