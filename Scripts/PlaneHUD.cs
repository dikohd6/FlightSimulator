using UnityEngine;
using TMPro;

public class PlaneHUD : MonoBehaviour
{
    [Header("References")]
    public PlaneController plane;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject LandingText;
    public GameObject controlPanel;
    private float controlsTimer = 0;
    private int minutes;
    private int seconds;
    private bool gamePaused = false;

    [Header("Gameplay")]
    public float timer = 0f;
    public int score = 0;

    void Update()
    {
        if (controlsTimer < 11f)
        {
            Time.timeScale = 0f;
            controlsTimer += Time.unscaledDeltaTime;
            Debug.Log(controlsTimer);
        }
        else if (controlsTimer >= 11f && controlPanel.activeSelf)
        {
            controlPanel.SetActive(false);
            Time.timeScale = 1f;
        }



        if (plane != null && !gamePaused)
        {
            float speedKmh = plane.GetCurrentSpeed() * 3.6f;
            speedText.GetComponent<LocalizedTextTMP>().UpdateText();
            speedText.text += $" {speedKmh:0} km/h";
        }

        timer += Time.deltaTime;
        minutes = Mathf.FloorToInt(timer / 60f);
        seconds = Mathf.FloorToInt(timer % 60f);
        timerText.GetComponent<LocalizedTextTMP>().UpdateText();
        scoreText.GetComponent<LocalizedTextTMP>().UpdateText();
        timerText.text += $" {minutes:00}:{seconds:00}";

        scoreText.text += $" {score}";
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
    public string GetTime()
    {
        return $"{minutes: 00}:{seconds: 00}";
        ;
    }
    
    public int GetScore()
    {
        return score;
    }
    public int GetMinutes()
    {
        return minutes;
    }
    public bool GetGamePaused()
    {
        return gamePaused;
    }
    public void SetGamePaused(bool isGamePaused)
    {
        gamePaused=isGamePaused;
    }

}
