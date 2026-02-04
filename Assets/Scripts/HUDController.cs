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

    // NEW
    [Header("Extra Labels (UXML)")]
    [SerializeField] private string altitudeLabelName = "altitudeTxt";
    [SerializeField] private string infoLabelName = "infoTxt";

    [Header("References")]
    [SerializeField] private PlaneController plane;

    // NEW
    [SerializeField] private EmergencyLandingMode emergency;

    [Header("Formatting")]
    [SerializeField] private bool showMinutesSeconds = true;
    [SerializeField] private string speedSuffix = " m/s";
    [SerializeField] private float speedMultiplier = 1f;

    // NEW (altitude formatting)
    [SerializeField] private string altitudeSuffix = " m";
    [SerializeField] private bool altitudeUseAGL = true; // use AGL from EmergencyLandingMode if possible

    private Label timeLabel;
    private Label scoreLabel;
    private Label speedLabel;

    // NEW
    private Label altitudeLabel;
    private Label infoLabel;

    private float startTime;
    private float frozenElapsed;     // final time once stopped
    private bool timerRunning = true;

    private int score;

    void Awake()
    {
        if (hudDocument == null) hudDocument = GetComponent<UIDocument>();
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();

        var root = hudDocument.rootVisualElement;

        timeLabel = root.Q<Label>(timeLabelName);
        scoreLabel = root.Q<Label>(scoreLabelName);
        speedLabel = root.Q<Label>(speedLabelName);

        // NEW binds
        altitudeLabel = root.Q<Label>(altitudeLabelName);
        infoLabel = root.Q<Label>(infoLabelName);

        if (timeLabel == null) Debug.LogError($"HUDController: Missing Label '{timeLabelName}' in HUD UXML.");
        if (scoreLabel == null) Debug.LogError($"HUDController: Missing Label '{scoreLabelName}' in HUD UXML.");
        if (speedLabel == null) Debug.LogError($"HUDController: Missing Label '{speedLabelName}' in HUD UXML.");

        // NEW: these are optional, so warn (not error)
        if (altitudeLabel == null) Debug.LogWarning($"HUDController: Missing Label '{altitudeLabelName}' in HUD UXML.");
        if (infoLabel == null) Debug.LogWarning($"HUDController: Missing Label '{infoLabelName}' in HUD UXML.");

        startTime = Time.time;

        SetScore(0);
        UpdateTime(0f);
        UpdateSpeed(0f);
        UpdateAltitude(0f);

        // Hide info by default
        if (infoLabel != null)
            infoLabel.style.display = DisplayStyle.None;
    }

    void Update()
    {
        // TIME
        float elapsed = timerRunning ? (Time.time - startTime) : frozenElapsed;
        UpdateTime(elapsed);

        // SPEED (your original behavior)
        if (plane != null)
        {
            float spd = plane.GetCurrentSpeed() * speedMultiplier;
            UpdateSpeed(spd);
        }

        // ALTITUDE (updates every frame)
        if (altitudeLabel != null)
        {
            float alt = 0f;

            if (altitudeUseAGL && emergency != null)
            {
                alt = emergency.GetAltitudeAGL_Smoothed();
            }
            else if (plane != null)
            {
                alt = plane.transform.position.y;
            }

            UpdateAltitude(alt);
        }

        // INFO (only show during the pre-emergency delay window)
        if (infoLabel != null && emergency != null)
        {
            if (emergency.IsPending())
            {
                infoLabel.style.display = DisplayStyle.Flex;

                // Needs these methods in EmergencyLandingMode:
                // - GetInfoText()
                // - IsPreObjectiveSatisfiedNow()
                infoLabel.text = emergency.GetInfoText();

                // Optional styling toggle (warning when failing current requirements)
                bool okNow = emergency.IsPreObjectiveSatisfiedNow();

                if (!okNow) infoLabel.AddToClassList("info-banner--warning");
                else infoLabel.RemoveFromClassList("info-banner--warning");
            }
            else
            {
                // Hide once emergency begins (you said: "don't include emergency stuff" in that text)
                infoLabel.style.display = DisplayStyle.None;
            }
        }
    }

    // ---------- Public API ----------
    public void SetScore(int newScore)
    {
        score = newScore;
        if (scoreLabel != null)
            scoreLabel.text = $"{score}";
    }

    public void AddScore(int delta) => SetScore(score + delta);

    public int GetScore() => score;

    public void ResetTimer()
    {
        timerRunning = true;
        frozenElapsed = 0f;
        startTime = Time.time;
    }

    public void StopTimer()
    {
        if (!timerRunning) return;
        frozenElapsed = Time.time - startTime;
        timerRunning = false;
    }

    // Alias (LandingJudge calls this)
    public void FreezeTimer() => StopTimer();

    public void UnfreezeTimer()
    {
        if (timerRunning) return;
        timerRunning = true;
        startTime = Time.time - frozenElapsed;
    }

    public float GetElapsedTimeSeconds()
    {
        return timerRunning ? (Time.time - startTime) : frozenElapsed;
    }

    public float GetElapsedTime() => GetElapsedTimeSeconds();

    public void SetSpeedVisible(bool visible)
    {
        if (speedLabel != null)
            speedLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // NEW: altitude visibility helper
    public void SetAltitudeVisible(bool visible)
    {
        if (altitudeLabel != null)
            altitudeLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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

    // NEW
    private void UpdateAltitude(float alt)
    {
        if (altitudeLabel == null) return;
        altitudeLabel.text = $"{alt:0}{altitudeSuffix}";
    }
}
