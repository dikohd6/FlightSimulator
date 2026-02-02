using UnityEngine;
using UnityEngine.UIElements;

public class ModeSelection : MonoBehaviour
{
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private UIDocument gameMenuDocument;
    private VisualElement missionsOverlay;

    private VisualElement root;
    private VisualElement missionSelect;
    private Button leftBtn;
    private Button rightBtn;
    private Label modeLabel;
    private VisualElement standardMission;
    private VisualElement emergencyMission;

    private int currentModeIndex = 0;

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

        // Grab UI after it exists
        missionSelect = root.Q<VisualElement>("missionSelect");
        if (missionSelect == null)
        {
            Debug.LogError("ModeSelection: Could not find #missionSelect in UXML.");
            enabled = false;
            return;
        }
        missionsOverlay = root.Q<VisualElement>("Missions");

        leftBtn = missionsOverlay.Q<Button>("missionLeftBtn");
        rightBtn = missionsOverlay.Q<Button>("missionRightBtn");

        if (leftBtn == null || rightBtn == null)
        {
            Debug.LogError("ModeSelection: leftBtn or rightBtn not found.");
            enabled = false;
            return;
        }

        leftBtn.clicked += () => ChangeMode(-1);
        rightBtn.clicked += () => ChangeMode(+1);
        modeLabel = root.Q<Label>("modeLabel");
        standardMission = missionsOverlay.Q<VisualElement>("StandardMission");
        emergencyMission = missionsOverlay.Q<VisualElement>("EmergencyMission");

        // Sync with ModeManager
        if (modeManager != null)
        {
            currentModeIndex = modeManager.CurrentModeIndex;
            ApplyMode();
        }

        // 🔹 HIDE THE GAME MENU AT START (this is the important part)
        missionSelect.style.display = DisplayStyle.None;
    }

    // Call this when player opens the game menu
    public void ShowMenu()
    {
        if (missionSelect != null)
            missionSelect.style.display = DisplayStyle.Flex;
    }

    // Call this when closing the menu / starting game
    public void HideMenu()
    {
        if (missionSelect != null)
            missionSelect.style.display = DisplayStyle.None;
    }

    private void ChangeMode(int delta)
    {
        if (modeManager == null || modeManager.ModeCount == 0) return;

        currentModeIndex = WrapIndex(currentModeIndex + delta, modeManager.ModeCount);
        ApplyMode();
    }

    private void ApplyMode()
    {
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
            standardMission.style.display =
                (mode == ModeManager.ModeType.Standard) ? DisplayStyle.Flex : DisplayStyle.None;

        if (emergencyMission != null)
            emergencyMission.style.display =
                (mode == ModeManager.ModeType.Emergency) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private int WrapIndex(int i, int count)
    {
        i %= count;
        if (i < 0) i += count;
        return i;
    }


}
