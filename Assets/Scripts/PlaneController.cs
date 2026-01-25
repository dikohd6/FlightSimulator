using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Flight (Forces)")]
    public float maxThrust = 120f;          // Forward force (tune 80–250)
    public float maxSpeed = 55f;            // Hard cap (m/s-ish)
    public float stallSpeed = 12f;          // Below this, lift fades
    public float liftPower = 0.35f;         // Lift strength (tune 0.2–0.8)
    public float dragPower = 0.02f;         // Base drag (tune 0.01–0.06)

    [Header("Throttle")]
    public float throttleChangeRate = 0.8f; // how fast throttle changes per second
    [Range(0f, 1f)] public float throttle01 = 0f;

    [Header("Rotation (Torques)")]
    public float pitchTorque = 35f;         // Keyboard-friendly
    public float rollTorque = 45f;          // Keyboard-friendly
    public float yawTorque = 25f;           // Keyboard-friendly
    public float controlMinSpeed = 6f;      // Controls weak below this speed
    public float angularDamp = 3f;          // Helps prevent spinning

    [Header("Stability")]
    public float rollLevelStrength = 12f;   // Auto-level wings (safe)
    public float rollLevelMaxTorque = 20f;  // Clamp auto-level torque

    [Header("Mode Scaling (set by ModeManager)")]
    public float thrustMultiplier = 1f;     // Emergency: 0.4–0.8
    public float maxSpeedCap = 1f;          // Emergency: ~0.55
    public float accelMultiplier = 1f;      // Emergency: ~0.4 (reduces thrust response)
    public float speedBleed = 0f;           // Emergency: 0.5–2.0 extra drag

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
        rb = GetComponent<Rigidbody>();

        rb.useGravity = true;
        rb.isKinematic = false;

        // Physics stability
        rb.linearDamping = 0f;          // we'll do our own drag
        rb.angularDamping = angularDamp;
        rb.maxAngularVelocity = 3.5f;   // prevents insane spin
    }

    void FixedUpdate()
    {
        if (rb == null || input == null) return;

        var flight = input.Flight;
        if (flight.Pitch == null || flight.Roll == null || flight.Yaw == null ||
            flight.ThrottleUp == null || flight.ThrottleDown == null)
            return;

        // ===== INPUT =====
        float pitchInput = Mathf.Clamp(flight.Pitch.ReadValue<float>(), -1f, 1f); // W/S should be -1..1
        float rollInput = Mathf.Clamp(flight.Roll.ReadValue<float>(), -1f, 1f); // likely A/D
        float yawInput = Mathf.Clamp(flight.Yaw.ReadValue<float>(), -1f, 1f); // Q/E

        bool throttleUp = flight.ThrottleUp.IsPressed();
        bool throttleDown = flight.ThrottleDown.IsPressed();

        // ===== THROTTLE =====
        float tDelta = throttleChangeRate * accelMultiplier * Time.fixedDeltaTime;

        if (throttleUp) throttle01 += tDelta;
        if (throttleDown) throttle01 -= tDelta;
        throttle01 = Mathf.Clamp01(throttle01);

        // ===== SPEED & AUTHORITY =====
        Vector3 vel = rb.linearVelocity;
        float speed = vel.magnitude;

        // Controls ramp up with speed (more realistic + prevents ground spinning)
        float authority = Mathf.InverseLerp(0f, controlMinSpeed, speed);
        authority = Mathf.Clamp01(authority);

        // ===== THRUST =====
        float thrust = maxThrust * throttle01 * thrustMultiplier * accelMultiplier;
        rb.AddForce(transform.forward * thrust, ForceMode.Force);

        // ===== LIFT =====
        // Lift rises with speed^2, but fades near stall
        float stallFactor = Mathf.InverseLerp(stallSpeed * 0.6f, stallSpeed, speed); // 0..1
        stallFactor = Mathf.Clamp01(stallFactor);

        // main lift amount
        float lift = liftPower * speed * speed * stallFactor;
        rb.AddForce(transform.up * lift, ForceMode.Force);

        // ===== DRAG =====
        // Basic drag proportional to speed^2 + extra bleed for emergency pressure
        if (speed > 0.01f)
        {
            Vector3 dragDir = -vel.normalized;
            float drag = dragPower * speed * speed;

            // Extra "bleed" makes emergency feel like losing performance
            drag += speedBleed * speed;

            rb.AddForce(dragDir * drag, ForceMode.Force);
        }

        // ===== HARD SPEED CAP =====
        float cap = maxSpeed * Mathf.Max(0.05f, maxSpeedCap);
        if (speed > cap)
            rb.linearVelocity = vel.normalized * cap;

        // ===== ROTATION (SAFE TORQUES) =====
        // Use local axes: X=pitch, Y=yaw, Z=roll
        Vector3 torque = new Vector3(
            -pitchInput * pitchTorque,
            yawInput * yawTorque,
            -rollInput * rollTorque
        ) * authority;

        rb.AddRelativeTorque(torque, ForceMode.Force);

        // ===== ROLL AUTO-LEVEL (gentle) =====
        // Only when player isn't actively rolling
        if (Mathf.Abs(rollInput) < 0.01f)
        {
            float rollAngle = GetSignedRollAngle(); // degrees
            float levelTorque = Mathf.Clamp(-rollAngle * rollLevelStrength * 0.01f, -rollLevelMaxTorque, rollLevelMaxTorque);
            rb.AddRelativeTorque(new Vector3(0f, 0f, levelTorque) * authority, ForceMode.Force);
        }
    }

    // Signed roll angle in degrees (-180..180)
    float GetSignedRollAngle()
    {
        float z = transform.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return z;
    }

    public float GetCurrentSpeed()
    {
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
