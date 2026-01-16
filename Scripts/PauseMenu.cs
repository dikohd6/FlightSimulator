using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    private PlaneHUD hud;
    bool m_EscIsPressed;
    public GameObject panel;
    void Start()
    {
        hud= GetComponent<PlaneHUD>();
        m_EscIsPressed=false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_EscIsPressed)
            {
                panel.SetActive(false);
                m_EscIsPressed = false;
                Time.timeScale = 1.0f;
                hud.SetGamePaused(m_EscIsPressed);
            }
            else
            {
                Time.timeScale =0f;
                panel.SetActive(true);
                m_EscIsPressed = true;
                hud.SetGamePaused(m_EscIsPressed);

            }
        }




    }
}
