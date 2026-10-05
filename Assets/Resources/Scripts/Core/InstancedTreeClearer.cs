using UnityEngine;

public class InstancedTreeClearer : MonoBehaviour
{
    [Header("Tree Clearing")]
    [SerializeField] private float clearRadius = 6f;

    public void ClearTrees()
    {
        InstancedTreeManager treeManager =
            FindFirstObjectByType<InstancedTreeManager>();

        if (treeManager == null)
        {
            Debug.LogWarning(
                "No InstancedTreeManager found."
            );

            return;
        }

        treeManager.RemoveTreesInRadius(
            transform.position,
            clearRadius
        );
    }
}