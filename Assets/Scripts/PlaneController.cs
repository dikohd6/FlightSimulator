using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Thrust / Speed")]
    [Tooltip("Forward acceleration at full throttle (m/s^2). Tune 6–20.")]
    public float maxThrustAccel = 12f;

    [Tooltip("Hard speed cap (m/s-ish).")]
    public float maxSpeed = 55f;

    [Header("Lift / Drag (mass-independent)")]
    [Tooltip("Below this forward speed, lift fades out.")]
    public float stallSpeed = 12f;

    [Tooltip("Lift acceleration factor. Tune 0.02–0.12.")]
    public float liftAccelFactor = 0.06f;

    [Tooltip("Drag acceleration factor. Tune 0.0005–0.004.")]
    public float dragAccelFactor = 0.0015f;

    [Header("Ground Handling")]
    public float groundCheckDistance = 1.6f;
    public float rotateSpeed = 18f;       // must be > stallSpeed
    [Range(0f, 0.2f)] public float taxiLiftFactor = 0.03f; // keep low
    public float groundStickAccel = 6f;   // pushes plane down when grounded to stop bouncing
    public LayerMask groundMask = ~0;

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

    [Header("Mode Scaling (set by ModeManager)")]
    public float thrustMultiplier = 1f;
    public float maxSpeedCap = 1f;
    public float accelMultiplier = 1f;
    public float speedBleed = 0f; // extra drag accel-ish

    private Rigidbody rb;
    private PlaneInputActions input;

    void Awake()
    {
        input = new PlaneInputActions();
    }

    void OnEnable()
    {
        if (input == null) input = new PlaneInputActions();
        input.Enable();
    }

    void OnDisable()
    {
        input?.Disable();
    }

    void Start()
    {
        var cols = GetComponentsInChildren<Collider>(true);
        Debug.Log("Plane colliders found: " + cols.Length);
        foreach (var c in cols)
            Debug.Log($" - {c.name}  trigger={c.isTrigger}  enabled={c.enabled}");

        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.isKinematic = false;

        rb.angularDamping = angularDamp;
        rb.maxAngularVelocity = 3.5f;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Let our own drag do the work
        rb.linearDamping = 0f;
    }

    void FixedUpdate()
    {
        if (rb == null || input == null) return;

        var flight = input.Flight;
        if (flight.Pitch == null || flight.Roll == null || flight.Yaw == null ||
            flight.ThrottleUp == null || flight.ThrottleDown == null)
            return;

        // ===== INPUT =====
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

        // Forward speed for aero
        float forwardSpeed = Mathf.Max(0f, Vector3.Dot(vel, transform.forward));

        // Ground check
        bool grounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);

        // ===== THRUST (Acceleration) =====
        float thrustAccel = maxThrustAccel * throttle01 * thrustMultiplier * accelMultiplier;
        rb.AddForce(transform.forward * thrustAccel, ForceMode.Acceleration);

        // ===== LIFT (Acceleration) =====
        // stallFactor 0..1 based on forward speed
        float stallFactor = Mathf.InverseLerp(stallSpeed * 0.6f, stallSpeed, forwardSpeed);
        stallFactor = Mathf.Clamp01(stallFactor);

        // Lift accel grows with v^2
        float liftAccel = liftAccelFactor * forwardSpeed * forwardSpeed * stallFactor;

        // Taxi lockout: keep it planted until rotate speed
        if (grounded && forwardSpeed < rotateSpeed)
            liftAccel *= taxiLiftFactor;

        rb.AddForce(Vector3.up * liftAccel, ForceMode.Acceleration);

        // Ground stick: slight downforce when grounded to prevent bouncing/jitter
        if (grounded && forwardSpeed < rotateSpeed * 1.2f)
            rb.AddForce(Vector3.down * groundStickAccel, ForceMode.Acceleration);

        // ===== DRAG (Acceleration) =====
        if (speed > 0.01f)
        {
            Vector3 dragDir = -vel.normalized;

            float dragAccel = dragAccelFactor * forwardSpeed * forwardSpeed;

            // Emergency bleed adds extra drag-like accel
            dragAccel += speedBleed * forwardSpeed * 0.02f;

            rb.AddForce(dragDir * dragAccel, ForceMode.Acceleration);
        }

        // ===== HARD SPEED CAP =====
        float cap = maxSpeed * Mathf.Max(0.05f, maxSpeedCap);
        if (speed > cap)
            rb.linearVelocity = vel.normalized * cap;

        // ===== CONTROL AUTHORITY =====
        float authority = Mathf.InverseLerp(0f, controlMinSpeed, speed);
        authority = Mathf.Clamp01(authority);

        // ===== ROTATION =====
        Vector3 torque = new Vector3(
            -pitchInput * pitchTorque,
            yawInput * yawTorque,
            -rollInput * rollTorque
        ) * authority;

        rb.AddRelativeTorque(torque, ForceMode.Force);

        // ===== ROLL AUTO-LEVEL =====
        if (Mathf.Abs(rollInput) < 0.01f)
        {
            float rollAngle = GetSignedRollAngle();
            float levelTorque = Mathf.Clamp(-rollAngle * rollLevelStrength * 0.01f, -rollLevelMaxTorque, rollLevelMaxTorque);
            rb.AddRelativeTorque(new Vector3(0f, 0f, levelTorque) * authority, ForceMode.Force);
        }
    }

    float GetSignedRollAngle()
    {
        float z = transform.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return z;
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
