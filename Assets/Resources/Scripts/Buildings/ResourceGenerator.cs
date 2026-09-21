using System.Collections;
using UnityEngine;

public class ResourceGenerator : MonoBehaviour
{

    public ResourceNode.ResourceType resourceToGenerate;
    public int resourceAmountToGenerate;
    public float resourceGenerationTime = 6f;
    private float resourceGenerationTimer = 0f;

     private void Update()
    {
        resourceGenerationTimer += Time.deltaTime;

        if (resourceGenerationTimer >= resourceGenerationTime)
        {
            GenerateResource();

            resourceGenerationTimer = 0f;
        }
    }

    private void GenerateResource()
    {
        ResourceManager.Instance.AddResource(
            resourceToGenerate,
            resourceAmountToGenerate
        );
    }

}
