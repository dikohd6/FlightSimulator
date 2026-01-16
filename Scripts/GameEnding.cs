using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class GameEnding : MonoBehaviour
{
    public RingManager ringManager;
    public PlaneHUD hud;
    private bool gameFinished = false;
    public GameObject gameEndingPanel;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI scoreText;
    private void Start()
    {
        timerText = gameEndingPanel.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.name == "TimeText");
        
        scoreText = gameEndingPanel.GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(t => t.name == "ScoreText");
       
    }
    void Update()
    {
        if (gameFinished)
        {
            EndLevel();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (ringManager.GetRingsCollected())
        {
            gameFinished = true;
        }
    }
    void EndLevel()
    {
        Time.timeScale = 0f;

        timerText.GetComponent<LocalizedTextTMP>().UpdateText();
        scoreText.GetComponent<LocalizedTextTMP>().UpdateText();
        timerText.text+=" "+hud.GetTime();

        scoreText.text += " " + hud.GetScore();
        gameEndingPanel.SetActive(true);

    }

}
