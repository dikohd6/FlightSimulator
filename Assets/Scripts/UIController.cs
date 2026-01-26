using AirportPack;
using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private UIDocument gameMenuDocument;
    [SerializeField] private HangarGateControl hangarGate;


    private VisualElement mainMenuOverlay;
    private VisualElement gameMenuOverlay;
    private VisualElement gameMenuMissionsOverlay;
    private VisualElement optionsOverlay;
    private Button startBtn;
    private Button optionsBtn;
    private Button exitBtn;
    private Button menubtn;
    private Button GameMenubtn;

    private Image background;

    void Awake()
    {

        var mainRoot = mainMenuDocument.rootVisualElement;
        var gameRoot = gameMenuDocument.rootVisualElement;
        gameMenuOverlay = gameRoot.Q<VisualElement>("missionSelect");
        gameMenuMissionsOverlay = gameRoot.Q<VisualElement>("Missions");
        mainMenuOverlay = mainRoot.Q<VisualElement>("MainMenu");
        optionsOverlay = mainRoot.Q<VisualElement>("Options");
        optionsOverlay.style.display = DisplayStyle.None;
        gameMenuOverlay.style.display = DisplayStyle.None;
        gameMenuMissionsOverlay.style.display= DisplayStyle.None;
        background = mainRoot.Q<Image>("background");
        startBtn = mainRoot.Q<Button>("startBtn");
        optionsBtn = mainRoot.Q<Button>("optionsBtn");
        exitBtn = mainRoot.Q<Button>("exitBtn");
        menubtn = optionsOverlay.Q<Button>("mainMenuBtn");
        GameMenubtn = gameMenuOverlay.Q<Button>("mainMenuBtn");

        startBtn.clicked += OnStartClicked;
        optionsBtn.clicked += OnOptionsClicked;
        exitBtn.clicked += OnExitClicked;
        menubtn.clicked += OnMainMenuClicked;
        GameMenubtn.clicked+= OnMainMenuClicked;
    }

    void OnStartClicked()
    {
        mainMenuOverlay.style.display = DisplayStyle.None;
        background.style.display = DisplayStyle.None;
        gameMenuOverlay.style.display = DisplayStyle.Flex;
        gameMenuMissionsOverlay.style.display = DisplayStyle.Flex;

        hangarGate.OpenGates();
    }

    void OnOptionsClicked()
    {
        optionsOverlay.style.display = DisplayStyle.Flex;
        mainMenuOverlay.style.display = DisplayStyle.None;

    }

    void OnExitClicked()
    {
        Debug.Log("exit button clicked!");
        Application.Quit();
    }

    void OnMainMenuClicked()
    {
        mainMenuOverlay.style.display = DisplayStyle.Flex;
        optionsOverlay.style.display = DisplayStyle.None;
        gameMenuOverlay.style.display = DisplayStyle.None;
        gameMenuMissionsOverlay.style.display = DisplayStyle.None;

        background.style.display = DisplayStyle.Flex;
    }
}
