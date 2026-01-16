using UnityEngine;

public class LeaderboardEntry
{
    public string name;
    public string timeRemaining;
    public int score;

    public LeaderboardEntry(string name, string timeRemaining, int score)
    {
        this.name = name;
        this.timeRemaining = timeRemaining;
        this.score = score;
    }
}