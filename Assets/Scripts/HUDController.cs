using System.Collections;
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

    // ---------------- Emergency Banner (Image) ----------------
    [Header("Emergency Banner (UXML)")]
    [SerializeField] private string emergencyBannerImageName = "emergencyBannerImage";

    [Header("Emergency Banner Textures (drag into Inspector)")]
    [SerializeField] private Texture2D banner_PartialEngineLoss;
    [SerializeField] private Texture2D banner_EngineFlameout;
    [SerializeField] private Texture2D banner_HydraulicsDamage;
    [SerializeField] private Texture2D banner_ControlSurfaceDamage;
    [SerializeField] private Texture2D banner_GearFailure;
    [SerializeField] private Texture2D banner_Default;

    [Header("Emergency Banner Flash")]
    [SerializeField] private bool flashBanner = true;
    [SerializeField] private float flashIntervalSeconds = 0.35f;
    [SerializeField, Range(0f, 1f)] private float flashDimOpacity = 0.15f;

    private Image emergencyBannerImage;
    private Coroutine bannerFlashRoutine;
    private bool bannerFlashOn;
    // -----------------------------------------------------------

    private Label timeLabel;
    private Label scoreLabel;
    private Label speedLabel;
    private Label altitudeLabel;
    private Label infoLabel;

    // Countdown override
    private string infoOverrideText = null;

    private float startTime;
    private float frozenElapsed;
    private bool timerRunning = true;
    private int score;

    private void Awake()
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

        // Emergency banner image
        emergencyBannerImage = root.Q<Image>(emergencyBannerImageName);
        if (emergencyBannerImage == null)
            Debug.LogWarning($"HUDController: Missing Image '{emergencyBannerImageName}'. (Name it in UXML/UI Builder)");
        else
        {
            emergencyBannerImage.style.display = DisplayStyle.None;
            emergencyBannerImage.style.opacity = 1f;
        }

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

    private void OnDisable()
    {
        StopBannerFlash();
    }

    private void Update()
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

        // ALTITUDE (AGL from emergency if available, otherwise from fuel addon if available)
        if (altitudeLabel != null)
        {
            float alt = 0f;

            if (altitudeUseAGL && emergency != null)
                alt = emergency.GetAltitudeAGL_Smoothed();
            else if (altitudeUseAGL && fuelMode != null)
                alt = fuelMode.GetAltitudeAGL_Smoothed();
            else if (plane != null)
                alt = plane.transform.position.y;

            UpdateAltitude(alt);
        }

        // ---------------- INFO (Countdown > Fuel pre-objective > Emergency) ----------------
        bool infoVisibleNow = false;

        if (infoLabel != null)
        {
            // 1) Countdown override
            if (!string.IsNullOrEmpty(infoOverrideText))
            {
                infoVisibleNow = true;
                infoLabel.style.display = DisplayStyle.Flex;
                infoLabel.text = infoOverrideText;
                infoLabel.RemoveFromClassList("info-banner--warning");
            }
            // 2) Fuel pre-objective
            else if (fuelMode != null && fuelMode.IsPreObjectiveActive)
            {
                infoVisibleNow = true;
                infoLabel.style.display = DisplayStyle.Flex;
                infoLabel.text = fuelMode.GetPreObjectiveInfoText();
                infoLabel.RemoveFromClassList("info-banner--warning");
            }
            // 3) Emergency pre-objective
            else if (emergency != null && emergency.ShouldShowInfoText())
            {
                infoVisibleNow = true;
                infoLabel.style.display = DisplayStyle.Flex;
                infoLabel.text = emergency.GetInfoText();

                bool okNow = emergency.IsPreObjectiveSatisfiedNow();
                if (!okNow) infoLabel.AddToClassList("info-banner--warning");
                else infoLabel.RemoveFromClassList("info-banner--warning");
            }
            else
            {
                infoLabel.style.display = DisplayStyle.None;
                infoLabel.RemoveFromClassList("info-banner--warning");
            }
        }
        // -------------------------------------------------------------------------------

        // EMERGENCY BANNER: show ONLY after info is gone
        UpdateEmergencyBanner(infoVisibleNow);

        // FUEL UI
        UpdateFuelUI();
    }

    private void UpdateEmergencyBanner(bool infoVisibleNow)
    {
        if (emergencyBannerImage == null || emergency == null) return;

        bool shouldShow = emergency.IsActive() && !infoVisibleNow;

        emergencyBannerImage.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;

        if (!shouldShow)
        {
            StopBannerFlash();
            return;
        }

        Texture2D tex = GetBannerTexture(emergency.GetActiveFailure());
        if (tex != null && emergencyBannerImage.image != tex)
            emergencyBannerImage.image = tex;

        if (flashBanner) StartBannerFlash();
        else StopBannerFlash();
    }

    private Texture2D GetBannerTexture(EmergencyLandingMode.FailureType t)
    {
        return t switch
        {
            EmergencyLandingMode.FailureType.PartialEngineLoss => banner_PartialEngineLoss,
            EmergencyLandingMode.FailureType.EngineFlameout => banner_EngineFlameout,
            EmergencyLandingMode.FailureType.HydraulicsDamage => banner_HydraulicsDamage,
            EmergencyLandingMode.FailureType.ControlSurfaceDamage => banner_ControlSurfaceDamage,
            EmergencyLandingMode.FailureType.GearFailure => banner_GearFailure,
            _ => banner_Default
        };
    }

    private void StartBannerFlash()
    {
        if (bannerFlashRoutine != null) return;
        bannerFlashRoutine = StartCoroutine(BannerFlashLoop());
    }

    private void StopBannerFlash()
    {
        if (bannerFlashRoutine != null)
        {
            StopCoroutine(bannerFlashRoutine);
            bannerFlashRoutine = null;
        }

        bannerFlashOn = false;
        if (emergencyBannerImage != null)
            emergencyBannerImage.style.opacity = 1f;
    }

    private IEnumerator BannerFlashLoop()
    {
        while (true)
        {
            bannerFlashOn = !bannerFlashOn;
            if (emergencyBannerImage != null)
                emergencyBannerImage.style.opacity = bannerFlashOn ? 1f : flashDimOpacity;

            yield return new WaitForSecondsRealtime(flashIntervalSeconds);
        }
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

    // Countdown helpers
    public void SetInfoOverride(string text) => infoOverrideText = text;
    public void ClearInfoOverride() => infoOverrideText = null;

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