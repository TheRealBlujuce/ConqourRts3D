using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tower Settings")]
    [SerializeField] private float attackRange = 12f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private int damage = 10;

    [Header("Targeting")]
    [SerializeField] private float targetSearchInterval = 0.2f;
    [SerializeField] private LayerMask targetLayers = ~0;

    [Header("Faction")]
    [SerializeField] private Race faction;

    // Arrow Models
    [Header("Arrow Models")]
    [SerializeField] private GameObject humanArrowPrefab;
    [SerializeField] private Vector3 humanArrowRotationOffset;

    [SerializeField] private GameObject orcArrowPrefab;
    [SerializeField] private Vector3 orcArrowRotationOffset;

    [SerializeField] private GameObject undeadArrowPrefab;
    [SerializeField] private Vector3 undeadArrowRotationOffset;

    [SerializeField] private GameObject elfArrowPrefab;
    [SerializeField] private Vector3 elfArrowRotationOffset;

    // Arrow Settings
    [Header("Arrow Settings")]
    [SerializeField] private Transform arrowSpawnPoint;

    [Tooltip("Global scale multiplier applied to all arrow models.")]
    [SerializeField] private float arrowScale = 1f;

    [Tooltip("How long the visual arrow takes to reach its target.")]
    [SerializeField] private float arrowTravelTime = 0.35f;

    [Tooltip("Maximum height added to the arrow's arc.")]
    [SerializeField] private float arrowArcHeight = 2f;

    private GameObject currentTarget;

    private float attackTimer;
    private float searchTimer;

    private void Update()
    {
        attackTimer -= Time.deltaTime;
        searchTimer -= Time.deltaTime;

        // ==========================================
        // CHECK CURRENT TARGET
        // ==========================================

        if (!IsValidTarget(currentTarget))
        {
            currentTarget = null;
        }

        // ==========================================
        // SEARCH FOR NEAREST ENEMY
        // ==========================================

        if (currentTarget == null && searchTimer <= 0f)
        {
            searchTimer = targetSearchInterval;

            currentTarget = FindNearestEnemy();
        }

        if (currentTarget == null)
            return;

        // ==========================================
        // ATTACK
        // ==========================================

        if (attackTimer <= 0f)
        {
            FireAtTarget();

            attackTimer = attackCooldown;
        }
    }

    // ==========================================
    // FIND NEAREST ENEMY
    // ==========================================

    private GameObject FindNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            attackRange,
            targetLayers
        );

        GameObject nearestEnemy = null;
        float nearestDistanceSqr = Mathf.Infinity;

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

            // Towers ONLY target Enemy faction.
            if (damageable.Team != CombatTeam.Enemy)
                continue;

            if (damageable.IsDead)
                continue;

            MonoBehaviour targetBehaviour =
                damageable as MonoBehaviour;

            if (targetBehaviour == null)
                continue;

            GameObject targetObject =
                targetBehaviour.gameObject;

            Vector3 difference =
                targetObject.transform.position -
                transform.position;

            float distanceSqr =
                difference.sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestEnemy = targetObject;
            }
        }

        return nearestEnemy;
    }

    // ==========================================
    // FIRE
    // ==========================================

    private void FireAtTarget()
    {

        GameObject arrowPrefab = GetArrowPrefab();

        if (currentTarget == null ||
            arrowSpawnPoint == null ||
            arrowPrefab == null)
        {
            return;
        }

        IDamageable damageable =
            currentTarget.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable =
                currentTarget.GetComponentInParent<IDamageable>();
        }

        if (damageable == null ||
            damageable.IsDead ||
            damageable.Team != CombatTeam.Enemy)
        {
            currentTarget = null;
            return;
        }

        // ==========================================
        // RAYCAST
        // ==========================================

        Vector3 targetPosition =
            GetTargetPosition(currentTarget);

        Vector3 direction =
            targetPosition - arrowSpawnPoint.position;

        float distance = direction.magnitude;

        RaycastHit hit;

        if (Physics.Raycast(
            arrowSpawnPoint.position,
            direction.normalized,
            out hit,
            distance,
            targetLayers))
        {
            IDamageable hitDamageable =
                hit.collider.GetComponent<IDamageable>();

            if (hitDamageable == null)
            {
                hitDamageable =
                    hit.collider.GetComponentInParent<IDamageable>();
            }

            // Something else blocked the shot.
            if (hitDamageable != damageable)
                return;
        }

        // ==========================================
        // CREATE VISUAL ARROW
        // ==========================================

        GameObject arrowObject = Instantiate(arrowPrefab, arrowSpawnPoint.position, Quaternion.identity);
        arrowObject.transform.localScale *= arrowScale;
        
        TowerArrow arrow = arrowObject.GetComponent<TowerArrow>();

        // Automatically add TowerArrow if the model
        // doesn't already have one.
        if (arrow == null)
        {
            arrow = arrowObject.AddComponent<TowerArrow>();
        }

        arrow.Initialize(currentTarget, damage, arrowTravelTime, arrowArcHeight, GetArrowRotationOffset());
    }

    // ==========================================
    // TARGET VALIDATION
    // ==========================================

    private bool IsValidTarget(GameObject target)
    {
        if (target == null)
            return false;

        IDamageable damageable =
            target.GetComponent<IDamageable>();

        if (damageable == null)
        {
            damageable =
                target.GetComponentInParent<IDamageable>();
        }

        if (damageable == null)
            return false;

        if (damageable.IsDead)
            return false;

        if (damageable.Team != CombatTeam.Enemy)
            return false;

        float distanceSqr =
            (target.transform.position - transform.position)
            .sqrMagnitude;

        return distanceSqr <= attackRange * attackRange;
    }

    // ==========================================
    // TARGET POSITION
    // ==========================================

    private Vector3 GetTargetPosition(GameObject target)
    {
        Collider targetCollider =
            target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return target.transform.position;
    }

    private GameObject GetArrowPrefab()
    {
        switch (faction)
        {
            case Race.Human:
                return humanArrowPrefab;

            case Race.Orc:
                return orcArrowPrefab;

            case Race.Undead:
                return undeadArrowPrefab;

            case Race.Elf:
                return elfArrowPrefab;

            default:
                Debug.LogWarning(
                    $"{gameObject.name} has an invalid faction."
                );

                return null;
        }
    }

    public void SetFaction(Race newFaction)
    {
        faction = newFaction;
    }

    public Race GetFaction()
    {
        return faction;
    }

    private Vector3 GetArrowRotationOffset()
    {
        switch (faction)
        {
            case Race.Human:
                return humanArrowRotationOffset;

            case Race.Orc:
                return orcArrowRotationOffset;

            case Race.Undead:
                return undeadArrowRotationOffset;

            case Race.Elf:
                return elfArrowRotationOffset;

            default:
                return Vector3.zero;
        }
    }

    // ==========================================
    // DEBUG RANGE
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );
    }
}