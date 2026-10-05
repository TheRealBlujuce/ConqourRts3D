using UnityEngine;
using UnityEngine.UI;

public class ThreatManager : MonoBehaviour
{
    [Header("Threat Settings")]
    [SerializeField] private float threat = 0f;
    [SerializeField] private float maxThreat = 100f;
    [SerializeField] private float threatIncreasePerSecond = 1f;
    [SerializeField] private float threatDecreasePerSecond = 10f;

    [Header("References")]
    [SerializeField] private WaveSpawner waveSpawner;

    [Header("Threat UI Settings")]
    [SerializeField] private Image threatBar;

    public static ThreatManager Instance { get; private set; }

    private bool drainingThreat = false;

    public float Threat => threat;
    public float MaxThreat => maxThreat;

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
        threat = 0f;

        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }

        UpdateThreatUI(true);
    }

    private void Update()
    {
        HandleThreat();
        UpdateThreatUI(false);
    }

    private void HandleThreat()
    {
        if (waveSpawner == null)
            return;

        // ==========================================
        // DRAIN THREAT AFTER A WAVE IS TRIGGERED
        // ==========================================

        if (drainingThreat)
        {
            threat = Mathf.MoveTowards(threat, 0f, threatDecreasePerSecond * Time.deltaTime);

            if (threat <= 0f)
            {
                threat = 0f;
                drainingThreat = false;
            }

            return;
        }

        // Do not begin filling again while the previous wave is still active.
        if (waveSpawner.IsWaveActive() || waveSpawner.AreEnemiesRemaining())
            return;

        // ==========================================
        // PASSIVE THREAT GENERATION
        // ==========================================

        float passiveThreat = threatIncreasePerSecond + (waveSpawner.GetCurrentWave() * 0.5f);
        threat += passiveThreat * Time.deltaTime;
        threat = Mathf.Clamp(threat, 0f, maxThreat);

        // ==========================================
        // START WAVE
        // ==========================================

        if (threat >= maxThreat)
        {
            if (waveSpawner.TryStartNextWave())
            {
                threat = maxThreat;
                drainingThreat = true;
            }
        }
    }

    public void AddThreat(int amount)
    {
        // Don't add more threat while the current meter is draining.
        if (drainingThreat)
            return;

        threat = Mathf.Clamp(threat + amount, 0f, maxThreat);
    }

    public void RemoveThreat(int amount)
    {
        threat = Mathf.Clamp(threat - amount, 0f, maxThreat);
    }

    private void UpdateThreatUI(bool immediate)
    {
        if (threatBar == null)
            return;

        float targetFill = threat / maxThreat;

        if (immediate)
        {
            threatBar.fillAmount = targetFill;
            return;
        }

        threatBar.fillAmount = Mathf.Lerp(threatBar.fillAmount, targetFill, 2f * Time.deltaTime);
    }
}