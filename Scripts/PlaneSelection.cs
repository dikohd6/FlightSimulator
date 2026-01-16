using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlaneSelection : MonoBehaviour
{
    public PlaneManager planeManager;
    public int speed;
    private int currentIndex = 0;
    private TextMeshProUGUI maxSpeed;
    private TextMeshProUGUI acceleration;
    private TextMeshProUGUI deceleration;
    private TextMeshProUGUI rotationSpeed;
    private TextMeshProUGUI autoAlign;
    private TextMeshProUGUI planeName;
    private void Awake()
    {
        maxSpeed = transform.Find("MaxSpeedText").GetComponent<TextMeshProUGUI>();
        acceleration = transform.Find("AccelerationText").GetComponent<TextMeshProUGUI>();
        deceleration = transform.Find("DecelerationText").GetComponent<TextMeshProUGUI>();
        rotationSpeed = transform.Find("RotationText").GetComponent<TextMeshProUGUI>();
        autoAlign = transform.Find("AutoLevelText").GetComponent<TextMeshProUGUI>();
        planeName = transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        planeManager.planes[currentIndex].plane.SetActive(true);
    }
    private void Start()
    {
       
        ShowPlaneStats();
    }

    private void Update()
    {
        ShowPlaneStats();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (currentIndex - 1 < 0)
            {

                planeManager.planes[currentIndex].plane.SetActive(false);
                currentIndex = 4;
                ShowPlaneStats();

            }
            else
            {
                planeManager.planes[currentIndex].plane.SetActive(false);
                currentIndex--;
                ShowPlaneStats();

            }

        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {

            if (currentIndex + 1 > 4)
            {
                planeManager.planes[currentIndex].plane.SetActive(false);
                currentIndex = 0;
                ShowPlaneStats();

            }
            else
            {
                planeManager.planes[currentIndex].plane.SetActive(false);
                currentIndex++;
                ShowPlaneStats();

            }
        }
        planeManager.planes[currentIndex].plane.SetActive(true);
        
        planeManager.planes[currentIndex].plane.transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
    public int currentPlane()
    {
        return currentIndex;
    }
    void ShowPlaneStats()
    {
        transform.Find("MaxSpeedText").GetComponent<LocalizedTextTMP>().UpdateText();
        transform.Find("AccelerationText").GetComponent<LocalizedTextTMP>().UpdateText();
        transform.Find("DecelerationText").GetComponent<LocalizedTextTMP>().UpdateText();
        transform.Find("RotationText").GetComponent<LocalizedTextTMP>().UpdateText();
        transform.Find("AutoLevelText").GetComponent<LocalizedTextTMP>().UpdateText();
        maxSpeed.text+=" "+planeManager.planes[currentIndex].speed;
        acceleration.text += " " + planeManager.planes[currentIndex].acceleration;
        deceleration.text += " " + planeManager.planes[currentIndex].deceleration;
        rotationSpeed.text += " " + planeManager.planes[currentIndex].rotation;
        autoAlign.text += " " + planeManager.planes[currentIndex].levelSpeed;
        planeName.text = planeManager.planes[currentIndex].plane.name;
    }
}
