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

    [Header("Extra Labels (UXML)")]
    [SerializeField] private string altitudeLabelName = "altitudeTxt";
    [SerializeField] private string infoLabelName = "infoTxt";

    // ---------------- FUEL (Clean Vertical Bar) ----------------
    [Header("Fuel UI (UXML)")]
    [SerializeField] private string fuelGroupName = "fuelGroup";
    [SerializeField] private string fuelFrameName = "fuelFrame";
    [SerializeField] private string fuelFillName = "fuelFill";
    [SerializeField] private string fuelPctName = "fuelPct"; // optional

    [SerializeField] private FuelModeAddon fuelMode; // on Plane
    [SerializeField, Range(0f, 1f)] private float lowFuelThreshold = 0.2f;
    [SerializeField] private float fuelSmooth = 10f;

    private VisualElement fuelGroup;
    private VisualElement fuelFrame;
    private VisualElement fuelFill;
    private Label fuelPct;

    private bool fuelUIVisible;
    private float fuelDisplay01 = 1f;
    // -----------------------------------------------------------

    [Header("References")]
    [SerializeField] private PlaneController plane;
    [SerializeField] private EmergencyLandingMode emergency;

    [Header("Formatting")]
    [SerializeField] private bool showMinutesSeconds = true;
    [SerializeField] private string speedSuffix = " m/s";
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Altitude Formatting")]
    [SerializeField] private string altitudeSuffix = " m";
    [SerializeField] private bool altitudeUseAGL = true;

    private Label timeLabel;
    private Label scoreLabel;
    private Label speedLabel;
    private Label altitudeLabel;
    private Label infoLabel;

    private float startTime;
    private float frozenElapsed;
    private bool timerRunning = true;
    private int score;

    void Awake()
    {
        if (hudDocument == null) hudDocument = GetComponent<UIDocument>();
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();
        if (fuelMode == null) fuelMode = FindFirstObjectByType<FuelModeAddon>();

        if (hudDocument == null)
        {
            Debug.LogError("HUDController: No UIDocument assigned/found.");
            enabled = false;
            return;
        }

        var root = hudDocument.rootVisualElement;

        // Base HUD
        timeLabel = root.Q<Label>(timeLabelName);
        scoreLabel = root.Q<Label>(scoreLabelName);
        speedLabel = root.Q<Label>(speedLabelName);

        altitudeLabel = root.Q<Label>(altitudeLabelName);
        infoLabel = root.Q<Label>(infoLabelName);

        if (timeLabel == null) Debug.LogError($"HUDController: Missing Label '{timeLabelName}'.");
        if (scoreLabel == null) Debug.LogError($"HUDController: Missing Label '{scoreLabelName}'.");
        if (speedLabel == null) Debug.LogError($"HUDController: Missing Label '{speedLabelName}'.");

        if (altitudeLabel == null) Debug.LogWarning($"HUDController: Missing Label '{altitudeLabelName}'.");
        if (infoLabel == null) Debug.LogWarning($"HUDController: Missing Label '{infoLabelName}'.");

        // Fuel UI
        fuelGroup = root.Q<VisualElement>(fuelGroupName);
        fuelFrame = root.Q<VisualElement>(fuelFrameName);
        fuelFill = root.Q<VisualElement>(fuelFillName);
        fuelPct = root.Q<Label>(fuelPctName);

        if (fuelGroup == null) Debug.LogWarning($"HUDController: Missing '{fuelGroupName}'.");
        if (fuelFrame == null) Debug.LogWarning($"HUDController: Missing '{fuelFrameName}'.");
        if (fuelFill == null) Debug.LogWarning($"HUDController: Missing '{fuelFillName}'.");

        // Default hidden
        if (fuelGroup != null) fuelGroup.style.display = DisplayStyle.None;
        fuelUIVisible = false;

        if (infoLabel != null) infoLabel.style.display = DisplayStyle.None;

        // Init
        startTime = Time.time;
        SetScore(0);
        UpdateTime(0f);
        UpdateSpeed(0f);
        UpdateAltitude(0f);
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

        // ALTITUDE
        if (altitudeLabel != null)
        {
            float alt = 0f;
            if (altitudeUseAGL && emergency != null) alt = emergency.GetAltitudeAGL_Smoothed();
            else if (plane != null) alt = plane.transform.position.y;

            UpdateAltitude(alt);
        }

        // INFO
        if (infoLabel != null && emergency != null)
        {
            if (emergency.IsPending())
            {
                infoLabel.style.display = DisplayStyle.Flex;
                infoLabel.text = emergency.GetInfoText();

                bool okNow = emergency.IsPreObjectiveSatisfiedNow();
                if (!okNow) infoLabel.AddToClassList("info-banner--warning");
                else infoLabel.RemoveFromClassList("info-banner--warning");
            }
            else
            {
                infoLabel.style.display = DisplayStyle.None;
            }
        }

        // FUEL
        UpdateFuelUI();
    }

    private void UpdateFuelUI()
    {
        bool shouldShowFuel = (fuelMode != null && fuelMode.fuelModeEnabled);

        if (fuelGroup != null && shouldShowFuel != fuelUIVisible)
        {
            fuelGroup.style.display = shouldShowFuel ? DisplayStyle.Flex : DisplayStyle.None;
            fuelUIVisible = shouldShowFuel;
        }

        if (!shouldShowFuel || fuelMode == null || fuelFill == null || fuelFrame == null) return;

        float target = Mathf.Clamp01(fuelMode.Fuel01);
        fuelDisplay01 = Mathf.Lerp(fuelDisplay01, target, Time.deltaTime * fuelSmooth);

        // Uses fixed frame height from USS (reliable)
        float frameH = fuelFrame.resolvedStyle.height;
        float pad = fuelFrame.resolvedStyle.paddingTop + fuelFrame.resolvedStyle.paddingBottom;
        float innerH = Mathf.Max(0f, frameH - pad);

        if (innerH > 0f)
            fuelFill.style.height = innerH * fuelDisplay01;

        fuelFill.EnableInClassList("fuel-fill--low", target <= lowFuelThreshold);

        if (fuelPct != null)
            fuelPct.text = $"{Mathf.RoundToInt(target * 100f)}%";
    }

    // ---------- Public API ----------
    public void SetScore(int newScore)
    {
        score = newScore;
        if (scoreLabel != null) scoreLabel.text = $"{score}";
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

    public void FreezeTimer() => StopTimer();

    public void UnfreezeTimer()
    {
        if (timerRunning) return;
        timerRunning = true;
        startTime = Time.time - frozenElapsed;
    }

    public float GetElapsedTimeSeconds() => timerRunning ? (Time.time - startTime) : frozenElapsed;

    public void SetSpeedVisible(bool visible)
    {
        if (speedLabel != null)
            speedLabel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

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

    private void UpdateAltitude(float alt)
    {
        if (altitudeLabel == null) return;
        altitudeLabel.text = $"{alt:0}{altitudeSuffix}";
    }
}
