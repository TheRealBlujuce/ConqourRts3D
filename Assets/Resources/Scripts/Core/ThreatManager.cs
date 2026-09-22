using UnityEngine;
using UnityEngine.UI;

public class ThreatManager : MonoBehaviour
{
 
    [Header("Threat Settings")]
    public int threat = 0;
    private int maxThreat = 100;

    [Header("Threat UI Settings")]
    [SerializeField] private Image threatBar;

    public static ThreatManager Instance { get; private set; }

    private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
	}


    private void Start()
    {
        threat = 0;
    }

    public void AddThreat(int amount)
    {
        threat = Mathf.Clamp(threat + amount, 0, maxThreat);
    }

    public void RemoveThreat(int amount)
    {
        threat = Mathf.Clamp(threat - amount, 0, maxThreat);
    }

    private void Update()
    {
        threatBar.fillAmount = Mathf.Lerp(threatBar.fillAmount, (float)threat / maxThreat, 2f * Time.deltaTime);
    }

}
