using System;
using UnityEngine;

public class LandingJudge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlaneController plane;
    [SerializeField] private EmergencyLandingMode emergency;
    [SerializeField] private LandingCinematicController ending;
    [SerializeField] private HUDController hud;
    [SerializeField] private ModeManager modeManager;

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

    [Header("Point Weights")]
    [SerializeField] private int pointsYaw = 30;
    [SerializeField] private int pointsBank = 20;
    [SerializeField] private int pointsDescent = 30;
    [SerializeField] private int pointsSpeed = 20;

    [Header("Coin Reward Tuning")]
    [Tooltip("Scales combined score (landing + HUD score) into coins.")]
    [SerializeField] private float coinMultiplier = 0.35f;

    [Tooltip("Random reward factor min/max.")]
    [SerializeField] private Vector2 coinRandomFactorRange = new Vector2(0.85f, 1.20f);

    [Tooltip("Extra random bonus on successful landing.")]
    [SerializeField] private Vector2Int successBonusRange = new Vector2Int(20, 60);

    [Tooltip("Fail reward is reduced by this factor.")]
    [SerializeField] private float failRewardFactor = 0.4f;

    [Tooltip("Minimum reward on success/fail.")]
    [SerializeField] private int minCoinsOnSuccess = 25;
    [SerializeField] private int minCoinsOnFail = 5;

    private enum Result { None, Success, Fail }
    private Result result = Result.None;

    private Rigidbody planeRb;
    private bool insideRunway;
    private float lastInsideTime;

    private void Awake()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();
        if (ending == null) ending = FindFirstObjectByType<LandingCinematicController>();
        if (hud == null) hud = FindFirstObjectByType<HUDController>();
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        if (plane != null) planeRb = plane.GetComponent<Rigidbody>();
        if (planeRb == null) planeRb = GetComponentInParent<Rigidbody>();

        if (ending == null)
            Debug.LogError("LandingJudge: LandingCinematicController not found/assigned!");
    }

    // Called by runway trigger script
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

    private void OnCollisionEnter(Collision col)
    {
        if (result != Result.None) return;
        if (plane == null || planeRb == null || col == null) return;

        // Only runway collisions count as touchdown
        if (!col.gameObject.CompareTag(runwayTag) && !col.transform.root.CompareTag(runwayTag))
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

        ScoreBreakdown breakdown = ComputeBreakdown(verticalSpeed, speed, bank, yaw, inZoneNow);
        string grade = GradeFromScore(breakdown.total);

        string failReason = null;
        if (!inZoneNow) failReason = "Missed the runway (touchdown outside runway zone).";
        else if (verticalSpeed < descentLimit) failReason = $"Hard landing (descent {verticalSpeed:0.0} m/s).";
        else if (speed < minTouchdownSpeed) failReason = $"Stall landing (too slow {speed:0.0}).";
        else if (speed > maxTouchdownSpeed) failReason = $"Too fast on touchdown ({speed:0.0}).";
        else if (bank > bankLimit) failReason = $"Wings not level (bank {bank:0}°).";
        else if (yaw > yawLimit) failReason = $"Not aligned with runway (yaw slip {yaw:0}°).";

        if (failReason != null)
        {
            result = Result.Fail;
            TriggerEnding(false, failReason, breakdown, grade);
            return;
        }

        result = Result.Success;
        TriggerEnding(true, null, breakdown, grade);
    }

    private void TriggerEnding(bool success, string reason, ScoreBreakdown b, string grade)
    {
        // Stop HUD timer at end
        if (hud != null) hud.FreezeTimer();

        // Get mode string from ModeManager
        string mode = GetModeDisplayName();

        // Time + HUD score
        float timeSec = hud != null ? hud.GetElapsedTimeSeconds() : 0f;
        int gameScore = hud != null ? hud.GetScore() : 0;

        // Calculate coin reward
        int coinsEarned = CalculateCoinsEarned(b.total, gameScore, success);

        // Add to existing coins + save (via PlaneManager)
        int totalCoinsAfter = 0;
        var planeManager = PlaneManager.Instance != null ? PlaneManager.Instance : FindFirstObjectByType<PlaneManager>();
        if (planeManager != null)
        {
            // Assumes PlaneManager.AddCoins(amount) saves to PlayerPrefs in your existing economy system
            planeManager.AddCoins(coinsEarned);
            totalCoinsAfter = planeManager.Coins;
        }
        else
        {
            Debug.LogWarning("LandingJudge: PlaneManager not found. Coins could not be awarded/saved.");
        }

        // Save leaderboard
        LeaderboardStore.AddEntry(new LeaderboardStore.Entry
        {
            mode = mode,
            timeSeconds = timeSec,
            score = b.total,
            grade = grade,
            success = success,
            dateUtc = DateTime.UtcNow.ToString("o")
        });

        // UI / cinematic data
        var data = new LandingScoreData
        {
            success = success,
            failReason = reason ?? "",

            yawPts = b.yawPts,
            bankPts = b.bankPts,
            descentPts = b.descentPts,
            speedPts = b.speedPts,

            maxYawPts = pointsYaw,
            maxBankPts = pointsBank,
            maxDescentPts = pointsDescent,
            maxSpeedPts = pointsSpeed,

            gameScore = gameScore,
            coinsEarned = coinsEarned,
            totalCoinsAfter = totalCoinsAfter,

            total = b.total,
            grade = grade
        };
        // Quest progress + rewards (auto adds coins via PlaneManager)
        int questCoinsAwarded = 0;

        if (modeManager != null)
        {
            questCoinsAwarded = QuestSystem.ReportLandingResult(
                success,
                b.total,
                grade,
                modeManager.CurrentMode
            );
        }
        else
        {
            // fallback if mode manager missing
            questCoinsAwarded = QuestSystem.ReportLandingResult(
                success,
                b.total,
                grade,
                ModeManager.ModeType.Standard
            );
        }

        if (questCoinsAwarded > 0)
        {
            Debug.Log($"✅ Quest coins awarded this run: +{questCoinsAwarded}");
        }

        if (ending != null)
            ending.PlayEnding(data);

        Debug.Log(
            $"{(success ? "✅" : "❌")} Landing End | LandingScore={b.total} | GameScore={gameScore} | CoinsEarned=+{coinsEarned} | TotalCoins={totalCoinsAfter} | Grade={grade}"
        );
    }

    private int CalculateCoinsEarned(int landingScore, int gameScore, bool success)
    {
        landingScore = Mathf.Max(0, landingScore);
        gameScore = Mathf.Max(0, gameScore);

        int combined = landingScore + gameScore;

        float minRand = Mathf.Min(coinRandomFactorRange.x, coinRandomFactorRange.y);
        float maxRand = Mathf.Max(coinRandomFactorRange.x, coinRandomFactorRange.y);
        float randomFactor = UnityEngine.Random.Range(minRand, maxRand);

        int coins = Mathf.RoundToInt(combined * coinMultiplier * randomFactor);

        if (success)
        {
            int bonusMin = Mathf.Min(successBonusRange.x, successBonusRange.y);
            int bonusMaxInclusive = Mathf.Max(successBonusRange.x, successBonusRange.y);
            coins += UnityEngine.Random.Range(bonusMin, bonusMaxInclusive + 1);
            coins = Mathf.Max(minCoinsOnSuccess, coins);
        }
        else
        {
            coins = Mathf.RoundToInt(coins * failRewardFactor);
            coins = Mathf.Max(minCoinsOnFail, coins);
        }

        return Mathf.Max(0, coins);
    }

    private string GetModeDisplayName()
    {
        if (modeManager == null)
            return "Unknown Mode";

        switch (modeManager.CurrentMode)
        {
            case ModeManager.ModeType.Standard:
                return "Standard Landing";

            case ModeManager.ModeType.Emergency:
                if (emergency != null && emergency.enabled)
                    return $"Emergency - {emergency.GetActiveFailure()}";
                return "Emergency Landing";

            case ModeManager.ModeType.Fuel:
                return "Fuel Challenge";

            default:
                return "Unknown Mode";
        }
    }

    // ---------- Crash fail entry point (call this from PlaneController) ----------
    public void FailMissionFromCrash(string reason)
    {
        if (result != Result.None) return;
        result = Result.Fail;

        ScoreBreakdown b = new ScoreBreakdown
        {
            yawPts = 0,
            bankPts = 0,
            descentPts = 0,
            speedPts = 0,
            zonePts = 0,
            total = 0
        };

        TriggerEnding(false, $"Crash: {reason}", b, "F");
    }

    // ---------- Scoring ----------
    private struct ScoreBreakdown
    {
        public int yawPts, bankPts, descentPts, speedPts, zonePts, total;
    }

    private ScoreBreakdown ComputeBreakdown(float verticalSpeed, float speed, float bankDeg, float yawDeg, bool inZone)
    {
        ScoreBreakdown b = new ScoreBreakdown();

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

        b.zonePts = inZone ? 0 : -30;
        b.total = Mathf.Clamp(b.yawPts + b.bankPts + b.descentPts + b.speedPts + b.zonePts, 0, 100);

        return b;
    }

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

    private float SignedBankDeg(Transform t)
    {
        float z = t.localEulerAngles.z;
        if (z > 180f) z -= 360f;
        return z;
    }

    private float YawMisalignmentDeg()
    {
        if (runwayTransform == null || plane == null) return 0f;

        Vector3 planeFwd = plane.transform.forward;
        planeFwd.y = 0f;
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