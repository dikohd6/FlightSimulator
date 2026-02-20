using AirportPack;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDocument;
    [SerializeField] private UIDocument gameMenuDocument;
    [SerializeField] private HangarGateControl hangarGate;
    [SerializeField] private ModeManager modeManager;

    private VisualElement mainMenuOverlay;
    private VisualElement gameMenuOverlay;
    private VisualElement gameMenuMissionsOverlay;
    private VisualElement optionsOverlay;

    private Button startBtn;
    private Button optionsBtn;
    private Button exitBtn;
    private Button menubtn;
    private Button GameMenubtn;
    private Button playBtn;

    private Image background;

    // ---------- Leaderboard ----------
    private VisualElement leaderboardRoot;
    private Button leaderboardBtn;
    private Button leaderboardMainMenuBtn;
    private Button leaderboardClearBtn;
    private ListView leaderboardList;

    // Keep a cached list so ListView has a stable reference
    private List<LeaderboardStore.Entry> cachedEntries = new();

    private void Awake()
    {
        var mainRoot = mainMenuDocument.rootVisualElement;
        var gameRoot = gameMenuDocument.rootVisualElement;

        // --- game menu overlays ---
        gameMenuOverlay = gameRoot.Q<VisualElement>("missionSelect");
        gameMenuMissionsOverlay = gameRoot.Q<VisualElement>("Missions");

        // --- main menu overlays ---
        mainMenuOverlay = mainRoot.Q<VisualElement>("MainMenu");
        optionsOverlay = mainRoot.Q<VisualElement>("Options");

        background = mainRoot.Q<Image>("background");

        startBtn = mainRoot.Q<Button>("startBtn");
        optionsBtn = mainRoot.Q<Button>("optionsBtn");
        leaderboardBtn = mainRoot.Q<Button>("leaderboardBtn");
        exitBtn = mainRoot.Q<Button>("exitBtn");

        menubtn = optionsOverlay.Q<Button>("mainMenuBtn");
        GameMenubtn = gameMenuOverlay.Q<Button>("mainMenuBtn");
        playBtn = gameMenuOverlay.Q<Button>("playBtn");

        // Start hidden
        optionsOverlay.style.display = DisplayStyle.None;
        gameMenuOverlay.style.display = DisplayStyle.None;
        gameMenuMissionsOverlay.style.display = DisplayStyle.None;

        // --- Leaderboard panel (your UXML) ---
        leaderboardRoot = mainRoot.Q<VisualElement>("LeaderboardRoot");
        if (leaderboardRoot != null)
        {
            leaderboardMainMenuBtn = leaderboardRoot.Q<Button>("mainMenuBtn");
            leaderboardClearBtn = leaderboardRoot.Q<Button>("clearBtn");

            // Prefer a named list
            leaderboardList = leaderboardRoot.Q<ListView>("leaderboardList");

            // Fallback: if you forgot to name it, grab the first ListView inside LeaderboardRoot
            if (leaderboardList == null)
                leaderboardList = leaderboardRoot.Q<ListView>();

            leaderboardRoot.style.display = DisplayStyle.None;
            SetupLeaderboardListView();
        }
        else
        {
            Debug.LogError("UIController: Could not find LeaderboardRoot in MainMenu UXML.");
        }

        // Hooks
        startBtn.clicked += OnStartClicked;
        optionsBtn.clicked += OnOptionsClicked;
        exitBtn.clicked += OnExitClicked;

        menubtn.clicked += OnMainMenuClicked;
        GameMenubtn.clicked += OnMainMenuClicked;

        playBtn.clicked += OnPlayClicked;

        leaderboardMainMenuBtn.clicked += OnMainMenuClicked;

        if (leaderboardBtn != null) leaderboardBtn.clicked += OnLeaderboardClicked;
        if (leaderboardClearBtn != null) leaderboardClearBtn.clicked += ClearLeaderboard;
    }

    // ---------------- Main Menu ----------------
    private void OnStartClicked()
    {
        mainMenuOverlay.style.display = DisplayStyle.None;
        background.style.display = DisplayStyle.None;

        gameMenuOverlay.style.display = DisplayStyle.Flex;
        gameMenuMissionsOverlay.style.display = DisplayStyle.Flex;

        hangarGate.OpenGates();
    }

    private void OnOptionsClicked()
    {
        optionsOverlay.style.display = DisplayStyle.Flex;
        mainMenuOverlay.style.display = DisplayStyle.None;

        if (leaderboardRoot != null)
            leaderboardRoot.style.display = DisplayStyle.None;
    }

    private void OnExitClicked()
    {
        Debug.Log("exit button clicked!");
        Application.Quit();
    }

    private void OnMainMenuClicked()
    {
        mainMenuOverlay.style.display = DisplayStyle.Flex;
        optionsOverlay.style.display = DisplayStyle.None;
        gameMenuOverlay.style.display = DisplayStyle.None;
        gameMenuMissionsOverlay.style.display = DisplayStyle.None;

        if (leaderboardRoot != null)
            leaderboardRoot.style.display = DisplayStyle.None;

        background.style.display = DisplayStyle.Flex;
    }

    // ---------------- Leaderboard ----------------
    private void OnLeaderboardClicked()
    {
        // Hide other overlays
        optionsOverlay.style.display = DisplayStyle.None;

        // Keep background visible (looks like your design)
        background.style.display = DisplayStyle.Flex;

        if (leaderboardRoot != null)
        {
            leaderboardRoot.style.display = DisplayStyle.Flex;
            RefreshLeaderboard();
        }
    }

    private void OnPlayClicked()
    {
        if (modeManager.CurrentMode == ModeManager.ModeType.Emergency)
        {
            SceneManager.LoadScene("EmergencyLanding");
        }
        else if (modeManager.CurrentMode == ModeManager.ModeType.Standard)
        {
            // standard logic
        }
        else if (modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            SceneManager.LoadScene("FuelMode");
        }
    }

    private void ClearLeaderboard()
    {
        LeaderboardStore.Clear();
        RefreshLeaderboard();
    }

    private void SetupLeaderboardListView()
    {
        if (leaderboardList == null)
        {
            Debug.LogError("UIController: Leaderboard ListView not found. Name it 'leaderboardList' in UXML.");
            return;
        }

        leaderboardList.selectionType = SelectionType.None;

        // Create a row VisualElement (matches your .row/.col styling approach)
        leaderboardList.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("row");

            // 6 columns: Rank | Mode | Time | Score | Grade | Result
            row.Add(MakeCol("rank", "col"));
            row.Add(MakeCol("mode", "col"));
            row.Add(MakeCol("time", "col"));
            row.Add(MakeCol("score", "col"));
            row.Add(MakeCol("grade", "col"));
            row.Add(MakeCol("result", "col"));

            return row;
        };

        leaderboardList.bindItem = (element, index) =>
        {
            if (index < 0 || index >= cachedEntries.Count) return;

            var e = cachedEntries[index];

            // query labels by name we assigned in MakeCol
            element.Q<Label>("rank").text = (index + 1).ToString();
            element.Q<Label>("mode").text = e.mode;
            element.Q<Label>("time").text = FormatTime(e.timeSeconds);
            element.Q<Label>("score").text = e.score.ToString();
            element.Q<Label>("grade").text = e.grade;
            element.Q<Label>("result").text = e.success ? "SUCCESS" : "FAIL";
        };
    }

    private Label MakeCol(string name, string className)
    {
        var l = new Label("-");
        l.name = name;
        l.AddToClassList(className);

        // If you already have a ".rank" selector etc, this lets it apply too
        l.AddToClassList(name);

        return l;
    }

    private void RefreshLeaderboard()
    {
        if (leaderboardList == null) return;

        // Load newest data
        cachedEntries = new List<LeaderboardStore.Entry>(LeaderboardStore.GetEntriesSorted());
        leaderboardList.itemsSource = cachedEntries;
        leaderboardList.Rebuild();
    }

    private static string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int mins = s / 60;
        int secs = s % 60;
        return $"{mins:00}:{secs:00}";
    }
}