using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI fpsText;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;

    private float timer;
    private int frameCount;

    private void Awake()
    {
        // Allows you to put this script directly
        // on the TextMeshProUGUI object.
        if (fpsText == null)
        {
            fpsText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= updateInterval)
        {
            float fps =
                frameCount / timer;

            if (fpsText != null)
            {
                fpsText.text =
                    $"FPS: {Mathf.RoundToInt(fps)}";
            }

            frameCount = 0;
            timer = 0f;
        }
    }
}