using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardDisplay : MonoBehaviour
{
    [SerializeField] private GameObject infoPrefab; // The template prefab placed in the UI
    [SerializeField] private Transform contentParent; // The parent with Vertical Layout Group

    void Start()
    {
        var entries = LeaderboardManager.Instance.leaderboardEntries;
        entries = entries.OrderByDescending(entry => entry.score).ToList();

        if (entries.Count > 0)
        {
            // 1️⃣ Populate the first prefab with the first entry
            SetInfoText(infoPrefab, entries[0].name, entries[0].timeRemaining, entries[0].score);
            // 2️⃣ Duplicate for the rest
            for (int i = 1; i < entries.Count; i++)
            {
                DuplicateInfoWithNewText(entries[i].name, entries[i].timeRemaining, entries[i].score);
            }
        }
        else
        {
            // No entries -> hide the first prefab
            infoPrefab.SetActive(false);
        }
    }

    private void SetInfoText(GameObject infoObject, string playerName, string timeRemaining, int score)
    {
        TextMeshProUGUI playerNameText = infoObject.transform.Find("PlayerNameButton")?.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI timeText = infoObject.transform.Find("TimeRemainingButton")?.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI scoreText = infoObject.transform.Find("ScoreButton")?.GetComponentInChildren<TextMeshProUGUI>();

        if (playerNameText != null) playerNameText.text = playerName;
        if (timeText != null) timeText.text = timeRemaining;
        if (scoreText != null) scoreText.text = score.ToString();
    }

    public void DuplicateInfoWithNewText(string playerName, string timeRemaining, int score)
    {
        if (infoPrefab != null && contentParent != null)
        {
            // Instantiate a copy of Info under Content
            GameObject newInfo = Instantiate(infoPrefab, contentParent);

            // Optionally rename the duplicate to avoid confusion
            newInfo.name = $"Info_{playerName}";

            // Fill in the text
            SetInfoText(newInfo, playerName, timeRemaining, score);

            // Force layout update
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent.GetComponent<RectTransform>());
        }
        else
        {
            Debug.LogError("Info prefab or Content parent not assigned!");
        }
    }
}
