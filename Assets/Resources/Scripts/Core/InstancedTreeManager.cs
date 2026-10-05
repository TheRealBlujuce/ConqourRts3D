using System.Collections.Generic;
using UnityEngine;

public class InstancedTreeManager : MonoBehaviour
{
    [Header("Instanced Tree Settings (edges)")]
    public Mesh[] treeMeshs;
    public Material treeMaterial;
    public Vector3 treeScale = Vector3.one;
    public int treeCount = 1000;
    public float decorativeNodeSpacing = 2f;
	public Transform resourcesTranform;        

    [Header("Node Tree Settings (full map)")]
    public GameObject mapBounds;              // Assign full map bounds object
    public GameObject nodeTreePrefab;         // Prefab with ResourceNode component
    public int nodeTreeCount = 500;           // Number of node trees to scatter
    public float nodeTreeSpacing = 4f;        // Minimum distance between node trees
    
    private List<Vector3> treePositions = new List<Vector3>();
    private List<Matrix4x4> matrices = new List<Matrix4x4>();
	private List<Vector3> nodePositions = new List<Vector3>();
    private List<int> treeMeshIndices = new List<int>();

    private Bounds mapAreaBounds;

    [Header("Tree Culling")]
    [SerializeField] private Camera targetCamera;

    [SerializeField] private float chunkSize = 30f;

    [SerializeField] private float cullingPadding = 10f;

    private readonly Dictionary<Vector2Int, TreeChunk> treeChunks = new Dictionary<Vector2Int, TreeChunk>();

    private Plane[] cameraPlanes;

    private void Start()
    {
        // Get map bounds for node trees
        if (!mapBounds)
        {
            Debug.LogError("Assign a GameObject to mapBounds in InstancedTreeManager!");
            return;
        }
        mapAreaBounds = GetObjectBounds(mapBounds);
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
        if (targetCamera == null)
        {
            targetCamera = Camera.main;

            if (targetCamera == null)
                return;
        }


        cameraPlanes =
            GeometryUtility.CalculateFrustumPlanes(
                targetCamera
            );


        foreach (TreeChunk chunk in treeChunks.Values)
        {
            Bounds expandedBounds =
                chunk.bounds;

            expandedBounds.Expand(
                cullingPadding
            );


            bool visible =
                GeometryUtility.TestPlanesAABB(
                    cameraPlanes,
                    expandedBounds
                );


            if (!visible)
                continue;


            DrawTreeChunk(chunk);
        }
    }

    public void GenerateDecorativeTrees()
    {
        treePositions.Clear();
        matrices.Clear();
        treeMeshIndices.Clear();

        Vector3 min = mapAreaBounds.min;
        Vector3 max = mapAreaBounds.max;

        for (int i = 0; i < treeCount; i++)
        {
            Vector3 pos = new Vector3(Random.Range(min.x, max.x),mapAreaBounds.min.y,Random.Range(min.z, max.z));

            bool tooClose = false;

            foreach (Vector3 existing in treePositions)
            {
                Vector3 offset = existing - pos;
                offset.y = 0f;

                if (offset.sqrMagnitude < decorativeNodeSpacing * decorativeNodeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                i--;
                continue;
            }

            treePositions.Add(pos);

            // Random visual variation.
            float randomRotation = Random.Range(0f, 360f);

            float randomScale = Random.Range(0.85f, 1.15f);

            Quaternion rotation = Quaternion.Euler(0f, randomRotation, 0f);

            Vector3 scale = treeScale * randomScale;

            matrices.Add(Matrix4x4.TRS(pos, rotation, scale));

            int randomMeshIndex = Random.Range(0, treeMeshs.Length);

            treeMeshIndices.Add(randomMeshIndex);
        }

        BuildTreeChunks();
    }

    private void BuildTreeChunks()
    {
        treeChunks.Clear();

        for (int i = 0; i < treePositions.Count; i++)
        {
            Vector3 position = treePositions[i];

            Vector2Int chunkCoordinate = new Vector2Int(Mathf.FloorToInt(position.x / chunkSize),Mathf.FloorToInt(position.z / chunkSize));

            if (!treeChunks.TryGetValue(chunkCoordinate, out TreeChunk chunk))
            {
                chunk = new TreeChunk();

                Vector3 center =
                    new Vector3(
                        (chunkCoordinate.x + 0.5f) * chunkSize,
                        position.y,
                        (chunkCoordinate.y + 0.5f) * chunkSize
                    );

                chunk.bounds =
                    new Bounds(
                        center,
                        new Vector3(
                            chunkSize,
                            20f,
                            chunkSize
                        )
                    );

                treeChunks.Add(
                    chunkCoordinate,
                    chunk
                );
            }

            int meshIndex = treeMeshIndices[i];

            if (!chunk.matricesByMesh.TryGetValue(meshIndex, out List<Matrix4x4> meshMatrices))
            {
                meshMatrices =
                    new List<Matrix4x4>();

                chunk.matricesByMesh.Add(
                    meshIndex,
                    meshMatrices
                );
            }

            meshMatrices.Add(matrices[i]);
        }


        // Build batches for each mesh in each chunk.
        foreach (TreeChunk chunk in treeChunks.Values)
        {
            foreach (var pair in chunk.matricesByMesh)
            {
                int meshIndex = pair.Key;
                List<Matrix4x4> meshMatrices = pair.Value;

                int batchCount = Mathf.CeilToInt(meshMatrices.Count / 1023f);

                Matrix4x4[][] batches = new Matrix4x4[batchCount][];

                for (int batch = 0;batch < batchCount;batch++)
                {
                    int startIndex =
                        batch * 1023;

                    int count =
                        Mathf.Min(
                            1023,
                            meshMatrices.Count - startIndex
                        );

                    Matrix4x4[] batchMatrices =
                        new Matrix4x4[count];

                    meshMatrices.CopyTo(
                        startIndex,
                        batchMatrices,
                        0,
                        count
                    );


                    batches[batch] = batchMatrices;
                }

                chunk.batchesByMesh.Add(
                    meshIndex,
                    batches
                );
            }
        }

        // Debug.Log(
        //     $"Built {treeChunks.Count} tree chunks."
        // );
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
            Vector3 pos = new Vector3(Random.Range(min.x, max.x), mapAreaBounds.min.y, Random.Range(min.z, max.z));

            bool tooClose = false;
            foreach (var existing in nodePositions)
            {
                if (Vector3.SqrMagnitude(existing - pos) < nodeTreeSpacing * nodeTreeSpacing)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                foreach (Vector3 decorativeTree in treePositions)
                {
                    Vector3 offset =
                        decorativeTree - pos;

                    offset.y = 0f;

                    if (offset.sqrMagnitude < decorativeNodeSpacing * decorativeNodeSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }
            
            if (tooClose) { i--; continue; }

            nodePositions.Add(pos);

            // Instantiate prefab at position
            GameObject node = Instantiate(nodeTreePrefab, pos, Quaternion.identity, resourcesTranform);
        }
    }

	public void GenerateTrees()
	{
		nodePositions.Clear(); // clear positions for actual node trees

		// Spawn both sets
		GenerateDecorativeTrees();
		GenerateNodeTrees();

        // If you want to update NavMesh after spawning trees:
        GameManager.Instance.GetNavUpdater().UpdateNavMeshInArea();
	}

	public void RegenerateTrees()
	{
        // Get map bounds for node trees
        if (!mapBounds)
        {
            //Debug.LogError("Assign a GameObject to mapBounds in InstancedTreeManager!");
            return;
        }
        mapAreaBounds = GetObjectBounds(mapBounds);

		GenerateTrees();
		// Invoke("GenerateEdgeTrees", 0.1f);
	}

    private void DrawTreeChunk(TreeChunk chunk)
    {
        foreach (var pair in chunk.batchesByMesh)
        {
            int meshIndex = pair.Key;
            Matrix4x4[][] batches = pair.Value;


            if (meshIndex < 0 ||
                meshIndex >= treeMeshs.Length)
            {
                continue;
            }


            Mesh mesh = treeMeshs[meshIndex];


            if (mesh == null)
                continue;


            foreach (Matrix4x4[] batch in batches)
            {
                if (batch == null ||
                    batch.Length == 0)
                {
                    continue;
                }


                Graphics.DrawMeshInstanced(
                    mesh,
                    0,
                    treeMaterial,
                    batch
                );
            }
        }
    }

    public void RemoveTreesInRadius(Vector3 center, float radius)
    {
        float radiusSqr = radius * radius;
        bool removedAny = false;

        for (int i = treePositions.Count - 1; i >= 0; i--)
        {
            Vector3 offset = treePositions[i] - center;

            // Only calculate horizontal distance.
            offset.y = 0f;

            if (offset.sqrMagnitude > radiusSqr)
                continue;

            // Remove the visual tree.
            treePositions.RemoveAt(i);
            matrices.RemoveAt(i);
            treeMeshIndices.RemoveAt(i);

            removedAny = true;
        }


        // Rebuild our rendering chunks after changing
        // the collection of instanced trees.
        if (removedAny)
        {
            BuildTreeChunks();
        }
    }

    

}

public class TreeChunk
{
    public Bounds bounds;

    public Dictionary<int, List<Matrix4x4>> matricesByMesh = new Dictionary<int, List<Matrix4x4>>();

    public Dictionary<int, Matrix4x4[][]> batchesByMesh = new Dictionary<int, Matrix4x4[][]>();
}