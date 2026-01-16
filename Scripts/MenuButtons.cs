using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UIElements;
public class MenuButtons : MonoBehaviour
{
    public GameObject planePanel;
    public GameObject mainMenuPanel;
    public GameObject leaderboardPanel;
    public PlaneSelection planeSelection;
    public GameObject settingsPanel;
    public AudioSource music;
    private void Awake()
    {
        if (PlayerPrefs.HasKey("musicVolume"))
        {
            AudioListener.volume = PlayerPrefs.GetFloat("musicVolume");
        }
        
    }
    public void PlayButton()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        leaderboardPanel.SetActive(false);
        planePanel.SetActive(true);
    }
    public void LeaderboardButton()
    {
        leaderboardPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        planePanel.SetActive(false);
        settingsPanel.SetActive(false);
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void BackToMenuButton()
    {
        mainMenuPanel.SetActive(true);
        leaderboardPanel.SetActive(false);
        planePanel.SetActive(false);    
        settingsPanel.SetActive(false);
    }
    public void StartButton()
    {
        PlayerPrefs.SetInt("Plane", planeSelection.currentPlane());
        PlayerPrefs.Save();
        DontDestroyOnLoad(music);
        SceneManager.LoadScene(1);
    }
    public void SettingsButton()
    {
        settingsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
        planePanel.SetActive(false);
        leaderboardPanel.SetActive(false) ;
    }

}
