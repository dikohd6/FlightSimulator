using System;
using System.Collections.Generic;
using UnityEngine;

public static class QuestSystem
{
    public struct QuestView
    {
        public string id;
        public string title;
        public int progress;
        public int target;
        public int rewardCoins;
        public bool completed;
    }

    private enum QuestMetric
    {
        SuccessfulLandings,
        Score80Plus,
        EmergencySuccesses,
        TotalLandingScoreAccumulated
    }

    private struct QuestDef
    {
        public string id;
        public string title;
        public int target;
        public int rewardCoins;
        public QuestMetric metric;

        public QuestDef(string id, string title, int target, int rewardCoins, QuestMetric metric)
        {
            this.id = id;
            this.title = title;
            this.target = target;
            this.rewardCoins = rewardCoins;
            this.metric = metric;
        }
    }

    private static readonly QuestDef[] quests =
    {
        new QuestDef("success_landings",  "Complete 3 successful landings", 3,   600, QuestMetric.SuccessfulLandings),
        new QuestDef("score_80_plus",     "Get 80+ score in 1 landing",     1,   450, QuestMetric.Score80Plus),
        new QuestDef("emergency_success", "Complete 1 emergency landing",   1,   800, QuestMetric.EmergencySuccesses),
        new QuestDef("total_score",       "Accumulate 250 landing score",   250, 700, QuestMetric.TotalLandingScoreAccumulated),
    };

    public static event Action OnQuestsChanged;

    // Call this once per landing result (success/fail)
    public static int ReportLandingResult(bool success, int landingScore, string grade, ModeManager.ModeType mode)
    {
        int totalQuestCoinsAwarded = 0;
        bool anyProgressChanged = false;

        for (int i = 0; i < quests.Length; i++)
        {
            QuestDef q = quests[i];

            // Ignore quests already completed
            if (IsCompleted(q.id))
                continue;

            int add = GetProgressToAdd(q, success, landingScore, grade, mode);
            if (add <= 0) continue;

            bool changed;
            int awarded = AddProgressAndCompleteOnce(q, add, out changed);

            if (changed) anyProgressChanged = true;
            totalQuestCoinsAwarded += awarded;
        }

        if (anyProgressChanged)
            OnQuestsChanged?.Invoke();

        return totalQuestCoinsAwarded;
    }

    // All quests (including completed)
    public static QuestView[] GetQuestViews()
    {
        QuestView[] result = new QuestView[quests.Length];

        for (int i = 0; i < quests.Length; i++)
        {
            bool completed = IsCompleted(quests[i].id);
            int progress = GetProgress(quests[i].id);

            // Clamp display to target when completed
            if (completed) progress = quests[i].target;

            result[i] = new QuestView
            {
                id = quests[i].id,
                title = quests[i].title,
                progress = Mathf.Clamp(progress, 0, quests[i].target),
                target = quests[i].target,
                rewardCoins = quests[i].rewardCoins,
                completed = completed
            };
        }

        return result;
    }

    // Only active (not completed) quests - useful for your main menu
    public static List<QuestView> GetActiveQuestViews()
    {
        var list = new List<QuestView>();
        var all = GetQuestViews();

        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].completed)
                list.Add(all[i]);
        }

        return list;
    }

    private static int GetProgressToAdd(QuestDef q, bool success, int landingScore, string grade, ModeManager.ModeType mode)
    {
        switch (q.metric)
        {
            case QuestMetric.SuccessfulLandings:
                return success ? 1 : 0;

            case QuestMetric.Score80Plus:
                return (success && landingScore >= 80) ? 1 : 0;

            case QuestMetric.EmergencySuccesses:
                return (success && mode == ModeManager.ModeType.Emergency) ? 1 : 0;

            case QuestMetric.TotalLandingScoreAccumulated:
                // Count only successful landings
                return success ? Mathf.Max(0, landingScore) : 0;
        }

        return 0;
    }

    // One-time quest behavior: progress accumulates until target, then completes once and rewards once.
    private static int AddProgressAndCompleteOnce(QuestDef q, int amount, out bool progressChanged)
    {
        progressChanged = false;
        if (amount <= 0) return 0;
        if (IsCompleted(q.id)) return 0;

        int progress = GetProgress(q.id);
        int newProgress = Mathf.Max(0, progress + amount);

        progressChanged = (newProgress != progress);

        // Complete quest
        if (newProgress >= q.target)
        {
            SetProgress(q.id, q.target);      // keep it full for display/history
            SetCompleted(q.id, true);
            AwardCoins(q.rewardCoins);

            Debug.Log($"✅ Quest completed: {q.title} (+{q.rewardCoins} coins)");
            return q.rewardCoins;
        }

        // Not completed yet
        SetProgress(q.id, newProgress);
        return 0;
    }

    private static void AwardCoins(int amount)
    {
        if (amount <= 0) return;

        PlaneManager pm = PlaneManager.Instance != null
            ? PlaneManager.Instance
            : UnityEngine.Object.FindFirstObjectByType<PlaneManager>();

        if (pm == null)
        {
            Debug.LogWarning($"QuestSystem: Could not find PlaneManager to award {amount} coins.");
            return;
        }

        pm.AddCoins(amount);
        Debug.Log($"🟡 Quest reward awarded: +{amount} coins");
    }

    private static int GetProgress(string id)
    {
        return PlayerPrefs.GetInt(GetProgressKey(id), 0);
    }

    private static void SetProgress(string id, int value)
    {
        PlayerPrefs.SetInt(GetProgressKey(id), Mathf.Max(0, value));
        PlayerPrefs.Save();
    }

    private static bool IsCompleted(string id)
    {
        return PlayerPrefs.GetInt(GetCompletedKey(id), 0) == 1;
    }

    private static void SetCompleted(string id, bool completed)
    {
        PlayerPrefs.SetInt(GetCompletedKey(id), completed ? 1 : 0);
        PlayerPrefs.Save();
    }

    private static string GetProgressKey(string id) => $"quest_progress_{id}";
    private static string GetCompletedKey(string id) => $"quest_completed_{id}";

    // Optional helper for testing in editor (call from temporary button/debug code)
    public static void ResetAllQuestProgress()
    {
        for (int i = 0; i < quests.Length; i++)
        {
            PlayerPrefs.DeleteKey(GetProgressKey(quests[i].id));
            PlayerPrefs.DeleteKey(GetCompletedKey(quests[i].id));
        }

        PlayerPrefs.Save();
        OnQuestsChanged?.Invoke();
        Debug.Log("QuestSystem: All quest progress reset.");
    }
}