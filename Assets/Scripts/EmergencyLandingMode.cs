using UnityEngine;

public class EmergencyLandingMode : MonoBehaviour
{
    public enum FailureType
    {
        PartialEngineLoss,
        EngineFlameout,
        HydraulicsDamage,
        ControlSurfaceDamage,
        GearFailure
    }

    [Header("References")]
    [SerializeField] private PlaneController plane;
    [SerializeField] private HUDController hud;

    [Header("Emergency Tuning")]
    [SerializeField] private FailureType forcedFailure = FailureType.PartialEngineLoss;
    [SerializeField] private bool randomizeFailure = true;

    [Tooltip("Seconds AFTER ActivateEmergency before the emergency effects begin.")]
    [SerializeField] private float startDelay = 150f; // 2:30 minutes default

    [Tooltip("Seconds to fade in the initial failure effects once the delay ends (0 = instant).")]
    [SerializeField] private float rampInTime = 1.5f;

    [Tooltip("How long (seconds) the emergency gets progressively worse (after delay).")]
    [SerializeField] private float degradeDuration = 80f;

    [Tooltip("Extra drag added over time (makes energy management harder).")]
    [SerializeField] private float extraBleedMax = 1.6f;

    [Tooltip("Stall speed multiplier during emergency (higher = harder approach).")]
    [SerializeField] private float stallMult = 1.15f;

    [Header("Hydraulics / Control Damage")]
    [Range(0.2f, 1f)][SerializeField] private float pitchMult = 0.65f;
    [Range(0.2f, 1f)][SerializeField] private float rollMult = 0.45f;
    [Range(0.2f, 1f)][SerializeField] private float yawMult = 0.55f;

    [Header("Engine Loss")]
    [Range(0f, 1f)][SerializeField] private float partialEngineStart = 0.75f;
    [Range(0f, 1f)][SerializeField] private float partialEngineEnd = 0.45f;

    // ----------------------------
    // Altitude (AGL) using raycast
    // ----------------------------
    [Header("Altitude (AGL)")]
    [Tooltip("Layers that count as ground: Terrain, Runway, Ground meshes, etc.")]
    [SerializeField] private LayerMask groundMask = ~0;

    [Tooltip("Max distance to raycast downward for altitude checks.")]
    [SerializeField] private float maxGroundCheck = 10000f;

    [Tooltip("Start the ray slightly above the plane to avoid starting inside colliders.")]
    [SerializeField] private float rayStartOffset = 2f;

    [Tooltip("Optional smoothing strength for AGL (0 = no smoothing).")]
    [SerializeField] private float aglSmoothing = 8f;

    private float smoothedAGL;

    // ----------------------------
    // Universal Pre-Emergency Objective (Random Targets)
    // ----------------------------
    [Header("Universal Pre-Emergency Objective")]
    [SerializeField] private bool enableUniversalPreObjective = true;

    [Tooltip("Max allowed bank angle during pre-objective (degrees).")]
    [SerializeField] private float preMaxBank = 25f;

    [Tooltip("Max allowed pitch angle magnitude during pre-objective (degrees).")]
    [SerializeField] private float preMaxPitch = 15f;

    [Header("Pre-Objective Random Ranges (Realistic)")]
    [Tooltip("Target AGL altitude range (Unity units; if 1u ≈ 1m, this is meters).")]
    [SerializeField] private Vector2 preAltRange = new Vector2(150f, 300f);

    [Tooltip("Target speed range in km/h (prop/trainer feel).")]
    [SerializeField] private Vector2 preTargetSpeedRangeKmh = new Vector2(50f, 80f);

    [Tooltip("Allowed +/- band around target speed in km/h.")]
    [SerializeField] private Vector2 preSpeedBandRangeKmh = new Vector2(10f, 20f);

    [Tooltip("Seconds player must hold the condition to be 'READY'.")]
    [SerializeField] private Vector2 preHoldSecondsRange = new Vector2(3f, 5f);

    private float preTimer;
    private bool preComplete;

    // chosen per activation
    private float preSafeAltChosen;
    private float preTargetSpeedChosenKmh;
    private float preSpeedBandChosenKmh;
    private float preHoldChosen;

    // ----------------------------
    // Hold bonus scoring (earn points while holding, until emergency begins)
    // ----------------------------
    [Header("Hold Bonus Scoring")]
    [SerializeField] private int pointsPerSecondWhileHolding = 2; // tune (2 * 150s = 300 pts max)

    private float holdScoreAccumulator;

    // ----------------------------
    // Saved baseline values to restore in Standard mode
    // ----------------------------
    private float basePitch, baseRoll, baseYaw;
    private float baseStall;
    private float baseRollLevelStrength;
    private float baseThrustMult, baseSpeedBleed, baseAccelMult, baseMaxSpeedCap;

    private FailureType activeFailure;

    private float activateTime;   // when ActivateEmergency was called
    private float t0;             // when effects actually begin (after delay)
    private bool active;          // emergency running
    private bool pending;         // waiting for delay to elapse
    private bool startedEffects;  // initial failure has been applied (or ramping)

    void Awake()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (hud == null) hud = FindFirstObjectByType<HUDController>();
    }

    void OnEnable()
    {
        if (plane == null) return;

        // capture baseline once
        basePitch = plane.pitchTorque;
        baseRoll = plane.rollTorque;
        baseYaw = plane.yawTorque;

        baseStall = plane.stallSpeed;
        baseRollLevelStrength = plane.rollLevelStrength;

        baseThrustMult = plane.thrustMultiplier;
        baseSpeedBleed = plane.speedBleed;
        baseAccelMult = plane.accelMultiplier;
        baseMaxSpeedCap = plane.maxSpeedCap;

        smoothedAGL = 0f;
    }

    public void ActivateEmergency()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (hud == null) hud = FindFirstObjectByType<HUDController>();
        if (plane == null) return;

        activeFailure = randomizeFailure
            ? (FailureType)Random.Range(0, System.Enum.GetValues(typeof(FailureType)).Length)
            : forcedFailure;

        activateTime = Time.time;
        t0 = activateTime + Mathf.Max(0f, startDelay);

        pending = true;
        active = false;
        startedEffects = false;

        // start universal pre-objective (randomized once)
        preTimer = 0f;
        preComplete = false;
        RandomizePreObjectiveTargets();

        // reset hold-score accumulation
        holdScoreAccumulator = 0f;
    }

    void FixedUpdate()
    {
        if (plane == null) return;

        // -------- PRE PHASE (delay window) --------
        if (pending)
        {
            if (enableUniversalPreObjective && !preComplete)
                RunUniversalPreObjective();

            // Keep adding points WHILE the player is holding the condition
            AwardHoldPointsIfHoldingNow();

            if (Time.time < t0) return;

            // Delay finished -> start emergency now
            pending = false;
            active = true;

            // Base emergency feel (apply at the moment emergency begins)
            plane.maxSpeedCap = Mathf.Min(plane.maxSpeedCap, 0.75f);
            plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, 0.6f);
            plane.speedBleed = Mathf.Max(plane.speedBleed, 0.6f);

            plane.stallSpeed = baseStall * stallMult;

            // If no ramp, apply immediately; otherwise we'll ramp it below
            if (rampInTime <= 0f)
            {
                ApplyImmediateFailure(activeFailure, 1f);
                startedEffects = true;
            }
        }

        if (!active) return;

        // Ramp-in initial failure effects smoothly (optional)
        if (!startedEffects)
        {
            float ramp = Mathf.Clamp01((Time.time - t0) / Mathf.Max(0.0001f, rampInTime));
            ApplyImmediateFailure(activeFailure, ramp);

            if (ramp >= 1f) startedEffects = true;
        }

        // Progressive worsening (start counting from t0)
        float u = Mathf.Clamp01((Time.time - t0) / Mathf.Max(1f, degradeDuration));
        float worsen = SmoothStep01(u);

        // Always: gradually increase bleed so player must commit to runway
        plane.speedBleed = Mathf.Max(plane.speedBleed, baseSpeedBleed + extraBleedMax * worsen);

        switch (activeFailure)
        {
            case FailureType.PartialEngineLoss:
                plane.thrustMultiplier = Mathf.Lerp(partialEngineStart, partialEngineEnd, worsen);
                plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, Mathf.Lerp(0.55f, 0.35f, worsen));
                break;

            case FailureType.EngineFlameout:
                plane.thrustMultiplier = 0f;
                plane.accelMultiplier = 0.25f;
                plane.maxSpeedCap = Mathf.Max(plane.maxSpeedCap, 0.95f);
                break;

            case FailureType.HydraulicsDamage:
                plane.pitchTorque = basePitch * Mathf.Lerp(1f, pitchMult, worsen);
                plane.rollTorque = baseRoll * Mathf.Lerp(1f, rollMult, worsen);
                plane.yawTorque = baseYaw * Mathf.Lerp(1f, yawMult, worsen);
                plane.rollLevelStrength = Mathf.Lerp(baseRollLevelStrength, baseRollLevelStrength * 0.35f, worsen);
                break;

            case FailureType.ControlSurfaceDamage:
                plane.rollTorque = baseRoll * Mathf.Lerp(1f, 0.55f, worsen);
                plane.yawTorque = baseYaw * Mathf.Lerp(1f, 0.45f, worsen);
                plane.pitchTorque = basePitch * Mathf.Lerp(1f, 0.7f, worsen);
                break;

            case FailureType.GearFailure:
                plane.thrustMultiplier = Mathf.Min(plane.thrustMultiplier, 0.7f);
                plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, 0.5f);
                break;
        }
    }

    private void AwardHoldPointsIfHoldingNow()
    {
        if (hud == null || pointsPerSecondWhileHolding <= 0) return;

        // Only earn during pre-emergency delay
        if (!pending) return;
        if (Time.time >= t0) return;

        // Earn ONLY while currently meeting the condition
        if (!IsPreObjectiveSatisfiedNow()) return;

        holdScoreAccumulator += pointsPerSecondWhileHolding * Time.fixedDeltaTime;

        int give = Mathf.FloorToInt(holdScoreAccumulator);
        if (give > 0)
        {
            hud.AddScore(give);
            holdScoreAccumulator -= give;
        }
    }

    // IMPORTANT: now takes "blend" so we can ramp in smoothly
    private void ApplyImmediateFailure(FailureType f, float blend01)
    {
        blend01 = Mathf.Clamp01(blend01);

        switch (f)
        {
            case FailureType.PartialEngineLoss:
                plane.thrustMultiplier = Mathf.Lerp(baseThrustMult, partialEngineStart, blend01);
                plane.maxSpeedCap = Mathf.Min(plane.maxSpeedCap, Mathf.Lerp(baseMaxSpeedCap, 0.7f, blend01));
                break;

            case FailureType.EngineFlameout:
                plane.thrustMultiplier = Mathf.Lerp(baseThrustMult, 0f, blend01);
                break;

            case FailureType.HydraulicsDamage:
                plane.pitchTorque = Mathf.Lerp(basePitch, basePitch * 0.85f, blend01);
                plane.rollTorque = Mathf.Lerp(baseRoll, baseRoll * 0.75f, blend01);
                plane.yawTorque = Mathf.Lerp(baseYaw, baseYaw * 0.85f, blend01);
                break;

            case FailureType.ControlSurfaceDamage:
                plane.rollTorque = Mathf.Lerp(baseRoll, baseRoll * 0.75f, blend01);
                plane.yawTorque = Mathf.Lerp(baseYaw, baseYaw * 0.7f, blend01);
                break;

            case FailureType.GearFailure:
                // flight mostly okay; judge handles touchdown strictness
                break;
        }
    }

    // ----------------------------
    // Altitude Helpers (AGL)
    // ----------------------------
    public float GetAltitudeAGL()
    {
        if (plane == null) return 0f;

        Vector3 pos = plane.transform.position;
        Vector3 origin = pos + Vector3.up * rayStartOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxGroundCheck, groundMask, QueryTriggerInteraction.Ignore))
            return origin.y - hit.point.y;

        return pos.y;
    }

    public float GetAltitudeAGL_Smoothed()
    {
        float agl = GetAltitudeAGL();
        if (aglSmoothing <= 0f) return agl;

        float k = 1f - Mathf.Exp(-aglSmoothing * Time.deltaTime);
        smoothedAGL = Mathf.Lerp(smoothedAGL, agl, k);
        return smoothedAGL;
    }

    public float GetSpeedKmh()
    {
        if (plane == null) return 0f;
        Rigidbody rb = plane.GetComponent<Rigidbody>();
        if (rb == null) return 0f;
        return rb.linearVelocity.magnitude * 3.6f;
    }

    // ----------------------------
    // Universal Pre-Emergency Objective
    // ----------------------------
    private void RandomizePreObjectiveTargets()
    {
        preSafeAltChosen = Random.Range(preAltRange.x, preAltRange.y);
        preTargetSpeedChosenKmh = Random.Range(preTargetSpeedRangeKmh.x, preTargetSpeedRangeKmh.y);
        preSpeedBandChosenKmh = Random.Range(preSpeedBandRangeKmh.x, preSpeedBandRangeKmh.y);
        preHoldChosen = Random.Range(preHoldSecondsRange.x, preHoldSecondsRange.y);

        // nicer numbers
        preSafeAltChosen = Mathf.Round(preSafeAltChosen / 10f) * 10f;
        preTargetSpeedChosenKmh = Mathf.Round(preTargetSpeedChosenKmh / 5f) * 5f;
        preSpeedBandChosenKmh = Mathf.Round(preSpeedBandChosenKmh / 5f) * 5f;
        preHoldChosen = Mathf.Round(preHoldChosen * 10f) / 10f;

        // safety clamps
        preSpeedBandChosenKmh = Mathf.Max(5f, preSpeedBandChosenKmh);
        preHoldChosen = Mathf.Max(1.5f, preHoldChosen);
        preSafeAltChosen = Mathf.Max(20f, preSafeAltChosen);
    }

    private void RunUniversalPreObjective()
    {
        float altAgl = GetAltitudeAGL_Smoothed();
        float spdKmh = GetSpeedKmh();

        float pitch = plane.transform.eulerAngles.x; if (pitch > 180f) pitch -= 360f;
        float roll = plane.transform.eulerAngles.z; if (roll > 180f) roll -= 360f;

        bool altOk = altAgl >= preSafeAltChosen;
        bool speedOk = spdKmh >= (preTargetSpeedChosenKmh - preSpeedBandChosenKmh) &&
                       spdKmh <= (preTargetSpeedChosenKmh + preSpeedBandChosenKmh);
        bool stableOk = Mathf.Abs(roll) <= preMaxBank && Mathf.Abs(pitch) <= preMaxPitch;

        if (altOk && speedOk && stableOk) preTimer += Time.fixedDeltaTime;
        else preTimer = 0f;

        if (preTimer >= preHoldChosen) preComplete = true;
    }

    public bool IsPreObjectiveSatisfiedNow()
    {
        if (plane == null) return false;

        float altAgl = GetAltitudeAGL_Smoothed();
        float spdKmh = GetSpeedKmh();

        float pitch = plane.transform.eulerAngles.x; if (pitch > 180f) pitch -= 360f;
        float roll = plane.transform.eulerAngles.z; if (roll > 180f) roll -= 360f;

        bool altOk = altAgl >= preSafeAltChosen;
        bool speedOk = spdKmh >= (preTargetSpeedChosenKmh - preSpeedBandChosenKmh) &&
                       spdKmh <= (preTargetSpeedChosenKmh + preSpeedBandChosenKmh);
        bool stableOk = Mathf.Abs(roll) <= preMaxBank && Mathf.Abs(pitch) <= preMaxPitch;

        return altOk && speedOk && stableOk;
    }

    // Clean banner text for infoTxt (no emergency words)
    public string GetInfoTextSimple()
    {
        if (!enableUniversalPreObjective) return "";

        float alt = GetAltitudeAGL_Smoothed();
        float spd = GetSpeedKmh();

        bool altOk = alt >= preSafeAltChosen;
        bool spdOk = spd >= (preTargetSpeedChosenKmh - preSpeedBandChosenKmh) &&
                     spd <= (preTargetSpeedChosenKmh + preSpeedBandChosenKmh);

        float remain = Mathf.Max(0f, preHoldChosen - preTimer);

        string line1 = $"REACH {preSafeAltChosen:0}m AGL • {preTargetSpeedChosenKmh:0}±{preSpeedBandChosenKmh:0} km/h if";

        if (preComplete)
            return "READY\nEMERGENCY IMMINENT…";

        if (altOk && spdOk && IsPreObjectiveSatisfiedNow())
            return $"GOOD • HOLDING…\n{remain:0.0}s LEFT";

        if (!altOk)
            return $"{line1}\nCLIMB ({alt:0}m) • HOLD {remain:0.0}s";

        return $"{line1}\nADJUST SPEED ({spd:0} km/h) • HOLD {remain:0.0}s";
    }

    // If you prefer one-line info:
    public string GetInfoText()
    {
        if (!enableUniversalPreObjective) return "";
        float alt = GetAltitudeAGL_Smoothed();
        float spd = GetSpeedKmh();
        float remain = Mathf.Max(0f, preHoldChosen - preTimer);
        return $"ALT {alt:0}m ≥{preSafeAltChosen:0}m • SPD {spd:0}km/h {preTargetSpeedChosenKmh:0}±{preSpeedBandChosenKmh:0} • HOLD {remain:0.0}s";
    }

    public bool IsPending() => pending;
    public bool IsActive() => active;

    public FailureType GetActiveFailure() => activeFailure;

    public void ResetToBaseline()
    {
        if (plane == null) return;

        plane.pitchTorque = basePitch;
        plane.rollTorque = baseRoll;
        plane.yawTorque = baseYaw;

        plane.stallSpeed = baseStall;
        plane.rollLevelStrength = baseRollLevelStrength;

        plane.thrustMultiplier = baseThrustMult;
        plane.speedBleed = baseSpeedBleed;
        plane.accelMultiplier = baseAccelMult;
        plane.maxSpeedCap = baseMaxSpeedCap;

        active = false;
        pending = false;
        startedEffects = false;

        preTimer = 0f;
        preComplete = false;

        holdScoreAccumulator = 0f;
    }

    private float SmoothStep01(float x) => x * x * (3f - 2f * x);
}
