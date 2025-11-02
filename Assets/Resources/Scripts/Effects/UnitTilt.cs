using UnityEngine;

public class UnitTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    [Tooltip("How much the unit tilts when rotating.")]
    public float tiltAmount = 15f;
    
    [Tooltip("How quickly the tilt updates.")]
    public float tiltSpeed = 5f;

    [Tooltip("Axis to tilt on (usually Z for aircraft-style banking, X for ground tilting).")]
    public Vector3 tiltAxis = new Vector3(1, 0, 0);

    private float currentTilt = 0f;
    private float lastYRotation;

    void Start()
    {
        lastYRotation = transform.eulerAngles.y;
    }

    void Update()
    {
        // Get change in Y rotation
        float deltaY = Mathf.DeltaAngle(lastYRotation, transform.eulerAngles.y);

        // Target tilt is based on how much the unit is turning
        float targetTilt = -deltaY * tiltAmount;

        // Smoothly interpolate tilt
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);

        // Apply tilt without affecting original rotation
        Quaternion baseRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        Quaternion tiltRotation = Quaternion.AngleAxis(currentTilt, tiltAxis);

        transform.rotation = baseRotation * tiltRotation;

        // Update last rotation for next frame
        lastYRotation = transform.eulerAngles.y;
    }
}

