using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LandingResultsUI : MonoBehaviour
{
    [SerializeField] private UIDocument resultsDocument;

    [Header("UXML Names")]
    [SerializeField] private string rootName = "ResultsRoot";
    [SerializeField] private string mainMenuButtonName = "mainMenuBtn";

    [SerializeField] private float revealDelay = 0.7f;

    private VisualElement root;
    private Button mainMenuBtn;

    private Label lineLanding, lineAlignment, lineBank, lineDescent, lineSpeed, lineTotal;

    // NEW: Only pause fuel in Fuel mode
    private FuelModeAddon fuelAddon;
    private ModeManager modeManager;

    void Awake()
    {
        if (resultsDocument == null) resultsDocument = GetComponent<UIDocument>();

        // NEW
        modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();
        fuelAddon = FindFirstObjectByType<FuelModeAddon>();

        var ve = resultsDocument.rootVisualElement;

        root = ve.Q<VisualElement>(rootName);

        lineLanding = ve.Q<Label>("lineLanding");
        lineAlignment = ve.Q<Label>("lineAlignment");
        lineBank = ve.Q<Label>("lineBank");
        lineDescent = ve.Q<Label>("lineDescent");
        lineSpeed = ve.Q<Label>("lineSpeed");
        lineTotal = ve.Q<Label>("lineTotal");

        mainMenuBtn = ve.Q<Button>(mainMenuButtonName);

        if (mainMenuBtn != null)
            mainMenuBtn.clicked += OnMainMenuClicked;

        HideAll();
    }

    private void OnMainMenuClicked()
    {
        // NEW: resume fuel before leaving (Fuel mode only)
        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
        }

        SceneManager.LoadScene("MainMenuScene");
    }

    public void HideAll()
    {
        if (root != null) root.style.display = DisplayStyle.None;
        ClearAndHideLines();

        // NEW: resume fuel if hiding (Fuel mode only)
        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
        }
    }

    private void ClearAndHideLines()
    {
        HideLine(lineLanding);
        HideLine(lineAlignment);
        HideLine(lineBank);
        HideLine(lineDescent);
        HideLine(lineSpeed);
        HideLine(lineTotal);

        if (mainMenuBtn != null)
            mainMenuBtn.style.display = DisplayStyle.None;
    }

    private void HideLine(Label l)
    {
        if (l == null) return;
        l.text = "";
        l.style.display = DisplayStyle.None;
    }

    private void ShowLine(Label l, string text)
    {
        if (l == null) return;
        l.text = text;
        l.style.display = DisplayStyle.Flex;
    }

    public void PlaySequence(LandingScoreData data)
    {
        StopAllCoroutines();

        // NEW: pause fuel as soon as results show (Fuel mode only)
        if (modeManager == null) modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();
        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(true);
        }

        if (root != null) root.style.display = DisplayStyle.Flex;
        ClearAndHideLines();

        StartCoroutine(SequenceRoutine(data));
    }

    private IEnumerator SequenceRoutine(LandingScoreData d)
    {
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineLanding, d.success ? "LANDING: SUCCESS" : $"LANDING: FAILED ({d.failReason})");
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineAlignment, $"ALIGNMENT: {d.yawPts}/{d.maxYawPts}");
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineBank, $"WINGS LEVEL: {d.bankPts}/{d.maxBankPts}");
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineDescent, $"SMOOTHNESS: {d.descentPts}/{d.maxDescentPts}");
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineSpeed, $"SPEED: {d.speedPts}/{d.maxSpeedPts}");
        yield return new WaitForSecondsRealtime(revealDelay);

        ShowLine(lineTotal, $"TOTAL: {d.total}/100  ({d.grade})");
        yield return new WaitForSecondsRealtime(revealDelay);

        if (mainMenuBtn != null)
            mainMenuBtn.style.display = DisplayStyle.Flex;

        // Keep fuel paused until user leaves or HideAll() is called
    }
}