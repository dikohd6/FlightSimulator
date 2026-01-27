using UnityEngine;

public class LandingJudge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneController plane;
    [SerializeField] private EmergencyLandingMode emergency;

    [Header("Touchdown Limits")]
    [SerializeField] private float maxDescentRate = -5.5f;
    [SerializeField] private float minTouchdownSpeed = 14f;
    [SerializeField] private float maxTouchdownSpeed = 30f;
    [SerializeField] private float maxBankDeg = 12f;
    [SerializeField] private float maxYawDeg = 10f;

    [Header("Runway")]
    [SerializeField] private Transform runwayTransform;

    private bool insideRunway;
    private bool landed;

    void Awake()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();
    }

    // Called by RunwayLandingZone trigger
    public void NotifyEnteredRunway(PlaneController p)
    {
        if (p == plane) insideRunway = true;
    }

    public void NotifyExitedRunway(PlaneController p)
    {
        if (p == plane) insideRunway = false;
    }

    // THIS is the real touchdown detection
    void OnCollisionEnter(Collision col)
    {
        if (landed || plane == null) return;

        // Only care about collisions with runway
        if (!col.collider.CompareTag("Runway"))
            return;

        // Only evaluate if this is the plane
        if (!col.collider || !plane) return;

        EvaluateTouchdown();
    }

    private void EvaluateTouchdown()
    {
        landed = true;

        if (!insideRunway)
        {
            Fail("Missed the runway (touchdown outside runway zone).");
            return;
        }

        float verticalSpeed = plane.GetVerticalSpeed();
        float speed = plane.GetCurrentSpeed();

        // Tighten limits if gear failure
        float descentLimit = maxDescentRate;
        float bankLimit = maxBankDeg;
        float yawLimit = maxYawDeg;

        if (emergency != null &&
            emergency.enabled &&
            emergency.GetActiveFailure() == EmergencyLandingMode.FailureType.GearFailure)
        {
            descentLimit = -4.0f;
            bankLimit = 8f;
            yawLimit = 7f;
        }

        if (verticalSpeed < descentLimit)
        {
            Fail($"Hard landing (descent rate {verticalSpeed:0.0} m/s).");
            return;
        }

        if (speed < minTouchdownSpeed)
        {
            Fail($"Stall landing (too slow: {speed:0.0}).");
            return;
        }

        if (speed > maxTouchdownSpeed)
        {
            Fail($"Too fast on touchdown ({speed:0.0}).");
            return;
        }

        float bank = SignedBankDeg(plane.transform);
        if (Mathf.Abs(bank) > bankLimit)
        {
            Fail($"Wings not level (bank {bank:0}°).");
            return;
        }

        float yawMisalign = YawMisalignmentDeg();
        if (Mathf.Abs(yawMisalign) > yawLimit)
        {
            Fail($"Not aligned with runway (yaw slip {yawMisalign:0}°).");
            return;
        }

        LandSuccess();
    }

    private void LandSuccess()
    {
        Debug.Log("✅ Emergency landing SUCCESS!");
        // TODO: freeze plane, show UI, score, etc.
    }

    private void Fail(string reason)
    {
        Debug.Log("❌ Emergency landing FAILED: " + reason);
    }

    private float SignedBankDeg(Transform t)
    {
        float z = t.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return z;
    }

    private float YawMisalignmentDeg()
    {
        if (runwayTransform == null)
            return 0f;

        Vector3 planeFwd = plane.transform.forward;
        planeFwd.y = 0f;
        planeFwd.Normalize();

        Vector3 runwayFwd = runwayTransform.forward;
        runwayFwd.y = 0f;
        runwayFwd.Normalize();

        return Vector3.SignedAngle(runwayFwd, planeFwd, Vector3.up);
    }
}
