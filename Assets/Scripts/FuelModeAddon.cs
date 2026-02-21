using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlaneController))]
[RequireComponent(typeof(Rigidbody))]
public class FuelModeAddon : MonoBehaviour
{
    [Header("Enable only in Fuel Mode")]
    public bool fuelModeEnabled = true;

    [Header("Fuel")]
    public float maxFuel = 100f;
    public float fuel = 100f;

    [Header("Drain (per second)")]
    public float baseDrain = 0.6f;
    public float speedDrain = 0.02f;
    public float climbDrain = 0.25f;
    public float turnDrain = 0.12f;

    [Header("When fuel is empty (engine cut)")]
    [Tooltip("Extra drag/bleed applied via PlaneController.speedBleed when fuel is empty.")]
    public float extraSpeedBleedWhenEmpty = 8f;

    [Tooltip("Sets plane throttle01 to 0 when fuel is empty.")]
    public bool forceThrottleZero = true;

    [Tooltip("Optional: add extra Rigidbody damping when fuel is empty (helps it slow down).")]
    public float engineOffLinearDamping = 0.6f;

    [Header("Crash after fuel empty")]
    public float crashMinImpactSpeed = 6f;

    [Tooltip("If you want runway impacts to count as crash when out of fuel, set to your runway tag.")]
    public string runwayTag = "Runway";

    [Tooltip("Safety fallback: restart if never crash (e.g., no ground collider).")]
    public float outOfFuelTimeout = 15f;

    // ---------------- Pause fuel drain (grace period / end screen only) ----------------
    [Header("Pause (grace/end screen only)")]
    [SerializeField] private bool pauseFuelDrain = false;
    public void SetFuelPaused(bool paused) => pauseFuelDrain = paused;
    public bool IsFuelPaused => pauseFuelDrain;
    // -------------------------------------------------------------------------------

    // ======================================================
    // PRE-OBJECTIVE (same as emergency idea)
    // NOTE: Fuel STILL drains during pre-objective.
    // ======================================================
    [Header("Pre-Objective (Fuel Mode)")]
    [SerializeField] private bool enablePreObjective = true;

    [Tooltip("Max allowed bank angle during pre-objective (degrees).")]
    [SerializeField] private float preMaxBank = 25f;

    [Tooltip("Max allowed pitch angle magnitude during pre-objective (degrees).")]
    [SerializeField] private float preMaxPitch = 15f;

    [Header("Pre-Objective Random Ranges")]
    [Tooltip("Target AGL altitude range (Unity units; if 1u ≈ 1m, this is meters).")]
    [SerializeField] private Vector2 preAltRange = new Vector2(150f, 300f);

    [Tooltip("Target speed range in km/h.")]
    [SerializeField] private Vector2 preTargetSpeedRangeKmh = new Vector2(50f, 80f);

    [Tooltip("Allowed +/- band around target speed in km/h.")]
    [SerializeField] private Vector2 preSpeedBandRangeKmh = new Vector2(10f, 20f);

    [Tooltip("Seconds player must hold the condition to be 'READY'.")]
    [SerializeField] private Vector2 preHoldSecondsRange = new Vector2(3f, 5f);

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

    [Header("Hold Bonus Scoring (optional)")]
    [SerializeField] private bool awardPointsWhileHolding = false;
    [SerializeField] private int pointsPerSecondWhileHolding = 2;
    private float holdScoreAccumulator;

    private float preTimer;
    private bool preComplete;

    private float preSafeAltChosen;
    private float preTargetSpeedChosenKmh;
    private float preSpeedBandChosenKmh;
    private float preHoldChosen;

    public bool IsPreObjectiveComplete => preComplete;
    public bool IsPreObjectiveActive => fuelModeEnabled && enablePreObjective && !preComplete;

    // Optional: if you want HUD to show this text
    public string GetPreObjectiveInfoText()
    {
        if (!enablePreObjective || preComplete) return "";

        float alt = GetAltitudeAGL_Smoothed();
        float spd = GetSpeedKmh();
        float remain = Mathf.Max(0f, preHoldChosen - preTimer);

        return $"ALT {alt:0}m ≥{preSafeAltChosen:0}m • SPD {spd:0}km/h {preTargetSpeedChosenKmh:0}±{preSpeedBandChosenKmh:0} • HOLD {remain:0.0}s";
    }

    public bool IsOutOfFuel => fuelModeEnabled && fuel <= 0.001f;
    public float Fuel01 => maxFuel <= 0f ? 0f : Mathf.Clamp01(fuel / maxFuel);

    private PlaneController plane;
    private Rigidbody rb;
    private LandingJudge landingJudge;
    private HUDController hud;

    private float origThrustMultiplier;
    private float origAccelMultiplier;
    private float origMaxSpeedCap;
    private float origSpeedBleed;
    private float origLinearDamping;

    private bool engineCutApplied;
    private bool crashed;
    private float outOfFuelTimer;

    void Awake()
    {
        plane = GetComponent<PlaneController>();
        rb = GetComponent<Rigidbody>();

        landingJudge = FindFirstObjectByType<LandingJudge>();
        hud = FindFirstObjectByType<HUDController>();

        fuel = Mathf.Clamp(fuel, 0f, maxFuel);
    }

    void OnEnable()
    {
        CacheOriginals();

        if (fuelModeEnabled && enablePreObjective)
            StartPreObjective();
        else
            preComplete = true;
    }

    void OnDisable()
    {
        RestoreOriginals();
    }

    void CacheOriginals()
    {
        if (!plane || !rb) return;

        origThrustMultiplier = plane.thrustMultiplier;
        origAccelMultiplier = plane.accelMultiplier;
        origMaxSpeedCap = plane.maxSpeedCap;
        origSpeedBleed = plane.speedBleed;
        origLinearDamping = rb.linearDamping;

        engineCutApplied = false;
        crashed = false;
        outOfFuelTimer = 0f;

        // IMPORTANT: don't force unpause here if another script paused it.
        // leave pauseFuelDrain as-is on enable unless you want a clean reset:
        // pauseFuelDrain = false;

        smoothedAGL = 0f;
        preTimer = 0f;
        preComplete = false;
        holdScoreAccumulator = 0f;
    }

    void RestoreOriginals()
    {
        if (!plane || !rb) return;

        plane.thrustMultiplier = origThrustMultiplier;
        plane.accelMultiplier = origAccelMultiplier;
        plane.maxSpeedCap = origMaxSpeedCap;
        plane.speedBleed = origSpeedBleed;

        rb.linearDamping = origLinearDamping;

        engineCutApplied = false;
        crashed = false;
        outOfFuelTimer = 0f;

        pauseFuelDrain = false;

        preTimer = 0f;
        preComplete = false;
        holdScoreAccumulator = 0f;
    }

    // ---------------- PRE-OBJECTIVE ----------------

    public void StartPreObjective()
    {
        if (!enablePreObjective)
        {
            preComplete = true;
            return;
        }

        preTimer = 0f;
        preComplete = false;
        holdScoreAccumulator = 0f;

        RandomizePreObjectiveTargets();
    }

    private void RandomizePreObjectiveTargets()
    {
        preSafeAltChosen = Random.Range(preAltRange.x, preAltRange.y);
        preTargetSpeedChosenKmh = Random.Range(preTargetSpeedRangeKmh.x, preTargetSpeedRangeKmh.y);
        preSpeedBandChosenKmh = Random.Range(preSpeedBandRangeKmh.x, preSpeedBandRangeKmh.y);
        preHoldChosen = Random.Range(preHoldSecondsRange.x, preHoldSecondsRange.y);

        preSafeAltChosen = Mathf.Round(preSafeAltChosen / 10f) * 10f;
        preTargetSpeedChosenKmh = Mathf.Round(preTargetSpeedChosenKmh / 5f) * 5f;
        preSpeedBandChosenKmh = Mathf.Round(preSpeedBandChosenKmh / 5f) * 5f;
        preHoldChosen = Mathf.Round(preHoldChosen * 10f) / 10f;

        preSpeedBandChosenKmh = Mathf.Max(5f, preSpeedBandChosenKmh);
        preHoldChosen = Mathf.Max(1.5f, preHoldChosen);
        preSafeAltChosen = Mathf.Max(20f, preSafeAltChosen);
    }

    private void RunPreObjective(float dt)
    {
        if (!enablePreObjective || preComplete) return;

        if (IsPreObjectiveSatisfiedNow())
            preTimer += dt;
        else
            preTimer = 0f;

        if (awardPointsWhileHolding && hud != null && pointsPerSecondWhileHolding > 0 && IsPreObjectiveSatisfiedNow())
        {
            holdScoreAccumulator += pointsPerSecondWhileHolding * dt;
            int give = Mathf.FloorToInt(holdScoreAccumulator);
            if (give > 0)
            {
                hud.AddScore(give);
                holdScoreAccumulator -= give;
            }
        }

        if (preTimer >= preHoldChosen)
            preComplete = true;
    }

    public bool IsPreObjectiveSatisfiedNow()
    {
        if (!enablePreObjective || plane == null || rb == null) return true;

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
        if (rb == null) return 0f;
        return rb.linearVelocity.magnitude * 3.6f;
    }

    // ---------------- MAIN LOOP ----------------

    void FixedUpdate()
    {
        if (!fuelModeEnabled || !plane || !rb) return;
        if (crashed) return;

        float dt = Time.fixedDeltaTime;

        // Pre-objective runs (fuel still drains)
        RunPreObjective(dt);

        // Pause drain only for grace / end screen
        if (pauseFuelDrain) return;

        if (!IsOutOfFuel)
        {
            float speed = rb.linearVelocity.magnitude;
            float climb = Mathf.Max(0f, rb.linearVelocity.y);
            float turn = rb.angularVelocity.magnitude;

            float drain = baseDrain + speed * speedDrain + climb * climbDrain + turn * turnDrain;
            fuel = Mathf.Max(0f, fuel - drain * dt);

            if (fuel <= 0.001f)
            {
                fuel = 0f;
                ApplyEngineCut();
            }
        }
        else
        {
            if (!engineCutApplied) ApplyEngineCut();

            outOfFuelTimer += dt;
            if (outOfFuelTimer >= outOfFuelTimeout)
                FailRun("Out of fuel (timeout)");
        }
    }

    void ApplyEngineCut()
    {
        engineCutApplied = true;
        outOfFuelTimer = 0f;

        plane.thrustMultiplier = 0f;
        plane.accelMultiplier = 0f;

        plane.speedBleed = origSpeedBleed + extraSpeedBleedWhenEmpty;

        if (forceThrottleZero)
            plane.throttle01 = 0f;

        rb.linearDamping = Mathf.Max(origLinearDamping, engineOffLinearDamping);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!fuelModeEnabled || crashed) return;
        if (!IsOutOfFuel) return;
        if (c == null || c.collider == null) return;
        if (c.collider.isTrigger) return;

        if (c.relativeVelocity.magnitude < crashMinImpactSpeed) return;

        bool hitRunway = (!string.IsNullOrEmpty(runwayTag) &&
                          (c.collider.CompareTag(runwayTag) || c.transform.root.CompareTag(runwayTag)));

        FailRun(hitRunway ? "Out of fuel crash on runway" : $"Out of fuel crash into {c.collider.name}");
    }

    void FailRun(string reason)
    {
        crashed = true;

        if (landingJudge != null)
        {
            landingJudge.FailMissionFromCrash(reason);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}