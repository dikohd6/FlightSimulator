using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float maxSpeed = 40f;
    public float acceleration = 5f;
    public float deceleration = 6f;
    public float minSpeed = 0f;

    [Header("Rotation")]
    public float rotationSpeed = 5f;
    public float autoLevelSpeed = 2f;
    public float maxPitchAngle = 45f;

    private Rigidbody rb;
    private float currentSpeed;

    // Input System
    private PlaneInputActions input;

    void Awake()
    {
        input = new PlaneInputActions();
    }

    void OnEnable()
    {
        input.Enable();
    }

    void OnDisable()
    {
        input.Disable();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 2f;

        currentSpeed = 0f;
        rb.linearVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // INPUT
        float pitchInput = input.Flight.Pitch.ReadValue<float>();
        float rollInput = input.Flight.Roll.ReadValue<float>();
        float yawInput = input.Flight.Yaw.ReadValue<float>();

        bool throttleUp = input.Flight.ThrottleUp.IsPressed();
        bool throttleDown = input.Flight.ThrottleDown.IsPressed();

        // THROTTLE
        if (throttleUp)
            currentSpeed += acceleration * Time.fixedDeltaTime;

        if (throttleDown)
            currentSpeed -= deceleration * Time.fixedDeltaTime;

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // FORWARD MOVEMENT
        rb.linearVelocity = transform.forward * currentSpeed;

        // ROTATION
        rb.AddTorque(transform.right * -pitchInput * rotationSpeed, ForceMode.Force);
        rb.AddTorque(transform.forward * -rollInput * rotationSpeed, ForceMode.Force);
        rb.AddTorque(transform.up * yawInput * rotationSpeed, ForceMode.Force);

        // CLAMP PITCH
        Vector3 euler = transform.eulerAngles;
        float pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
        transform.rotation = Quaternion.Euler(pitch, euler.y, euler.z);

        // AUTO LEVEL
        if (Mathf.Abs(pitchInput) < 0.01f)
        {
            Quaternion levelRotation = Quaternion.Euler(
                0f,
                transform.eulerAngles.y,
                transform.eulerAngles.z
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                levelRotation,
                Time.fixedDeltaTime * autoLevelSpeed
            );
        }
    }

    public float GetCurrentSpeed()
    {
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (!collision.gameObject.CompareTag("PlaneLane"))
        //{
           // Debug.Log("Crashed into: " + collision.gameObject.name);
            //RestartGame();
        //}
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
