using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI Toolkit")]
    [SerializeField] private UIDocument hudDocument;

    [Header("Label Names (UXML)")]
    [SerializeField] private string timeLabelName = "timeTxt";
    [SerializeField] private string scoreLabelName = "scoreTxt";
    [SerializeField] private string speedLabelName = "speedTxt";

    [Header("References")]
    [SerializeField] private PlaneController plane;

    [Header("Formatting")]
    [SerializeField] private bool showMinutesSeconds = true;
    [SerializeField] private string speedSuffix = " m/s";
    [SerializeField] private float speedMultiplier = 1f;

    private Label timeLabel;
    private Label scoreLabel;
    private Label speedLabel;

    private float startTime;
    private float frozenElapsed;   // <- stores final time when stopped
    private bool timerRunning = true;

    private int score;

    void Awake()
    {
        if (hudDocument == null) hudDocument = GetComponent<UIDocument>();
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();

        var root = hudDocument.rootVisualElement;

        timeLabel = root.Q<Label>(timeLabelName);
        scoreLabel = root.Q<Label>(scoreLabelName);
        speedLabel = root.Q<Label>(speedLabelName);

        if (timeLabel == null) Debug.LogError($"HUDController: Missing Label '{timeLabelName}' in HUD UXML.");
        if (scoreLabel == null) Debug.LogError($"HUDController: Missing Label '{scoreLabelName}' in HUD UXML.");
        if (speedLabel == null) Debug.LogError($"HUDController: Missing Label '{speedLabelName}' in HUD UXML.");

        startTime = Time.time;

        SetScore(0);
        UpdateTime(0f);
        UpdateSpeed(0f);
    }

    void Update()
    {
        // TIME
        float elapsed = timerRunning ? (Time.time - startTime) : frozenElapsed;
        UpdateTime(elapsed);

        // SPEED
        if (plane != null)
        {
            float spd = plane.GetCurrentSpeed() * speedMultiplier;
            UpdateSpeed(spd);
        }
    }

    // ---------- Public API ----------
    public void SetScore(int newScore)
    {
        score = newScore;
        if (scoreLabel != null)
            scoreLabel.text = $"{score}";
    }

    public void AddScore(int delta)
    {
        SetScore(score + delta);
    }

    public void ResetTimer()
    {
        timerRunning = true;
        frozenElapsed = 0f;
        startTime = Time.time;
    }

    public void StopTimer()
    {
        if (!timerRunning) return;
        frozenElapsed = Time.time - startTime;  // lock the final time
        timerRunning = false;
    }

    public float GetElapsedTime()
    {
        return timerRunning ? (Time.time - startTime) : frozenElapsed;
    }

    public void SetSpeedVisible(bool visible)
    {
        if (speedLabel != null)
            speedLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------- Internals ----------
    private void UpdateTime(float seconds)
    {
        if (timeLabel == null) return;

        if (showMinutesSeconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int mins = s / 60;
            int secs = s % 60;
            timeLabel.text = $"{mins:00}:{secs:00}";
        }
        else
        {
            timeLabel.text = $"{seconds:0}";
        }
    }

    private void UpdateSpeed(float speed)
    {
        if (speedLabel == null) return;
        speedLabel.text = $"{speed:0.0}{speedSuffix}";
    }
}
