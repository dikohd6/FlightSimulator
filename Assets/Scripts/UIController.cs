using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement mainMenuOverlay;
    private Button startBtn;
    private Button optionsBtn;
    private Button exitBtn;

    void Awake()
    {
        var root = uiDocument.rootVisualElement;

        mainMenuOverlay = root.Q<VisualElement>("MainMenu");
        startBtn = root.Q<Button>("startBtn");
        optionsBtn = root.Q<Button>("optionsBtn");
        exitBtn = root.Q<Button>("exitBtn");

        startBtn.clicked += OnStartClicked;
        optionsBtn.clicked += OnOptionsClicked;
        exitBtn.clicked += OnExitClicked;
    }

    void OnStartClicked()
    {
        mainMenuOverlay.style.display = DisplayStyle.None;


    }

    void OnOptionsClicked()
    {
        Debug.Log("options button clicked!");


    }

    void OnExitClicked()
    {
        Debug.Log("exit button clicked!");
        Application.Quit();
    }
}
