using System.Collections.Generic;
using UnityEngine;

public class NodeRegistry : MonoBehaviour
{
    public static NodeRegistry Instance { get; private set; }

	public readonly List<ResourceNode> resourceNodes = new();
	public readonly List<DropOffPoint> dropOffPoints = new();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}

	public void RegisterResourceNode(ResourceNode node)
	{
		if (node == null || resourceNodes.Contains(node))
			return;

		resourceNodes.Add(node);
	}

	public void UnregisterResourceNode(ResourceNode node)
	{
		if (node == null)
			return;

		resourceNodes.Remove(node);
	}

	public void RegisterDropOffPoint(DropOffPoint dropOff)
	{
		if (dropOff == null || dropOffPoints.Contains(dropOff))
			return;

		dropOffPoints.Add(dropOff);
	}

	public void UnregisterDropOffPoint(DropOffPoint dropOff)
	{
		if (dropOff == null)
			return;

		dropOffPoints.Remove(dropOff);
	}
}
