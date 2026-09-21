using UnityEngine;

public class ResourceNodePoint : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 2f;

    [Header("Slots")]
    [SerializeField] private int interactionSlots = 8;

    private Transform[] slots;

    private void Awake()
    {
        GenerateSlots();
    }

    private void GenerateSlots()
    {
        slots = new Transform[interactionSlots];

        for (int i = 0; i < interactionSlots; i++)
        {
            float angle = (360f / interactionSlots) * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 localPosition = new Vector3(
                Mathf.Cos(radians),
                0f,
                Mathf.Sin(radians)
            ) * interactionRadius;

            GameObject slot = new GameObject($"ResourceSlot_{i}");
            slot.transform.SetParent(transform);
            slot.transform.localPosition = localPosition;

            slots[i] = slot.transform;
        }
    }

    public Transform GetClosestSlot(Vector3 position)
    {
        Transform closestSlot = null;
        float closestDistance = Mathf.Infinity;

        foreach (Transform slot in slots)
        {
            float distance = Vector3.Distance(position, slot.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlot = slot;
            }
        }

        return closestSlot;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}