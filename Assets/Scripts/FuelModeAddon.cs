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

    public bool IsOutOfFuel => fuelModeEnabled && fuel <= 0.001f;
    public float Fuel01 => maxFuel <= 0f ? 0f : Mathf.Clamp01(fuel / maxFuel);

    private PlaneController plane;
    private Rigidbody rb;
    private LandingJudge landingJudge;

    // Cache original values so other modes are unaffected
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

        // Try to find LandingJudge (PlaneController also does this, but we keep our own reference)
        landingJudge = FindFirstObjectByType<LandingJudge>();

        fuel = Mathf.Clamp(fuel, 0f, maxFuel);
    }

    void OnEnable()
    {
        CacheOriginals();
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
    }

    void FixedUpdate()
    {
        if (!fuelModeEnabled || !plane || !rb) return;
        if (crashed) return;

        float dt = Time.fixedDeltaTime;

        // ----- Drain fuel while engine is on -----
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
            // keep engine cut effects applied
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

        // Cut thrust by zeroing multipliers (PlaneController will still run, but thrust becomes 0)
        plane.thrustMultiplier = 0f;
        plane.accelMultiplier = 0f;

        // Make it slow down & stall faster (uses your existing drag model)
        plane.speedBleed = origSpeedBleed + extraSpeedBleedWhenEmpty;

        // Optional: enforce no throttle input effect
        if (forceThrottleZero)
            plane.throttle01 = 0f;

        // Optional: add some linear damping so it loses speed naturally
        rb.linearDamping = Mathf.Max(origLinearDamping, engineOffLinearDamping);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!fuelModeEnabled || crashed) return;
        if (!IsOutOfFuel) return; // only crash after fuel runs out
        if (c == null || c.collider == null) return;
        if (c.collider.isTrigger) return;

        // Only count meaningful impacts
        if (c.relativeVelocity.magnitude < crashMinImpactSpeed) return;

        // If this is runway, decide if we still want to crash
        // (Your PlaneController normally ignores runway collisions.)
        bool hitRunway = (!string.IsNullOrEmpty(runwayTag) &&
                          (c.collider.CompareTag(runwayTag) || c.transform.root.CompareTag(runwayTag)));

        // In fuel-empty mode, we DO want runway impact to count as crash (stall crash),
        // so we don't early-return for runway.
        // If you *don't* want runway to crash, uncomment the next line:
        // if (hitRunway) return;

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

        // fallback if LandingJudge isn't in scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}