using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Thrust / Speed")]
    public float maxThrustAccel = 12f;
    public float maxSpeed = 55f;

    [Header("Lift / Drag")]
    public float stallSpeed = 12f;
    public float liftCoeff = 0.06f;          // base lift
    public float inducedDragCoeff = 0.015f;  // drag from lift
    public float baseDragCoeff = 0.0015f;    // parasitic drag

    [Header("Ground Handling")]
    public float groundCheckDistance = 1.6f;
    public float rotateSpeed = 18f;
    [Range(0f, 0.2f)] public float taxiLiftFactor = 0.03f;
    public float groundStickAccel = 6f;
    public LayerMask groundMask = ~0;

    [Header("Crash / Mission Fail")]
    [SerializeField] private LandingJudge landingJudge;          // assign in inspector or auto-find
    [SerializeField] private string runwayTag = "Runway";

    [Tooltip("Anything with this tag will NEVER cause mission fail on collision.")]
    [SerializeField] private string noCrashTag = "NoCrash";

    [SerializeField] private float crashArmSeconds = 0.5f;        // avoids instant fail on spawn
    private float spawnTime;

    [Header("Throttle")]
    public float throttleChangeRate = 0.8f;
    [Range(0f, 1f)] public float throttle01 = 0f;

    [Header("Rotation (Torques)")]
    public float pitchTorque = 35f;
    public float rollTorque = 45f;
    public float yawTorque = 25f;
    public float controlMinSpeed = 6f;
    public float angularDamp = 3f;

    [Header("Stability")]
    public float rollLevelStrength = 12f;
    public float rollLevelMaxTorque = 20f;

    [Header("Mode Scaling")]
    public float thrustMultiplier = 1f;
    public float maxSpeedCap = 1f;
    public float accelMultiplier = 1f;
    public float speedBleed = 0f;

    private Rigidbody rb;
    private PlaneInputActions input;

    void Awake()
    {
        input = new PlaneInputActions();
    }

    void OnEnable()
    {
        input?.Enable();
    }

    void OnDisable()
    {
        input?.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        rb.angularDamping = angularDamp;
        rb.maxAngularVelocity = 3.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0f;
        rb.sleepThreshold = 0f;
        rb.WakeUp();

        // IMPORTANT: this was missing before
        spawnTime = Time.time;

        if (landingJudge == null)
            landingJudge = FindFirstObjectByType<LandingJudge>();
    }

    void FixedUpdate()
    {
        if (rb == null || input == null) return;

        var flight = input.Flight;

        float pitchInput = Mathf.Clamp(flight.Pitch.ReadValue<float>(), -1f, 1f);
        float rollInput = Mathf.Clamp(flight.Roll.ReadValue<float>(), -1f, 1f);
        float yawInput = Mathf.Clamp(flight.Yaw.ReadValue<float>(), -1f, 1f);

        bool throttleUp = flight.ThrottleUp.IsPressed();
        bool throttleDown = flight.ThrottleDown.IsPressed();

        // ===== THROTTLE =====
        float tDelta = throttleChangeRate * accelMultiplier * Time.fixedDeltaTime;
        if (throttleUp) throttle01 += tDelta;
        if (throttleDown) throttle01 -= tDelta;
        throttle01 = Mathf.Clamp01(throttle01);

        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(vel, transform.forward));

        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);

        // ===== THRUST =====
        float thrustAccel = maxThrustAccel * throttle01 * thrustMultiplier * accelMultiplier;
        rb.AddForce(transform.forward * thrustAccel, ForceMode.Acceleration);

        // ===== ANGLE OF ATTACK =====
        float aoa = 0f;
        if (speed > 0.1f)
            aoa = Vector3.SignedAngle(transform.forward, vel.normalized, transform.right);

        float aoaLiftFactor = Mathf.Clamp01((aoa + 10f) / 20f);

        // ===== LIFT =====
        float stallFactor = Mathf.InverseLerp(stallSpeed * 0.6f, stallSpeed, forwardSpeed);
        float lift = liftCoeff * forwardSpeed * forwardSpeed * stallFactor * aoaLiftFactor;

        if (grounded && forwardSpeed < rotateSpeed)
            lift *= taxiLiftFactor;

        rb.AddForce(transform.up * lift, ForceMode.Acceleration);

        // ===== DRAG =====
        float inducedDrag = lift * inducedDragCoeff;
        float parasiticDrag = baseDragCoeff * speed * speed;
        float totalDrag = inducedDrag + parasiticDrag + (speedBleed * forwardSpeed * 0.02f);

        if (speed > 0.01f)
            rb.AddForce(-vel.normalized * totalDrag, ForceMode.Acceleration);

        // ===== HARD SPEED CAP =====
        float cap = maxSpeed * Mathf.Max(0.05f, maxSpeedCap);
        if (speed > cap)
            rb.linearVelocity = vel.normalized * cap;

        // ===== CONTROL AUTHORITY =====
        float authority = Mathf.InverseLerp(0f, controlMinSpeed, speed);

        // ===== TORQUES =====
        Vector3 torque = new Vector3(
            -pitchInput * pitchTorque,
            yawInput * yawTorque,
            -rollInput * rollTorque
        ) * authority;

        rb.AddRelativeTorque(torque, ForceMode.Force);

        // ===== AUTO-LEVEL =====
        if (Mathf.Abs(rollInput) < 0.01f)
        {
            float rollAngle = GetSignedRollAngle();
            float levelTorque = Mathf.Clamp(-rollAngle * rollLevelStrength * 0.01f, -rollLevelMaxTorque, rollLevelMaxTorque);
            rb.AddRelativeTorque(new Vector3(0f, 0f, levelTorque) * authority, ForceMode.Force);
        }

        // ===== GROUND STICK =====
        float takeoffIntent = Mathf.Clamp01(-pitchInput);
        if (grounded && forwardSpeed < rotateSpeed && takeoffIntent < 0.2f)
            rb.AddForce(Vector3.down * groundStickAccel, ForceMode.Acceleration);

        rb.WakeUp();
    }

    float GetSignedRollAngle()
    {
        float z = transform.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return z;
    }

    void OnCollisionEnter(Collision c)
    {
        if (rb == null || c == null || c.collider == null) return;

        if (((1 << c.gameObject.layer) & groundMask) != 0)
            rb.angularVelocity *= 0.25f;

        // wait a moment after spawn
        if (Time.time - spawnTime < crashArmSeconds) return;

        // Ignore triggers
        if (c.collider.isTrigger) return;

        // Ignore runway itself (landing judge handles touchdown)
        if (c.collider.CompareTag(runwayTag) || c.transform.root.CompareTag(runwayTag))
            return;

        // ✅ NEW: Ignore anything tagged NoCrash (collider object OR its root)
        if (c.collider.CompareTag(noCrashTag) || c.transform.root.CompareTag(noCrashTag))
            return;

        // Anything else -> crash fail mission
        if (landingJudge != null)
        {
            landingJudge.FailMissionFromCrash($"Crashed into {c.collider.name}");
        }
        else
        {
            Debug.Log($"❌ Crash: {c.collider.name} (LandingJudge not found)");
        }
    }

    public float GetCurrentSpeed() => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float GetVerticalSpeed() => rb != null ? rb.linearVelocity.y : 0f;
    public float GetForwardSpeed() => rb != null ? Vector3.Dot(rb.linearVelocity, transform.forward) : 0f;
    public Rigidbody GetRigidbody() => rb;

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
