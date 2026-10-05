using UnityEngine;

public class TowerManager : MonoBehaviour
{
    public static TowerManager Instance { get; private set; }

    [Header("Tower Limit")]
    [SerializeField] private int maxTowers = 10;

    private int currentTowers = 0;

    public int CurrentTowers => currentTowers;
    public int MaxTowers => maxTowers;

    public bool CanBuildTower =>
        currentTowers < maxTowers;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterTower()
    {
        currentTowers++;
    }

    public void UnregisterTower()
    {
        currentTowers =
            Mathf.Max(0, currentTowers - 1);
    }
}