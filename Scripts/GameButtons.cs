using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameButtons : MonoBehaviour
{
    public PlaneHUD hud;
    public PlaneController plane;
    public GameObject settingsPanel;
    private GameObject music; 

    private void Awake()
    {
        AudioListener.volume = PlayerPrefs.GetFloat("musicVolume");
        music = GameObject.FindWithTag("Music");

    }
    public void BackToMenu()
    {
        Destroy(music);
        if (hud.GetGamePaused())
        {
            SceneManager.LoadScene(0);
            return;
        }
        AddEntry();
        SceneManager.LoadScene(0);
    }
    public void RestartGame()
    {
        AddEntry();
        SceneManager.LoadScene(1);
    }
    public void SettingsButton()
    {
        settingsPanel.SetActive(true);
    }
    public void BackButton()
    {
        settingsPanel.SetActive(false);
    }
    private void AddEntry()
    {
        if (hud.GetMinutes() < 1)
        {
            hud.AddScore(400);

        }else if (hud.GetMinutes() < 2)
        {
            hud.AddScore(200);
        }else if (hud.GetMinutes() < 3)
        {
            hud.AddScore(100);
        }
            LeaderboardManager.Instance.AddLeaderboardEntry(plane.name, hud.GetTime(), hud.GetScore());
    }
    
}
