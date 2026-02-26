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

    private VisualElement mainMenuOverlay;
    private VisualElement gameMenuOverlay;
    private VisualElement gameMenuMissionsOverlay;
    private VisualElement gameMenuStatsOverlay;

    private VisualElement optionsOverlay;

    private Button startBtn;
    private Button optionsBtn;
    private Button exitBtn;
    private Button menubtn;
    private Button GameMenubtn;
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

        // IMPORTANT: always bind to the persistent singleton after scene loads
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        var mainRoot = mainMenuDocument.rootVisualElement;
        var gameRoot = gameMenuDocument.rootVisualElement;

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

            if (volumeSlider == null)
            {
                // fallback: grab first slider in options panel if name doesn't match
                volumeSlider = optionsOverlay.Q<Slider>();
            }

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

        // Start hidden
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (gameMenuOverlay != null) gameMenuOverlay.style.display = DisplayStyle.None;
        if (gameMenuMissionsOverlay != null) gameMenuMissionsOverlay.style.display = DisplayStyle.None;
        if (gameMenuStatsOverlay != null) gameMenuStatsOverlay.style.display = DisplayStyle.None;

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

        // Hooks (null-safe)
        if (startBtn != null) startBtn.clicked += OnStartClicked;
        if (optionsBtn != null) optionsBtn.clicked += OnOptionsClicked;
        if (exitBtn != null) exitBtn.clicked += OnExitClicked;

        if (menubtn != null) menubtn.clicked += OnMainMenuClicked;
        if (GameMenubtn != null) GameMenubtn.clicked += OnMainMenuClicked;

        if (playBtn != null) playBtn.clicked += OnPlayClicked;

        if (leaderboardMainMenuBtn != null) leaderboardMainMenuBtn.clicked += OnMainMenuClicked;

        if (leaderboardBtn != null) leaderboardBtn.clicked += OnLeaderboardClicked;
        if (leaderboardClearBtn != null) leaderboardClearBtn.clicked += ClearLeaderboard;
    }

    private void Start()
    {
        // Extra safety if scene reload timing is weird
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();

        // Ensure music volume is applied when menu starts
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

    // ---------------- Main Menu ----------------
    private void OnStartClicked()
    {
        if (mainMenuOverlay != null) mainMenuOverlay.style.display = DisplayStyle.None;
        if (background != null) background.style.display = DisplayStyle.None;

        if (gameMenuOverlay != null) gameMenuOverlay.style.display = DisplayStyle.Flex;
        if (gameMenuMissionsOverlay != null) gameMenuMissionsOverlay.style.display = DisplayStyle.Flex;
        if (gameMenuStatsOverlay != null) gameMenuStatsOverlay.style.display = DisplayStyle.Flex;

        if (hangarGate != null) hangarGate.OpenGates();
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
        if (mainMenuOverlay != null) mainMenuOverlay.style.display = DisplayStyle.Flex;
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (gameMenuOverlay != null) gameMenuOverlay.style.display = DisplayStyle.None;
        if (gameMenuMissionsOverlay != null) gameMenuMissionsOverlay.style.display = DisplayStyle.None;
        if (gameMenuStatsOverlay != null) gameMenuStatsOverlay.style.display = DisplayStyle.None;

        if (leaderboardRoot != null)
            leaderboardRoot.style.display = DisplayStyle.None;

        if (background != null)
            background.style.display = DisplayStyle.Flex;

        // IMPORTANT: rebind singleton again (in case scene got reloaded)
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

        // Save + apply immediately
        var mm = MusicManager.Instance != null ? MusicManager.Instance : FindFirstObjectByType<MusicManager>();
        if (mm != null)
            mm.SetVolume(v01); // <-- fixed (was SetMusicVolume)
        else
            GameAudioSettings.MusicVolume = v01; // still save even if manager not found yet
    }

    // ---------------- Leaderboard ----------------
    private void OnLeaderboardClicked()
    {
        // Hide other overlays
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;

        // Keep background visible (looks like your design)
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

        if (modeManager.CurrentMode == ModeManager.ModeType.Emergency)
        {
            SceneManager.LoadScene("EmergencyLanding");
        }
        else if (modeManager.CurrentMode == ModeManager.ModeType.Standard)
        {
            SceneManager.LoadScene("StandardMode");
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