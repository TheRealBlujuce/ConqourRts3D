using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnitController))]
[RequireComponent(typeof(UnitStats))]
public class UnitMilitary : MonoBehaviour
{
    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;

    private float attackTimer = 0f;

    private IDamageable target;
    private Transform targetTransform;

    private UnitController controller;
    private UnitStats stats;
    private NavMeshAgent agent;

    private bool isAttacking = false;

    private void Awake()
    {
        controller = GetComponent<UnitController>();
        stats = GetComponent<UnitStats>();
    }

    private void Start()
    {
        agent = controller.GetUnitAgent();
    }

    private void Update()
    {
        if (!isAttacking)
            return;

        // Target was destroyed
        if (targetTransform == null)
        {
            StopAttacking();
            return;
        }

        if (target == null || target.IsDead)
        {
            StopAttacking();
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            targetTransform.position
        );

        // Chase target
        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(targetTransform.position);

            controller.GetUnitAnimator().ResetTrigger(
                "isAttacking"
            );

            controller.SetState(UnitState.Moving);

            return;
        }

        // Attack target
        agent.isStopped = true;

        controller.SetState(UnitState.Attacking);

        // Face target
        Vector3 direction = targetTransform.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                5f * Time.deltaTime
            );
        }

        attackTimer += Time.deltaTime;

        if (attackTimer >= attackCooldown)
        {
            PerformAttack();
            attackTimer = 0f;
        }
    }

    public void StartAttacking(GameObject newTarget)
    {
        if (newTarget == null)
            return;

        IDamageable damageable =
            newTarget.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable =
                newTarget.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
            return;


        MonoBehaviour targetBehaviour =
            damageable as MonoBehaviour;

        if (targetBehaviour == null)
            return;


        // Don't attack ourselves
        if (targetBehaviour.gameObject == gameObject)
            return;


        // Don't attack allies
        if (damageable.Team == stats.Team)
        {
            Debug.Log(
                $"{gameObject.name} cannot attack " +
                $"{targetBehaviour.gameObject.name}: same team."
            );

            return;
        }


        // Neutral objects aren't automatically considered enemies.
        if (damageable.Team == CombatTeam.Neutral)
        {
            return;
        }


        target = damageable;
        targetTransform = targetBehaviour.transform;

        isAttacking = true;

        attackTimer = attackCooldown;

        agent.isStopped = false;
        agent.SetDestination(targetTransform.position);
    }

    public void StopAttacking()
    {
        isAttacking = false;
    
        target = null;
        targetTransform = null;

        attackTimer = 0f;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        controller.GetUnitAnimator().ResetTrigger(
			"isAttacking"
		);

        controller.SetState(UnitState.Idle);

    }

    private void PerformAttack()
    {
        if (target == null || target.IsDead)
            return;

        controller.GetUnitAnimator().SetTrigger(
			"isAttacking"
		);

        target.TakeDamage(stats.damage);

        // Later:
        //
        // Archer -> spawn projectile
        // Warrior -> melee animation/event
        //
        // For now damage happens immediately.

        if (target == null || target.IsDead)
        {
            StopAttacking();
        }
    }

    public bool IsAttacking()
    {
        return isAttacking;
    }

    public IDamageable GetTarget()
    {
        return target;
    }
}