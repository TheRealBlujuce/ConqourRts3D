using System.Collections.Generic;
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

	private ResourceNodePoint currentResourcePoint;
	private Transform currentResourceSlot;
	private Transform currentDropOffSlot;

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

	private void Awake()
	{
		controller = GetComponent<UnitController>();
	}

	private void Start()
	{
		agent = controller.GetUnitAgent();

		agent.obstacleAvoidanceType =
			ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	private void Update()
	{
		// Need to find another resource.
		if (isGatheringEnabled &&
			currentNode == null &&
			!returningToDropOff)
		{
			FindNewNode();
			return;
		}

		// Returning resources to drop-off.
		if (returningToDropOff)
		{
			if (currentDropOffSlot != null &&
				IsInRange(currentDropOffSlot.position))
			{
				agent.isStopped = true;
				DepositResources();
			}

			return;
		}

		// Gathering from resource.
		if (currentNode != null)
		{
			if (currentResourceSlot != null &&
				IsInRange(currentResourceSlot.position))
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

					if (carriedAmount >= maxCarryAmount ||
						currentNode == null)
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

				if (carriedAmount == 0)
				{
					agent.obstacleAvoidanceType =
						ObstacleAvoidanceType.HighQualityObstacleAvoidance;
				}
			}
		}
	}

	public void StartGathering(ResourceNode node, DropOffPoint dropOff)
	{
		if (node == null)
			return;

		ResetResourceVisuals();

		currentNode = node;
		dropOffPoint = dropOff;

		returningToDropOff = false;
		isGatheringEnabled = true;

		currentResourceType = node.resourceType;

		// Get the interaction point component from the resource.
		currentResourcePoint =
			node.GetComponent<ResourceNodePoint>();

		if (currentResourcePoint == null)
		{
			Debug.LogWarning(
				$"{node.name} does not have a ResourceNodePoint component."
			);

			return;
		}

		// Find the closest interaction slot to this gatherer.
		currentResourceSlot =
			currentResourcePoint.GetClosestSlot(transform.position);

		if (currentResourceSlot == null)
		{
			Debug.LogWarning(
				$"{node.name} has no available ResourceNode interaction slot."
			);

			return;
		}

		agent.isStopped = false;

		agent.SetDestination(currentResourceSlot.position);

		controller.SetState(UnitState.Moving);

		agent.obstacleAvoidanceType =
			ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	private void MoveToDropOff()
	{
		DropOffPoint nearestDropOff = FindNearestDropOff();

		if (nearestDropOff == null)
		{
			Debug.LogWarning(
				$"{gameObject.name} could not find a valid DropOffPoint."
			);

			return;
		}

		dropOffPoint = nearestDropOff;

		currentDropOffSlot =
			dropOffPoint.GetClosestSlot(transform.position);

		if (currentDropOffSlot == null)
		{
			Debug.LogWarning(
				$"{dropOffPoint.name} has no available DropOff interaction slot."
			);

			return;
		}

		returningToDropOff = true;

		agent.isStopped = false;

		// Carrying resources, so use low-quality avoidance
		// for faster movement through other units.
		agent.obstacleAvoidanceType =
			ObstacleAvoidanceType.NoObstacleAvoidance;

		agent.SetDestination(currentDropOffSlot.position);

		controller.SetState(UnitState.Moving);

		harvestTool.SetActive(false);
	}

	private DropOffPoint FindNearestDropOff()
	{
		DropOffPoint nearestDropOff = null;

		float shortestDistance = Mathf.Infinity;

		List<DropOffPoint> allDropOffs =
			NodeRegistry.Instance.dropOffPoints;

		foreach (DropOffPoint dropOff in allDropOffs)
		{
			if (dropOff == null)
				continue;

			Transform closestSlot =
				dropOff.GetClosestSlot(transform.position);

			if (closestSlot == null)
				continue;

			float distance =
				Vector3.Distance(
					transform.position,
					closestSlot.position
				);

			if (distance < shortestDistance)
			{
				shortestDistance = distance;
				nearestDropOff = dropOff;
			}
		}

		return nearestDropOff;
	}

	public void DepositResources()
	{
		if (dropOffPoint != null)
		{
			ResourceManager.Instance.AddResource(
				currentResourceType,
				carriedAmount
			);
		}

		carriedAmount = 0;

		ResetResourceVisuals();

		returningToDropOff = false;
		currentDropOffSlot = null;

		if (currentNode != null)
		{
			// Get a new interaction slot because another
			// gatherer may have occupied the previous one.
			currentResourceSlot =
				currentResourcePoint != null
					? currentResourcePoint.GetClosestSlot(transform.position)
					: null;

			if (currentResourceSlot != null)
			{
				agent.isStopped = false;

				agent.SetDestination(
					currentResourceSlot.position
				);

				controller.SetState(UnitState.Moving);

				agent.obstacleAvoidanceType =
					ObstacleAvoidanceType.HighQualityObstacleAvoidance;
			}
			else
			{
				FindNewNode();
			}
		}
		else
		{
			FindNewNode();
		}
	}

	private void HarvestResource()
	{
		if (currentNode == null)
			return;

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

		if (currentNode == null)
			return;

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

		if (carriedAmount <= 0)
		{
			currentNode = null;
			currentResourcePoint = null;
			currentResourceSlot = null;
		}

		dropOffPoint = null;
		currentDropOffSlot = null;

		returningToDropOff = false;
		gatherTimer = 0f;

		agent.isStopped = false;

		controller.SetState(UnitState.Idle);

		controller.GetUnitAnimator().SetBool(
			"isHarvesting",
			false
		);

		agent.obstacleAvoidanceType =
			ObstacleAvoidanceType.HighQualityObstacleAvoidance;
	}

	public void MoveToDropOff(DropOffPoint dropOff)
	{
		isGatheringEnabled = true;

		if (dropOff == null)
			return;

		dropOffPoint = dropOff;

		currentDropOffSlot = dropOffPoint.GetClosestSlot(transform.position);

		if (currentDropOffSlot == null)
		{
			Debug.LogWarning(
				$"{dropOffPoint.name} has no available interaction slot."
			);

			return;
		}

		returningToDropOff = true;

		agent.isStopped = false;

		agent.SetDestination(
			currentDropOffSlot.position
		);

		controller.SetState(UnitState.Moving);

		agent.obstacleAvoidanceType =
			ObstacleAvoidanceType.NoObstacleAvoidance;

		harvestTool.SetActive(false);
	}

	private bool IsInRange(Vector3 targetPos)
	{
		float minDist = returningToDropOff
			? controller.GetMinDistanceToDropOff()
			: controller.GetMinDistanceToResource();

		return Vector3.Distance(
			transform.position,
			targetPos
		) <= minDist;
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

			controller.GetUnitAnimator().SetBool(
				"isHarvesting",
				false
			);

			agent.obstacleAvoidanceType =
				ObstacleAvoidanceType.HighQualityObstacleAvoidance;

			return;
		}

		ResourceNode newTargetNode =
			FindNearestResourceNode(currentResourceType);

		if (newTargetNode != null)
		{
			StartGathering(
				newTargetNode,
				dropOffPoint
			);
		}
		else
		{
			agent.isStopped = true;

			controller.SetState(UnitState.Idle);

			controller.GetUnitAnimator().SetBool(
				"isHarvesting",
				false
			);

			agent.obstacleAvoidanceType =
				ObstacleAvoidanceType.HighQualityObstacleAvoidance;
		}
	}

	private ResourceNode FindNearestResourceNode(ResourceNode.ResourceType? resourceType)
	{
		ResourceNode nearestNode = null;

		float minDistance = nodeFindDistance;

		List<ResourceNode> allNodes =
			NodeRegistry.Instance.resourceNodes;

		foreach (ResourceNode node in allNodes)
		{
			if (node == null)
				continue;

			if (resourceType.HasValue &&
				node.resourceType != resourceType.Value)
			{
				continue;
			}

			// Ignore resources that don't have an interaction point.
			ResourceNodePoint resourcePoint =
				node.GetComponent<ResourceNodePoint>();

			if (resourcePoint == null)
				continue;

			float distance =
				Vector3.Distance(
					transform.position,
					node.transform.position
				);

			if (distance < minDistance)
			{
				minDistance = distance;
				nearestNode = node;
			}
		}

		return nearestNode;
	}
}