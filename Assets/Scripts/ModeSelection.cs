using UnityEngine;
using UnityEngine.UIElements;

public class ModeSelection : MonoBehaviour
{
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private UIDocument gameMenuDocument;

    private VisualElement root;
    private VisualElement missionSelect;
    private VisualElement missionsOverlay;

    private Button leftBtn;
    private Button rightBtn;

    private Label modeLabel;
    private VisualElement standardMission;
    private VisualElement emergencyMission;
    private VisualElement fuelMission;

    private int currentModeIndex = 0;

    private void Awake()
    {
        // ALWAYS rebind to the persistent singleton (fixes after scene reload)
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) modeManager = FindFirstObjectByType<ModeManager>();
    }

    private void Start()
    {
        if (gameMenuDocument == null)
        {
            Debug.LogError("ModeSelection: gameMenuDocument not assigned.");
            enabled = false;
            return;
        }

        root = gameMenuDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("ModeSelection: rootVisualElement is null.");
            enabled = false;
            return;
        }

        missionSelect = root.Q<VisualElement>("missionSelect");
        if (missionSelect == null)
        {
            Debug.LogError("ModeSelection: Could not find #missionSelect in UXML.");
            enabled = false;
            return;
        }

        missionsOverlay = root.Q<VisualElement>("Missions");
        if (missionsOverlay == null)
        {
            Debug.LogError("ModeSelection: Could not find #Missions in UXML.");
            enabled = false;
            return;
        }

        leftBtn = missionsOverlay.Q<Button>("missionLeftBtn");
        rightBtn = missionsOverlay.Q<Button>("missionRightBtn");

        if (leftBtn == null || rightBtn == null)
        {
            Debug.LogError("ModeSelection: missionLeftBtn/missionRightBtn not found under #Missions.");
            enabled = false;
            return;
        }

        // Cache rest of UI
        modeLabel = root.Q<Label>("modeLabel");
        standardMission = missionsOverlay.Q<VisualElement>("StandardMission");
        emergencyMission = missionsOverlay.Q<VisualElement>("EmergencyMission");
        fuelMission = missionsOverlay.Q<VisualElement>("FuelMission");

        // Hook buttons
        leftBtn.clicked += OnLeft;
        rightBtn.clicked += OnRight;

        // Sync from ModeManager every time this menu scene loads
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager != null)
            currentModeIndex = modeManager.CurrentModeIndex;
        else
            currentModeIndex = 0;

        ApplyMode(); // updates UI + ModeManager

        // DON'T hide missionSelect here unless you really want it hidden always.
        // If you want it hidden at boot, do it from UIController when showing/hiding screens.
        // missionSelect.style.display = DisplayStyle.None;
    }

    private void OnDestroy()
    {
        if (leftBtn != null) leftBtn.clicked -= OnLeft;
        if (rightBtn != null) rightBtn.clicked -= OnRight;
    }

    private void OnLeft() => ChangeMode(-1);
    private void OnRight() => ChangeMode(+1);

    public void ShowMenu()
    {
        if (missionSelect != null)
            missionSelect.style.display = DisplayStyle.Flex;
    }

    public void HideMenu()
    {
        if (missionSelect != null)
            missionSelect.style.display = DisplayStyle.None;
    }

    private void ChangeMode(int delta)
    {
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null || modeManager.ModeCount == 0) return;

        currentModeIndex = WrapIndex(currentModeIndex + delta, modeManager.ModeCount);
        ApplyMode();
    }

    private void ApplyMode()
    {
        if (modeManager == null) modeManager = ModeManager.Instance;
        if (modeManager == null) return;

        modeManager.SetModeByIndex(currentModeIndex);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (modeManager == null) return;

        var mode = modeManager.CurrentMode;

        if (modeLabel != null)
            modeLabel.text = mode.ToString().ToUpper();

        if (standardMission != null)
            standardMission.style.display = (mode == ModeManager.ModeType.Standard) ? DisplayStyle.Flex : DisplayStyle.None;

        if (emergencyMission != null)
            emergencyMission.style.display = (mode == ModeManager.ModeType.Emergency) ? DisplayStyle.Flex : DisplayStyle.None;

        if (fuelMission != null)
            fuelMission.style.display = (mode == ModeManager.ModeType.Fuel) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private int WrapIndex(int i, int count)
    {
        i %= count;
        if (i < 0) i += count;
        return i;
    }
}