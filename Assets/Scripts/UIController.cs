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

    [Header("Options UI (Volume Slider)")]
    [SerializeField] private string volumeSliderName = "volumeSlider"; // set this to your slider's UXML name
    [SerializeField] private bool sliderUsesPercentRange = true;       // true if slider range is 0-100, false if 0-1

    // Root documents (important fix: hide/show entire docs)
    private VisualElement mainRoot;
    private VisualElement gameRoot;

    private VisualElement mainMenuOverlay;
    private VisualElement gameMenuOverlay;
    private VisualElement gameMenuMissionsOverlay;
    private VisualElement gameMenuStatsOverlay;

    private VisualElement optionsOverlay;

    private Button startBtn;
    private Button optionsBtn;
    private Button exitBtn;
    private Button menubtn;       // main menu button inside Options (main doc)
    private Button GameMenubtn;   // main menu button inside Hangar (game doc)
    private Button playBtn;

    private Image background;

    // ---------- Options ----------
    private Slider volumeSlider;
    private bool suppressVolumeSliderCallback;

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
        if (mainMenuDocument == null)
        {
            Debug.LogError("UIController: mainMenuDocument is not assigned.");
            enabled = false;
            return;
        }

        if (gameMenuDocument == null)
        {
            Debug.LogError("UIController: gameMenuDocument is not assigned.");
            enabled = false;
            return;
        }

        // Bind singleton
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        mainRoot = mainMenuDocument.rootVisualElement;
        gameRoot = gameMenuDocument.rootVisualElement;

        if (mainRoot == null || gameRoot == null)
        {
            Debug.LogError("UIController: rootVisualElement is null. Check UIDocument setup.");
            enabled = false;
            return;
        }

        // --- game menu overlays ---
        gameMenuOverlay = gameRoot.Q<VisualElement>("missionSelect");
        gameMenuMissionsOverlay = gameRoot.Q<VisualElement>("Missions");
        gameMenuStatsOverlay = gameRoot.Q<VisualElement>("PlaneStatsGroup");

        // --- main menu overlays ---
        mainMenuOverlay = mainRoot.Q<VisualElement>("MainMenu");
        optionsOverlay = mainRoot.Q<VisualElement>("Options");
        background = mainRoot.Q<Image>("background");

        startBtn = mainRoot.Q<Button>("startBtn");
        optionsBtn = mainRoot.Q<Button>("optionsBtn");
        leaderboardBtn = mainRoot.Q<Button>("leaderboardBtn");
        exitBtn = mainRoot.Q<Button>("exitBtn");

        menubtn = optionsOverlay != null ? optionsOverlay.Q<Button>("mainMenuBtn") : null;
        GameMenubtn = gameMenuOverlay != null ? gameMenuOverlay.Q<Button>("mainMenuBtn") : null;
        playBtn = gameMenuOverlay != null ? gameMenuOverlay.Q<Button>("playBtn") : null;

        // -------- Volume Slider bind --------
        if (optionsOverlay != null)
        {
            volumeSlider = optionsOverlay.Q<Slider>(volumeSliderName);
            if (volumeSlider == null) volumeSlider = optionsOverlay.Q<Slider>();

            if (volumeSlider != null)
            {
                volumeSlider.RegisterValueChangedCallback(OnVolumeSliderChanged);
                SyncVolumeSliderFromSavedSettings();
            }
            else
            {
                Debug.LogWarning($"UIController: Could not find volume slider '{volumeSliderName}' in Options panel.");
            }
        }
        else
        {
            Debug.LogWarning("UIController: Could not find 'Options' panel in MainMenu UXML.");
        }

        // --- Leaderboard panel ---
        leaderboardRoot = mainRoot.Q<VisualElement>("LeaderboardRoot");
        if (leaderboardRoot != null)
        {
            leaderboardMainMenuBtn = leaderboardRoot.Q<Button>("mainMenuBtn");
            leaderboardClearBtn = leaderboardRoot.Q<Button>("clearBtn");

            leaderboardList = leaderboardRoot.Q<ListView>("leaderboardList");
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
        if (startBtn != null) startBtn.clicked += OnStartClicked;
        if (optionsBtn != null) optionsBtn.clicked += OnOptionsClicked;
        if (exitBtn != null) exitBtn.clicked += OnExitClicked;

        if (menubtn != null) menubtn.clicked += OnMainMenuClicked;
        if (GameMenubtn != null) GameMenubtn.clicked += OnMainMenuClicked;

        if (playBtn != null) playBtn.clicked += OnPlayClicked;

        if (leaderboardMainMenuBtn != null) leaderboardMainMenuBtn.clicked += OnMainMenuClicked;
        if (leaderboardBtn != null) leaderboardBtn.clicked += OnLeaderboardClicked;
        if (leaderboardClearBtn != null) leaderboardClearBtn.clicked += ClearLeaderboard;

        // ✅ IMPORTANT FIX: start in main menu state and HIDE the entire game menu document
        ShowMainMenuState();
    }

    private void Start()
    {
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        var mm = MusicManager.Instance != null ? MusicManager.Instance : FindFirstObjectByType<MusicManager>();
        if (mm != null)
            mm.SetVolume(GameAudioSettings.MusicVolume);

        SyncVolumeSliderFromSavedSettings();
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
            volumeSlider.UnregisterValueChangedCallback(OnVolumeSliderChanged);

        if (startBtn != null) startBtn.clicked -= OnStartClicked;
        if (optionsBtn != null) optionsBtn.clicked -= OnOptionsClicked;
        if (exitBtn != null) exitBtn.clicked -= OnExitClicked;

        if (menubtn != null) menubtn.clicked -= OnMainMenuClicked;
        if (GameMenubtn != null) GameMenubtn.clicked -= OnMainMenuClicked;
        if (playBtn != null) playBtn.clicked -= OnPlayClicked;

        if (leaderboardMainMenuBtn != null) leaderboardMainMenuBtn.clicked -= OnMainMenuClicked;
        if (leaderboardBtn != null) leaderboardBtn.clicked -= OnLeaderboardClicked;
        if (leaderboardClearBtn != null) leaderboardClearBtn.clicked -= ClearLeaderboard;
    }

    // ---------------- STATE HELPERS (KEY FIX) ----------------

    private void ShowMainMenuState()
    {
        // Show main doc, hide game doc completely (this prevents purchaseBtn leaking)
        if (mainRoot != null) mainRoot.style.display = DisplayStyle.Flex;
        if (gameRoot != null) gameRoot.style.display = DisplayStyle.None;

        if (mainMenuOverlay != null) mainMenuOverlay.style.display = DisplayStyle.Flex;
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;

        if (leaderboardRoot != null) leaderboardRoot.style.display = DisplayStyle.None;

        if (background != null) background.style.display = DisplayStyle.Flex;

        // Extra safety
        if (gameMenuOverlay != null) gameMenuOverlay.style.display = DisplayStyle.None;
        if (gameMenuMissionsOverlay != null) gameMenuMissionsOverlay.style.display = DisplayStyle.None;
        if (gameMenuStatsOverlay != null) gameMenuStatsOverlay.style.display = DisplayStyle.None;
    }

    private void ShowHangarState()
    {
        // Hide main menu stuff
        if (mainMenuOverlay != null) mainMenuOverlay.style.display = DisplayStyle.None;
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (leaderboardRoot != null) leaderboardRoot.style.display = DisplayStyle.None;
        if (background != null) background.style.display = DisplayStyle.None;

        // Show game menu doc + overlays
        if (gameRoot != null) gameRoot.style.display = DisplayStyle.Flex;

        if (gameMenuOverlay != null) gameMenuOverlay.style.display = DisplayStyle.Flex;
        if (gameMenuMissionsOverlay != null) gameMenuMissionsOverlay.style.display = DisplayStyle.Flex;
        if (gameMenuStatsOverlay != null) gameMenuStatsOverlay.style.display = DisplayStyle.Flex;

        if (hangarGate != null) hangarGate.OpenGates();
    }

    private void HideAllDocsBeforeSceneLoad()
    {
        // If anything persists unexpectedly, this prevents stuck UI across scene load
        if (mainRoot != null) mainRoot.style.display = DisplayStyle.None;
        if (gameRoot != null) gameRoot.style.display = DisplayStyle.None;
    }

    // ---------------- Main Menu ----------------

    private void OnStartClicked()
    {
        ShowHangarState();
    }

    private void OnOptionsClicked()
    {
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.Flex;
        if (mainMenuOverlay != null) mainMenuOverlay.style.display = DisplayStyle.None;

        if (leaderboardRoot != null)
            leaderboardRoot.style.display = DisplayStyle.None;

        SyncVolumeSliderFromSavedSettings();
    }

    private void OnExitClicked()
    {
        Debug.Log("exit button clicked!");
        Application.Quit();
    }

    private void OnMainMenuClicked()
    {
        ShowMainMenuState();

        if (modeManager == null) modeManager = ModeManager.Instance;
    }

    // ---------------- Volume ----------------

    private void SyncVolumeSliderFromSavedSettings()
    {
        if (volumeSlider == null) return;

        float saved01 = GameAudioSettings.MusicVolume;
        float sliderValue = sliderUsesPercentRange ? (saved01 * 100f) : saved01;

        suppressVolumeSliderCallback = true;
        volumeSlider.SetValueWithoutNotify(sliderValue);
        suppressVolumeSliderCallback = false;
    }

    private void OnVolumeSliderChanged(ChangeEvent<float> evt)
    {
        if (suppressVolumeSliderCallback) return;

        float v01 = sliderUsesPercentRange
            ? Mathf.Clamp01(evt.newValue / 100f)
            : Mathf.Clamp01(evt.newValue);

        var mm = MusicManager.Instance != null ? MusicManager.Instance : FindFirstObjectByType<MusicManager>();
        if (mm != null)
            mm.SetVolume(v01);
        else
            GameAudioSettings.MusicVolume = v01;
    }

    // ---------------- Leaderboard ----------------

    private void OnLeaderboardClicked()
    {
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (background != null) background.style.display = DisplayStyle.Flex;

        if (leaderboardRoot != null)
        {
            leaderboardRoot.style.display = DisplayStyle.Flex;
            RefreshLeaderboard();
        }
    }

    private void OnPlayClicked()
    {
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        if (modeManager == null)
        {
            Debug.LogError("UIController: ModeManager is missing, cannot Play.");
            return;
        }

        var pm = PlaneManager.Instance != null ? PlaneManager.Instance : FindFirstObjectByType<PlaneManager>();
        if (pm != null && !pm.IsPlanePurchased(modeManager.SelectedPlaneIndex))
        {
            Debug.Log("🚫 Selected plane is locked. Purchase it first.");
            return;
        }

        HideAllDocsBeforeSceneLoad();

        if (modeManager.CurrentMode == ModeManager.ModeType.Emergency)
            SceneManager.LoadScene("EmergencyLanding");
        else if (modeManager.CurrentMode == ModeManager.ModeType.Standard)
            SceneManager.LoadScene("StandardMode");
        else if (modeManager.CurrentMode == ModeManager.ModeType.Fuel)
            SceneManager.LoadScene("FuelMode");
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

        leaderboardList.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("row");

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

            var rank = element.Q<Label>("rank");
            var mode = element.Q<Label>("mode");
            var time = element.Q<Label>("time");
            var score = element.Q<Label>("score");
            var grade = element.Q<Label>("grade");
            var result = element.Q<Label>("result");

            if (rank != null) rank.text = (index + 1).ToString();
            if (mode != null) mode.text = e.mode;
            if (time != null) time.text = FormatTime(e.timeSeconds);
            if (score != null) score.text = e.score.ToString();
            if (grade != null) grade.text = e.grade;
            if (result != null) result.text = e.success ? "SUCCESS" : "FAIL";
        };
    }

    private Label MakeCol(string name, string className)
    {
        var l = new Label("-");
        l.name = name;
        l.AddToClassList(className);
        l.AddToClassList(name);
        return l;
    }

    private void RefreshLeaderboard()
    {
        if (leaderboardList == null) return;

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