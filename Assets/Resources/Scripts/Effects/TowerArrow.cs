using UnityEngine;

public class TowerArrow : MonoBehaviour
{
    private GameObject target;

    private int damage;

    private float travelTime;
    private float arcHeight;

    private float elapsedTime;

    private Vector3 startPosition;
    private Vector3 lastPosition;
    private Vector3 rotationOffset;

    public void Initialize(GameObject newTarget, int newDamage, float newTravelTime, float newArcHeight, Vector3 newRotationOffset)
    {
        target = newTarget;
        damage = newDamage;

        travelTime = Mathf.Max(0.01f, newTravelTime);

        arcHeight = newArcHeight;

        startPosition = transform.position;
        lastPosition = transform.position;

        rotationOffset = newRotationOffset;
    }

    private void Update()
    {
        // Target died before arrow arrived.
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / travelTime);

        Vector3 targetPosition = GetTargetPosition();

        // ==========================================
        // BASIC LINE
        // ==========================================

        Vector3 position = Vector3.Lerp( startPosition, targetPosition, t );

        // ==========================================
        // ARC
        //
        // sin(0)   = 0
        // sin(PI/2)= 1
        // sin(PI)  = 0
        //
        // So the arrow rises and then falls.
        // ==========================================

        float arc = Mathf.Sin(t * Mathf.PI) * arcHeight;

        position.y += arc;

        transform.position = position;

        // ==========================================
        // ROTATE ARROW ALONG TRAJECTORY
        // ==========================================

        Vector3 movement = transform.position - lastPosition;

        if (movement.sqrMagnitude > 0.0001f)
        {
            Quaternion trajectoryRotation = Quaternion.LookRotation(movement.normalized);

            Quaternion modelOffset = Quaternion.Euler(rotationOffset);

            transform.rotation = trajectoryRotation * modelOffset;
        }

        lastPosition = transform.position;

        // ==========================================
        // HIT
        // ==========================================

        if (t >= 1f)
        {
            HitTarget();
        }
    }

    private void HitTarget()
    {
        if (target != null)
        {
            IDamageable damageable = target.GetComponent<IDamageable>();

            if (damageable == null)
            {
                damageable = target.GetComponentInParent<IDamageable>();
            }

            if (damageable != null && !damageable.IsDead && damageable.Team == CombatTeam.Enemy)
            {
                damageable.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    private Vector3 GetTargetPosition()
    {
        Collider targetCollider = target.GetComponentInChildren<Collider>();

        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return target.transform.position;
    }
}