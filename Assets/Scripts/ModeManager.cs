using UnityEngine;

public class ModeManager : MonoBehaviour
{
    public PlaneController plane;
    public EmergencyLandingMode emergency;   // optional, if you already made it

    void Start()
    {
        // Start in Standard mode
        SetStandardMode();
    }

    void Update()
    {
        // Temporary keyboard switching for testing
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetStandardMode();
            Debug.Log("Mode: Standard");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetEmergencyMode();
            Debug.Log("Mode: Emergency");
        }
    }

    void SetStandardMode()
    {
        // Reset flight behavior
        plane.maxSpeedCap = 1f;
        plane.accelMultiplier = 1f;
        plane.speedBleed = 0f;
        plane.thrustMultiplier = 1f;

        // Turn off emergency system if active
        if (emergency != null)
            emergency.enabled = false;
    }

    void SetEmergencyMode()
    {
        // Apply emergency flight behavior
        plane.maxSpeedCap = 0.55f;      // lower top speed
        plane.accelMultiplier = 0.4f;   // weak engine
        plane.speedBleed = 1.0f;        // speed slowly drops
        plane.thrustMultiplier = 1f;    // keep 1 (velocity already scaled by speed)

        // Activate emergency timer / UI if you have it
        if (emergency != null)
        {
            emergency.enabled = true;
            emergency.ActivateEmergency();
        }
    }
}
