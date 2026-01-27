using UnityEngine;

public class EmergencyLandingMode : MonoBehaviour
{
    public enum FailureType
    {
        PartialEngineLoss,   // power degrades over time (most fun)
        EngineFlameout,      // no thrust (glide landing)
        HydraulicsDamage,    // reduced control authority
        ControlSurfaceDamage,// asymmetric/limited controls
        GearFailure          // landing harsher (judge thresholds tighter)
    }

    [Header("References")]
    [SerializeField] private PlaneController plane;

    [Header("Emergency Tuning")]
    [SerializeField] private FailureType forcedFailure = FailureType.PartialEngineLoss;
    [SerializeField] private bool randomizeFailure = true;

    [Tooltip("How long (seconds) the emergency gets progressively worse.")]
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

    // Saved baseline values to restore in Standard mode
    private float basePitch, baseRoll, baseYaw;
    private float baseStall;
    private float baseRollLevelStrength;
    private float baseThrustMult, baseSpeedBleed, baseAccelMult, baseMaxSpeedCap;

    private FailureType activeFailure;
    private float t0;
    private bool active;

    void Awake()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
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
    }

    public void ActivateEmergency()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (plane == null) return;

        activeFailure = randomizeFailure
            ? (FailureType)Random.Range(0, System.Enum.GetValues(typeof(FailureType)).Length)
            : forcedFailure;

        // Start worsening from now
        t0 = Time.time;
        active = true;

        // Base emergency feel (your ModeManager already sets some of these; we reinforce)
        plane.maxSpeedCap = Mathf.Min(plane.maxSpeedCap, 0.75f);
        plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, 0.6f);
        plane.speedBleed = Mathf.Max(plane.speedBleed, 0.6f);

        // Make stall more punishing so player must manage airspeed
        plane.stallSpeed = baseStall * stallMult;

        // Apply immediate failure effects
        ApplyImmediateFailure(activeFailure);
    }

    void FixedUpdate()
    {
        if (!active || plane == null) return;

        float u = Mathf.Clamp01((Time.time - t0) / Mathf.Max(1f, degradeDuration)); // 0..1
        float worsen = SmoothStep01(u);

        // Always: gradually increase bleed so player must commit to runway
        plane.speedBleed = Mathf.Max(plane.speedBleed, baseSpeedBleed + extraBleedMax * worsen);

        // Failure-specific progressive behavior
        switch (activeFailure)
        {
            case FailureType.PartialEngineLoss:
                // thrust gradually degrades
                plane.thrustMultiplier = Mathf.Lerp(partialEngineStart, partialEngineEnd, worsen);
                // throttle response feels sluggish
                plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, Mathf.Lerp(0.55f, 0.35f, worsen));
                break;

            case FailureType.EngineFlameout:
                plane.thrustMultiplier = 0f;
                plane.accelMultiplier = 0.25f;
                // don’t cap speed too low; dives matter for energy
                plane.maxSpeedCap = Mathf.Max(plane.maxSpeedCap, 0.95f);
                break;

            case FailureType.HydraulicsDamage:
                // progressively harder to control precisely
                plane.pitchTorque = basePitch * Mathf.Lerp(1f, pitchMult, worsen);
                plane.rollTorque = baseRoll * Mathf.Lerp(1f, rollMult, worsen);
                plane.yawTorque = baseYaw * Mathf.Lerp(1f, yawMult, worsen);
                // reduce auto-level so they must fly it
                plane.rollLevelStrength = Mathf.Lerp(baseRollLevelStrength, baseRollLevelStrength * 0.35f, worsen);
                break;

            case FailureType.ControlSurfaceDamage:
                // asymmetric “damage”: slightly different effectiveness left/right via yaw bias
                plane.rollTorque = baseRoll * Mathf.Lerp(1f, 0.55f, worsen);
                plane.yawTorque = baseYaw * Mathf.Lerp(1f, 0.45f, worsen);
                plane.pitchTorque = basePitch * Mathf.Lerp(1f, 0.7f, worsen);
                break;

            case FailureType.GearFailure:
                // flight mostly okay but landing should be stricter via judge (handled there)
                plane.thrustMultiplier = Mathf.Min(plane.thrustMultiplier, 0.7f);
                plane.accelMultiplier = Mathf.Min(plane.accelMultiplier, 0.5f);
                break;
        }
    }

    private void ApplyImmediateFailure(FailureType f)
    {
        // Small instant changes so emergency feels immediate
        switch (f)
        {
            case FailureType.PartialEngineLoss:
                plane.thrustMultiplier = partialEngineStart;
                plane.maxSpeedCap = Mathf.Min(plane.maxSpeedCap, 0.7f);
                break;

            case FailureType.EngineFlameout:
                plane.thrustMultiplier = 0f;
                break;

            case FailureType.HydraulicsDamage:
                plane.pitchTorque = basePitch * 0.85f;
                plane.rollTorque = baseRoll * 0.75f;
                plane.yawTorque = baseYaw * 0.85f;
                break;

            case FailureType.ControlSurfaceDamage:
                plane.rollTorque = baseRoll * 0.75f;
                plane.yawTorque = baseYaw * 0.7f;
                break;

            case FailureType.GearFailure:
                // nothing to flight now; judge will tighten touchdown thresholds
                break;
        }
    }

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
    }

    private float SmoothStep01(float x) => x * x * (3f - 2f * x);
}
