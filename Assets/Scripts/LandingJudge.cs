using UnityEngine;

public class LandingJudge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneController plane;
    [SerializeField] private EmergencyLandingMode emergency;

    [Header("Touchdown Limits (pass/fail)")]
    [SerializeField] private float maxDescentRate = -5.5f;
    [SerializeField] private float minTouchdownSpeed = 14f;
    [SerializeField] private float maxTouchdownSpeed = 30f;
    [SerializeField] private float maxBankDeg = 12f;
    [SerializeField] private float maxYawDeg = 10f;

    [Header("Scoring Ranges (not pass/fail)")]
    [Tooltip("Vertical speed (m/s). 0 is perfect, more negative is worse.")]
    [SerializeField] private float descentPerfect = -1.2f;
    [SerializeField] private float descentOkay = -3.0f;
    [SerializeField] private float descentBad = -6.5f;

    [Tooltip("Yaw misalignment degrees. 0 is perfect.")]
    [SerializeField] private float yawPerfect = 0.5f;
    [SerializeField] private float yawOkay = 4f;
    [SerializeField] private float yawBad = 12f;

    [Tooltip("Bank degrees. 0 is perfect.")]
    [SerializeField] private float bankPerfect = 1.0f;
    [SerializeField] private float bankOkay = 6f;
    [SerializeField] private float bankBad = 15f;

    [Header("Speed Scoring")]
    [SerializeField] private bool autoSpeedTarget = true;
    [SerializeField] private float speedTarget = 22f;
    [SerializeField] private float speedOkayWindow = 5f;
    [SerializeField] private float speedBadWindow = 10f;

    [Header("Runway")]
    [SerializeField] private Transform runwayTransform;
    [Tooltip("Your runway collider is long on X, so keep TRUE.")]
    [SerializeField] private bool runwayUsesRightAxis = true;

    [Header("Runway Zone Grace")]
    [SerializeField] private float runwayGraceSeconds = 0.5f;

    [Header("Tags")]
    [SerializeField] private string runwayTag = "Runway";

    // Weights (points)
    [Header("Point Weights")]
    [SerializeField] private int pointsYaw = 30;
    [SerializeField] private int pointsBank = 20;
    [SerializeField] private int pointsDescent = 30;
    [SerializeField] private int pointsSpeed = 20;

    private enum Result { None, Success, Fail }
    private Result result = Result.None;

    private Rigidbody planeRb;
    private bool insideRunway;
    private float lastInsideTime;

    void Awake()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();
        if (plane != null) planeRb = plane.GetComponent<Rigidbody>();
        if (planeRb == null) planeRb = GetComponentInParent<Rigidbody>();
    }

    public void NotifyEnteredRunway(PlaneController p)
    {
        if (p != plane) return;
        insideRunway = true;
        lastInsideTime = Time.time;
    }

    public void NotifyExitedRunway(PlaneController p)
    {
        if (p != plane) return;
        insideRunway = false;
    }

    void OnCollisionEnter(Collision col)
    {
        if (result != Result.None) return;
        if (plane == null || planeRb == null || col == null) return;

        if (!col.gameObject.CompareTag(runwayTag) && !col.gameObject.transform.root.CompareTag(runwayTag))
            return;

        EvaluateTouchdown();
    }

    private void EvaluateTouchdown()
    {
        if (result != Result.None) return;

        bool inZoneNow = insideRunway || (Time.time - lastInsideTime) <= runwayGraceSeconds;

        float verticalSpeed = planeRb.linearVelocity.y;
        float speed = planeRb.linearVelocity.magnitude;
        float bank = Mathf.Abs(SignedBankDeg(plane.transform));
        float yaw = Mathf.Abs(YawMisalignmentDeg());

        // Gear failure tightens PASS/FAIL only
        float descentLimit = maxDescentRate;
        float bankLimit = maxBankDeg;
        float yawLimit = maxYawDeg;

        if (emergency != null && emergency.enabled &&
            emergency.GetActiveFailure() == EmergencyLandingMode.FailureType.GearFailure)
        {
            descentLimit = -4.0f;
            bankLimit = 8f;
            yawLimit = 7f;
        }

        // ---- Score (always) ----
        var breakdown = ComputeBreakdown(verticalSpeed, speed, bank, yaw, inZoneNow);
        string grade = GradeFromScore(breakdown.total);

        // ---- Pass/Fail ----
        if (!inZoneNow) { Fail($"Missed the runway (touchdown outside runway zone)."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }
        if (verticalSpeed < descentLimit) { Fail($"Hard landing (descent {verticalSpeed:0.0} m/s)."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }
        if (speed < minTouchdownSpeed) { Fail($"Stall landing (too slow {speed:0.0})."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }
        if (speed > maxTouchdownSpeed) { Fail($"Too fast on touchdown ({speed:0.0})."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }
        if (bank > bankLimit) { Fail($"Wings not level (bank {bank:0}°)."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }
        if (yaw > yawLimit) { Fail($"Not aligned with runway (yaw slip {yaw:0}°)."); PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow); return; }

        result = Result.Success;
        Debug.Log($"✅ Emergency landing SUCCESS! Score: {breakdown.total}/100 ({grade})");
        PrintBreakdown(verticalSpeed, speed, bank, yaw, breakdown, grade, inZoneNow);
    }

    // ---------- Breakdown struct ----------
    private struct ScoreBreakdown
    {
        public int yawPts;
        public int bankPts;
        public int descentPts;
        public int speedPts;
        public int zonePts;
        public int total;
    }

    private ScoreBreakdown ComputeBreakdown(float verticalSpeed, float speed, float bankDeg, float yawDeg, bool inZone)
    {
        ScoreBreakdown b = new ScoreBreakdown();

        // target speed
        float target = autoSpeedTarget ? (minTouchdownSpeed + maxTouchdownSpeed) * 0.5f : speedTarget;

        float yaw01 = Score01(yawDeg, yawPerfect, yawOkay, yawBad);
        float bank01 = Score01(bankDeg, bankPerfect, bankOkay, bankBad);

        float descentAbs = Mathf.Abs(verticalSpeed);
        float perfectAbs = Mathf.Abs(descentPerfect);
        float okayAbs = Mathf.Abs(descentOkay);
        float badAbs = Mathf.Abs(descentBad);
        float descent01 = Score01(descentAbs, perfectAbs, okayAbs, badAbs);

        float speedDelta = Mathf.Abs(speed - target);
        float speed01 = Score01(speedDelta, 0f, speedOkayWindow, speedBadWindow);

        b.yawPts = Mathf.RoundToInt(yaw01 * pointsYaw);
        b.bankPts = Mathf.RoundToInt(bank01 * pointsBank);
        b.descentPts = Mathf.RoundToInt(descent01 * pointsDescent);
        b.speedPts = Mathf.RoundToInt(speed01 * pointsSpeed);

        // Runway zone points (binary + grace already handled by inZone)
        b.zonePts = inZone ? 0 : -30; // penalty if outside zone

        b.total = Mathf.Clamp(b.yawPts + b.bankPts + b.descentPts + b.speedPts + b.zonePts, 0, 100);
        return b;
    }

    // 1 = perfect, 0 = bad
    private float Score01(float value, float perfect, float okay, float bad)
    {
        if (value <= perfect) return 1f;
        if (value >= bad) return 0f;

        if (value <= okay)
        {
            float t = Mathf.InverseLerp(perfect, okay, value);
            return Mathf.Lerp(1f, 0.6f, t);
        }
        else
        {
            float t = Mathf.InverseLerp(okay, bad, value);
            return Mathf.Lerp(0.6f, 0f, t);
        }
    }

    private string GradeFromScore(int score)
    {
        if (score >= 90) return "S";
        if (score >= 80) return "A";
        if (score >= 70) return "B";
        if (score >= 60) return "C";
        if (score >= 50) return "D";
        return "F";
    }

    private void PrintBreakdown(float vSpeed, float speed, float bank, float yaw, ScoreBreakdown b, string grade, bool inZone)
    {
        Debug.Log(
            $"--- Landing Score Breakdown ---\n" +
            $"Runway Zone: {(inZone ? "IN" : "OUT")} (penalty {b.zonePts})\n" +
            $"Yaw Alignment: {yaw:0.0}° -> {b.yawPts}/{pointsYaw}\n" +
            $"Bank Level: {bank:0.0}° -> {b.bankPts}/{pointsBank}\n" +
            $"Descent Rate: {vSpeed:0.0} m/s -> {b.descentPts}/{pointsDescent}\n" +
            $"Touchdown Speed: {speed:0.0} m/s -> {b.speedPts}/{pointsSpeed}\n" +
            $"TOTAL: {b.total}/100 ({grade})"
        );
    }

    private void Fail(string reason)
    {
        if (result != Result.None) return;
        result = Result.Fail;
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
        if (runwayTransform == null || plane == null) return 0f;

        Vector3 planeFwd = plane.transform.forward; planeFwd.y = 0f;
        if (planeFwd.sqrMagnitude < 0.0001f) return 0f;
        planeFwd.Normalize();

        Vector3 runwayDir = runwayUsesRightAxis ? runwayTransform.right : runwayTransform.forward;
        runwayDir.y = 0f;
        if (runwayDir.sqrMagnitude < 0.0001f) return 0f;
        runwayDir.Normalize();

        return Vector3.SignedAngle(runwayDir, planeFwd, Vector3.up);
    }

    public void ResetJudge()
    {
        insideRunway = false;
        lastInsideTime = 0f;
        result = Result.None;
    }
}
