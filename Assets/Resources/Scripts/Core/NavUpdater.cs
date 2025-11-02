using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavUpdater : MonoBehaviour
{

	public NavMeshSurface navSurface;
	private NavMeshData navMeshData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        navSurface = FindFirstObjectByType<NavMeshSurface>();
		navMeshData = navSurface.navMeshData;
    }

	public void UpdateNavMesh()
	{
		navSurface.BuildNavMesh();
	}

	public void UpdateNavMeshInArea()
	{
		navSurface.UpdateNavMesh(navSurface.navMeshData);
	}

}
