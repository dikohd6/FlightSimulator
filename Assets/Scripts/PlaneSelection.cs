using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaneSelection : MonoBehaviour
{
    public PlaneManager planeManager;
    [SerializeField] private UIDocument gameMenuDocument;
    private VisualElement gameMenuOverlay;
    Button leftBtn;
    Button rightBtn;
    public int speed;
    private int currentIndex = 0;
    private void Awake()
    {
        var gameRoot = gameMenuDocument.rootVisualElement;
        gameMenuOverlay = gameRoot.Q<VisualElement>("missionSelect");
        planeManager.planes[currentIndex].plane.SetActive(true);
        leftBtn = gameMenuOverlay.Q<Button>("leftBtn");
        rightBtn = gameMenuOverlay.Q<Button>("rightBtn");
        leftBtn.clicked += OnLeftButtonClicked;
        rightBtn.clicked += OnRightButtonClicked;
    }
    private void Start()
    {
       
        ShowPlaneStats();
    }

    private void Update()
    {
        ShowPlaneStats();

        
        planeManager.planes[currentIndex].plane.SetActive(true);
        
        planeManager.planes[currentIndex].plane.transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
    public int currentPlane()
    {
        return currentIndex;
    }
    void ShowPlaneStats()
    {
        
    }

    void OnLeftButtonClicked()
    {
        if (currentIndex - 1 < 0)
        {

            planeManager.planes[currentIndex].plane.SetActive(false);
            currentIndex = 1;
            ShowPlaneStats();

        }
        else
        {
            planeManager.planes[currentIndex].plane.SetActive(false);
            currentIndex--;
            ShowPlaneStats();

        }
    }
    void OnRightButtonClicked()
    {
        


        if (currentIndex + 1 > 1)
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
}
