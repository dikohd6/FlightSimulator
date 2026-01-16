using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("Flight Settings")]
    public float maxSpeed ;          // Max throttle
    public float acceleration;       // How fast throttle increases
    public float deceleration;       // How fast throttle decreases
    public float minSpeed = 0f;            // Can fully stop
    public float rotationSpeed;      // Rotation strength
    public float autoLevelSpeed;      // How fast it levels when no input
    public float maxPitchAngle = 45f;  
    // Limit pitch up/down

    private Rigidbody rb;
    private float currentSpeed;
    private int currentPlane;

    void Start()
    {
        SpawnPlane();

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 1f;
        rb.angularDrag = 2f;

        currentSpeed = 0f;
        rb.velocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        // --- Input ---
        float pitchInput = Input.GetAxis("Vertical");   // W/S
        float rollInput = Input.GetAxis("Horizontal"); // A/D
        float yawInput = 0f;
        if (Input.GetKey(KeyCode.Q)) yawInput = -1f;
        if (Input.GetKey(KeyCode.E)) yawInput = 1f;

        // --- Throttle Control ---
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed += acceleration * Time.deltaTime; // Speed up
        if (Input.GetKey(KeyCode.LeftControl))
            currentSpeed -= deceleration * Time.deltaTime; // Slow down

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // --- Forward movement ---
        if (currentSpeed > 0.1f)
            rb.AddForce(transform.forward * currentSpeed, ForceMode.Force);
        else
            rb.velocity = Vector3.zero; // Stop when throttle is at 0

        // --- Rotation ---
        rb.AddTorque(transform.right * -pitchInput * rotationSpeed);   // Pitch
        rb.AddTorque(transform.forward * -rollInput * rotationSpeed);  // Roll
        rb.AddTorque(transform.up * yawInput * rotationSpeed);         // Yaw

        // --- Clamp Pitch ---
        Vector3 currentRotation = transform.eulerAngles;
        float pitch = currentRotation.x;
        if (pitch > 180) pitch -= 360;
        pitch = Mathf.Clamp(pitch, -maxPitchAngle, maxPitchAngle);
        transform.rotation = Quaternion.Euler(pitch, currentRotation.y, currentRotation.z);

        // --- Auto-Level ---
        if (Mathf.Approximately(pitchInput, 0f))
        {
            Quaternion level = Quaternion.Euler(0, transform.eulerAngles.y, currentRotation.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, level, Time.deltaTime * autoLevelSpeed);
        }
    }
    public float GetCurrentSpeed()
    {
        return rb.velocity.magnitude;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.tag.Equals("PlaneLane"))
        {
            Debug.Log(collision.gameObject.name);
            Debug.Log("Crashed into a building! Restarting...");
            RestartGame();
        }
       

    }

    void RestartGame()
    {
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    void SpawnPlane()
    {
        currentPlane = PlayerPrefs.GetInt("Plane");
        if (currentPlane == 0)
        {
            GameObject plane = GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "ComPlaneV1")?.gameObject;
            plane.SetActive(true);
            acceleration = 4;
            deceleration = 5;
            maxSpeed = 35;
            rotationSpeed = 5;
            autoLevelSpeed = 1.5f;

        }
        else if (currentPlane == 1)
        {
            GameObject plane = GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "ComPlaneV2")?.gameObject;
            plane.SetActive(true);
            acceleration = 7;
            deceleration = 10;
            maxSpeed = 40;
            rotationSpeed = 6;
            autoLevelSpeed = 2f;
        }
        else if (currentPlane == 2)
        {
            GameObject plane = GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "ComPlaneV3")?.gameObject;
            plane.SetActive(true);
            acceleration = 8;
            deceleration = 8;
            maxSpeed = 30;
            rotationSpeed = 7;
            autoLevelSpeed = 2.3f;
        }
        else if (currentPlane == 3)
        {
            GameObject plane = GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "EaglePlane")?.gameObject;
            plane.SetActive(true);
            acceleration = 2;
            deceleration = 4;
            maxSpeed = 25;
            rotationSpeed = 1;
            autoLevelSpeed = 3f;
        }
        else if (currentPlane == 4)
        {
            GameObject plane = GetComponentsInChildren<Transform>(true)
                           .FirstOrDefault(t => t.name == "LightPlane")?.gameObject;
            plane.SetActive(true);
            acceleration = 1;
            deceleration = 3;
            maxSpeed = 20;
            rotationSpeed = 3.5f;
            autoLevelSpeed = 3f;
        }
        {

        }
    }
}
