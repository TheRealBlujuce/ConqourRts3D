using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InstancedTreeManager : MonoBehaviour
{
    [Header("Instanced Tree Settings (edges)")]
    public Mesh treeMesh;
    public Material treeMaterial;
    public Vector3 treeScale = Vector3.one;
    public int treeCount = 1000;
    public float minSpacing = 2f;
	public Transform resourcesTranform;

    [Header("Edge Spawn Settings")]
    public GameObject terrainBorder;          
    public float edgeBandWidth = 20f;         

    [Header("Node Tree Settings (full map)")]
    public GameObject mapBounds;              // Assign full map bounds object
    public GameObject nodeTreePrefab;         // Prefab with ResourceNode component
    public int nodeTreeCount = 500;           // Number of node trees to scatter
    public float nodeTreeSpacing = 4f;        // Minimum distance between node trees

    private List<Vector3> treePositions = new List<Vector3>();
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
    private List<BoxCollider> colliders = new List<BoxCollider>();
	private List<Vector3> nodePositions = new List<Vector3>();

    private Bounds borderBounds;
    private Bounds mapAreaBounds;

    private void Start()
    {
        // Get terrainBorder bounds
        if (!terrainBorder)
        {
            Debug.LogError("Assign a GameObject to terrainBorder in InstancedTreeManager!");
            return;
        }
        borderBounds = GetObjectBounds(terrainBorder);

        // Get map bounds for node trees
        if (!mapBounds)
        {
            Debug.LogError("Assign a GameObject to mapBounds in InstancedTreeManager!");
            return;
        }
        mapAreaBounds = GetObjectBounds(mapBounds);

		GenerateTrees();
    }

    private Bounds GetObjectBounds(GameObject obj)
    {
        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null) return rend.bounds;

        Collider col = obj.GetComponent<Collider>();
        if (col != null) return col.bounds;

        Debug.LogError(obj.name + " must have a Renderer or Collider to get bounds!");
        return new Bounds(Vector3.zero, Vector3.zero);
    }

    private void Update()
    {
        for (int i = 0; i < matrices.Count; i += 1023)
        {
            int batchSize = Mathf.Min(1023, matrices.Count - i);
            Graphics.DrawMeshInstanced(treeMesh, 0, treeMaterial, matrices.GetRange(i, batchSize));
        }
    }

    public void GenerateEdgeTrees()
    {
        treePositions.Clear();
        matrices.Clear();
        colliders.Clear();

        Vector3 min = borderBounds.min;
        Vector3 max = borderBounds.max;

        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = GetRandomEdgePosition(min, max);

            bool tooClose = false;
            foreach (var existing in treePositions)
            {
                if (Vector3.SqrMagnitude(existing - pos) < minSpacing * minSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) { i--; continue; }

            treePositions.Add(pos);
            matrices.Add(Matrix4x4.TRS(pos, Quaternion.identity, treeScale));

            // Make a collider object for interaction
            GameObject colObj = new GameObject("TreeCollider_" + i);
            colObj.transform.SetParent(resourcesTranform);
            colObj.transform.position = pos;

            BoxCollider col = colObj.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(4f, 16f, 4f);
            col.isTrigger = false;

            colliders.Add(col);
        }
    }

    public void GenerateNodeTrees()
    {
        if (!nodeTreePrefab)
        {
            Debug.LogError("Node Tree Prefab is not assigned!");
            return;
        }

        Vector3 min = mapAreaBounds.min;
        Vector3 max = mapAreaBounds.max;

        for (int i = 0; i < nodeTreeCount; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(min.x, max.x),
                mapAreaBounds.min.y,
                Random.Range(min.z, max.z)
            );

            bool tooClose = false;
            foreach (var existing in nodePositions)
            {
                if (Vector3.SqrMagnitude(existing - pos) < nodeTreeSpacing * nodeTreeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) { i--; continue; }

            nodePositions.Add(pos);

            // Instantiate prefab at position
            GameObject node = Instantiate(nodeTreePrefab, pos, Quaternion.identity, resourcesTranform);
        }
    }

    private Vector3 GetRandomEdgePosition(Vector3 min, Vector3 max)
    {
        float x, z;

        if (Random.value < 0.5f)
        {
            // Along X edges
            if (Random.value < 0.5f)
                x = min.x + Random.Range(0f, edgeBandWidth);
            else
                x = max.x - Random.Range(0f, edgeBandWidth);

            z = Random.Range(min.z, max.z);
        }
        else
        {
            // Along Z edges
            if (Random.value < 0.5f)
                z = min.z + Random.Range(0f, edgeBandWidth);
            else
                z = max.z - Random.Range(0f, edgeBandWidth);

            x = Random.Range(min.x, max.x);
        }

        float y = borderBounds.min.y; 
        return new Vector3(x, y, z);
    }

	public void GenerateTrees()
	{
		nodePositions.Clear(); // clear positions for actual node trees

		// Spawn both sets
		GenerateEdgeTrees();
		GenerateNodeTrees();

        // If you want to update NavMesh after spawning trees:
        GameManager.Instance.GetNavUpdater().UpdateNavMeshInArea();
	}

	public void RegenerateTrees()
	{
		// Get terrainBorder bounds
        if (!terrainBorder)
        {
            //Debug.LogError("Assign a GameObject to terrainBorder in InstancedTreeManager!");
            return;
        }
        borderBounds = GetObjectBounds(terrainBorder);

        // Get map bounds for node trees
        if (!mapBounds)
        {
            //Debug.LogError("Assign a GameObject to mapBounds in InstancedTreeManager!");
            return;
        }
        mapAreaBounds = GetObjectBounds(mapBounds);

		GenerateTrees();
		Invoke("GenerateEdgeTrees", 0.1f);
	}

}
