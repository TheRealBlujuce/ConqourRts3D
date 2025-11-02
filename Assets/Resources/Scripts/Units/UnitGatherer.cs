using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(UnitController))]
public class UnitGatherer : MonoBehaviour
{
	[Header("Harvest Settings")]
    public int maxCarryAmount = 10;
    public float harvestInterval = 2f;
	[SerializeField] private int carriedAmount = 0;
    [SerializeField] private ResourceNode currentNode;
    [SerializeField] private DropOffPoint dropOffPoint;
    private ResourceNode.ResourceType currentResourceType;
	[SerializeField] private float nodeFindDistance;

	[Header("Resource Visual Prefabs")]
	public GameObject goldVisual;
	public GameObject lumberVisual;
	public GameObject foodVisual;
	public GameObject harvestTool;
	private GameObject activeResourceVisual;

    private UnitController controller;
    private NavMeshAgent agent;
    
    private float gatherTimer = 0f;
    private bool isGatheringEnabled = false;

    private bool returningToDropOff = false;
    private Vector3 originalNodePosition;

    private void Awake()
    {
        controller = GetComponent<UnitController>();
    }

	private void Start()
	{
		agent = controller.GetUnitAgent();
        // Set default avoidance at the start of the game
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	private void Update()
	{
		if (isGatheringEnabled && currentNode == null && !returningToDropOff)
		{
			FindNewNode();
			return;
		}

		if (returningToDropOff)
		{
			if (dropOffPoint && IsInRange(dropOffPoint.transform.position))
			{
				agent.isStopped = true;
				DepositResources();
			}
		}
		else if (currentNode != null)
		{
			if (IsInRange(currentNode.transform.position))
			{
				agent.isStopped = true;
				controller.SetState(UnitState.Gathering);

				if (!controller.GetUnitAnimator().GetBool("isHarvesting"))
				{
					harvestTool.SetActive(true);
					controller.GetUnitAnimator().SetBool("isHarvesting", true);
				}

				gatherTimer += Time.deltaTime;
				if (gatherTimer >= harvestInterval)
				{
					HarvestResource();
					gatherTimer = 0f;

					if (carriedAmount >= maxCarryAmount || currentNode == null)
					{
						MoveToDropOff();
						controller.GetUnitAnimator().SetBool("isHarvesting", false);
					}
				}
			}
			else
			{
				agent.isStopped = false;
				controller.SetState(UnitState.Moving);
                // Ensure correct avoidance when moving to a resource node
                if(carriedAmount == 0)
                {
                    agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                }
			}
		}
	}

    public void StartGathering(ResourceNode node, DropOffPoint dropOff)
    {
		ResetResourceVisuals();

        currentNode = node;
        dropOffPoint = dropOff;
        returningToDropOff = false;
        isGatheringEnabled = true;
        currentResourceType = node.resourceType;
        originalNodePosition = node.transform.position;

        agent.isStopped = false;
        controller.GetUnitAgent().SetDestination(originalNodePosition);
        controller.SetState(UnitState.Moving);
        // Unit is not carrying resources, so use high-quality avoidance
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
    }

    private void MoveToDropOff()
    {
        if (dropOffPoint == null) return;

        returningToDropOff = true;
        agent.isStopped = false;
        // Unit is carrying resources, so use low-quality avoidance for faster pathing
		agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        controller.GetUnitAgent().SetDestination(dropOffPoint.transform.position);
        controller.SetState(UnitState.Moving);
		harvestTool.SetActive(false);
    }

	public void DepositResources()
	{
		if (dropOffPoint != null)
		{
			ResourceManager.Instance.AddResource(currentResourceType, carriedAmount);
		}
    
		carriedAmount = 0;
		ResetResourceVisuals();
		returningToDropOff = false;

		if (currentNode != null)
		{
			agent.isStopped = false;
			controller.GetUnitAgent().SetDestination(originalNodePosition);
			controller.SetState(UnitState.Moving);
            // Unit is now empty, so switch back to high-quality avoidance
			agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
		}
		else
		{
			FindNewNode();
		}
	}

	private void HarvestResource()
	{
		if (currentNode == null) return;
    
		int harvested = currentNode.Harvest();
		if (harvested > 0)
		{
			carriedAmount += harvested;
			SetCurrentResourceVisual();
		}
	}

	private void SetCurrentResourceVisual()
	{
		ResetResourceVisuals();

		if (currentNode == null) return;
    
		switch (currentNode.resourceType)
		{
			case ResourceNode.ResourceType.Gold:
				goldVisual.SetActive(true);
				activeResourceVisual = goldVisual;
				break;
			case ResourceNode.ResourceType.Lumber:
				lumberVisual.SetActive(true);
				activeResourceVisual = lumberVisual;
				break;
			case ResourceNode.ResourceType.Food:
				foodVisual.SetActive(true);
				activeResourceVisual = foodVisual;
				break;
		}
	}

	private void ResetResourceVisuals()
	{
		if (activeResourceVisual != null)
		{
			activeResourceVisual.SetActive(false);
			activeResourceVisual = null;
		}
		goldVisual.SetActive(false);
		lumberVisual.SetActive(false);
		foodVisual.SetActive(false);
	}

	public void StopGathering()
	{
        isGatheringEnabled = false;
		if (carriedAmount <= 0) { currentNode = null; }
		dropOffPoint = null;
		returningToDropOff = false;
		gatherTimer = 0f;
		agent.isStopped = false;
		controller.SetState(UnitState.Idle);
		controller.GetUnitAnimator().SetBool("isHarvesting", false);
        // Unit is no longer gathering, so use high-quality avoidance
		agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	public void MoveToDropOff(DropOffPoint dropOff)
	{
        isGatheringEnabled = true;
		if (dropOff == null) return;

		returningToDropOff = true;
		dropOffPoint = dropOff;
		agent.isStopped = false;
		controller.GetUnitAgent().SetDestination(dropOff.transform.position);
		controller.SetState(UnitState.Moving);
        // Unit is moving to drop-off while carrying resources, so use low-quality avoidance
		agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
	}

	private bool IsInRange(Vector3 targetPos)
	{
		float minDist = returningToDropOff ? controller.GetMinDistanceToDropOff() : controller.GetMinDistanceToResource();
		return Vector3.Distance(transform.position, targetPos) <= minDist;
	}

	public ResourceNode GetCurrentNode() => currentNode;
	public int GetCurrentCarriedResources() => carriedAmount;

	public void SetReturningToDropOff(bool value)
	{
		returningToDropOff = value;
	}

	private void FindNewNode()
	{
		if (!isGatheringEnabled)
		{
			agent.isStopped = true;
			controller.SetState(UnitState.Idle);
			controller.GetUnitAnimator().SetBool("isHarvesting", false);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
			return;
		}

		ResourceNode newTargetNode = FindNearestResourceNode(currentResourceType);
		
		if (newTargetNode != null)
		{
			StartGathering(newTargetNode, dropOffPoint);
			//Debug.Log($"Unit {gameObject.name} found a new resource node ({newTargetNode.resourceType}) and is heading there.");
		}
		else
		{
			agent.isStopped = true;
			controller.SetState(UnitState.Idle);
			controller.GetUnitAnimator().SetBool("isHarvesting", false);
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
			//Debug.Log($"Unit {gameObject.name} could not find a new resource node of type {currentResourceType} and is now idle.");
		}
	}

	private ResourceNode FindNearestResourceNode(ResourceNode.ResourceType? resourceType)
	{
		ResourceNode nearestNode = null;
		float minDistance = nodeFindDistance;
		ResourceNode[] allNodes = FindObjectsByType<ResourceNode>(FindObjectsSortMode.None);

		foreach (ResourceNode node in allNodes)
		{
			if (node != null)
			{
				if (resourceType.HasValue && node.resourceType != resourceType.Value)
				{
					continue;
				}

				float distance = Vector3.Distance(transform.position, node.transform.position);
				if (distance < minDistance)
				{
					minDistance = distance;
					nearestNode = node;
				}
			}
		}
		return nearestNode;
	}
}