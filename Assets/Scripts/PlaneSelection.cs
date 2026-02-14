using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaneSelection : MonoBehaviour
{
    public PlaneManager planeManager;
    [SerializeField] private ModeManager modeManager; // NEW
    [SerializeField] private UIDocument gameMenuDocument;

    private VisualElement gameMenuOverlay;
    Button leftBtn;
    Button rightBtn;
    public int speed;
    private int currentIndex = 0;

    private void Awake()
    {
        // Find ModeManager if not assigned
        if (modeManager == null)
            modeManager = FindFirstObjectByType<ModeManager>();

        var gameRoot = gameMenuDocument.rootVisualElement;
        gameMenuOverlay = gameRoot.Q<VisualElement>("missionSelect");

        planeManager.planes[currentIndex].plane.SetActive(true);

        leftBtn = gameMenuOverlay.Q<Button>("leftBtn");
        rightBtn = gameMenuOverlay.Q<Button>("rightBtn");

        leftBtn.clicked += OnLeftButtonClicked;
        rightBtn.clicked += OnRightButtonClicked;

        // NEW: Update ModeManager with initial selection
        UpdateModeManager();
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
        // Your plane stats display code here
    }

    // NEW: Update ModeManager whenever plane selection changes
    private void UpdateModeManager()
    {
        if (modeManager != null)
            modeManager.SetSelectedPlane(currentIndex);
    }

    void OnLeftButtonClicked()
    {
        planeManager.planes[currentIndex].plane.SetActive(false);

        if (currentIndex - 1 < 0)
        {
            currentIndex = planeManager.planes.Length - 1; // Fixed: wrap to last plane
        }
        else
        {
            currentIndex--;
        }

        ShowPlaneStats();
        UpdateModeManager(); // NEW
    }

    void OnRightButtonClicked()
    {
        planeManager.planes[currentIndex].plane.SetActive(false);

        if (currentIndex + 1 >= planeManager.planes.Length) // Fixed: use array length
        {
            currentIndex = 0;
        }
        else
        {
            currentIndex++;
        }

        ShowPlaneStats();
        UpdateModeManager(); // NEW
    }
}