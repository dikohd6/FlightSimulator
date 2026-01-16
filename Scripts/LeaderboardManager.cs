using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance; // Singleton pattern
    public List<LeaderboardEntry> leaderboardEntries = new List<LeaderboardEntry>(); // Store scores
    public string playerName = "DK"; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public void AddLeaderboardEntry(string name, string timeRemaining, int score)
    {
        LeaderboardEntry entry = new LeaderboardEntry(name, timeRemaining, score);
        leaderboardEntries.Add(entry);
        Debug.Log($"Added entry: {name}, {timeRemaining}, {score}. Total entries: {leaderboardEntries.Count}");
    }
    
}
