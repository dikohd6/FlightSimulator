using UnityEngine;
using TMPro;

public class EmergencyLandingMode : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public TextMeshProUGUI warningText; // optional but recommended

    [Header("Emergency Settings")]
    public bool emergencyActive = false;
    public float emergencyDuration = 90f;     // seconds until full failure
    [Range(0f, 1f)] public float thrustMultiplier = 0.5f; // 50% thrust

    [Header("Thrust (connect to your controller)")]
    public float normalMaxThrust = 3000f; // set to your normal thrust
    public float currentThrottle01;       // 0..1 set by your flight controller

    private float timer;
    private bool engineDead;

    void Start()
    {
        timer = emergencyDuration;

        // If you want emergency mode always ON for this scene:
        ActivateEmergency();
    }

    void Update()
    {
        // For testing: press K to activate emergency
        if (Input.GetKeyDown(KeyCode.K))
            ActivateEmergency();

        if (!emergencyActive) return;

        if (!engineDead)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0f;
                engineDead = true;
            }
        }

        UpdateUI();
    }

    void FixedUpdate()
    {
        // Only apply emergency thrust logic when active
        if (!emergencyActive) return;

        float maxThrustNow = engineDead ? 0f : normalMaxThrust * thrustMultiplier;
        float thrustForce = maxThrustNow * Mathf.Clamp01(currentThrottle01);

        // Apply thrust in forward direction
        rb.AddForce(transform.forward * thrustForce, ForceMode.Force);
    }

    public void ActivateEmergency()
    {
        emergencyActive = true;
        engineDead = false;
        timer = emergencyDuration;

        // Optional: immediate warning update
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (warningText == null) return;

        if (!emergencyActive)
        {
            warningText.gameObject.SetActive(false);
            return;
        }

        warningText.gameObject.SetActive(true);

        int seconds = Mathf.CeilToInt(timer);
        int min = seconds / 60;
        int sec = seconds % 60;

        if (engineDead)
            warningText.text = $"<color=#FF3333>ENGINE FAILED</color> — GLIDE & LAND NOW!";
        else
            warningText.text = $"<color=#FF3333>ENGINE FAILURE</color> — {min:00}:{sec:00} to shutdown";
    }
}
