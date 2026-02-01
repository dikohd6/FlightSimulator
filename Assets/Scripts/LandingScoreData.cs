using System;

[Serializable]
public class LandingScoreData
{
    public bool success;
    public string failReason;

    public int yawPts;
    public int bankPts;
    public int descentPts;
    public int speedPts;

    public int maxYawPts;
    public int maxBankPts;
    public int maxDescentPts;
    public int maxSpeedPts;

    public int total;
    public string grade;
}
