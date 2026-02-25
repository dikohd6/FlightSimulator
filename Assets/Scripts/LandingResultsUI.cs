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

    [Header("Coin Count Animation")]
    [SerializeField] private float minCoinCountDuration = 0.8f;
    [SerializeField] private float maxCoinCountDuration = 2.0f;
    [SerializeField] private bool showTotalCoinsAfterReward = true;

    private VisualElement root;
    private Button mainMenuBtn;

    private Label lineLanding;
    private Label lineAlignment;
    private Label lineBank;
    private Label lineDescent;
    private Label lineSpeed;
    private Label lineCoins;   // NEW
    private Label lineTotal;

    private FuelModeAddon fuelAddon;
    private ModeManager modeManager;

    private void Awake()
    {
        if (resultsDocument == null) resultsDocument = GetComponent<UIDocument>();

        modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();
        fuelAddon = FindFirstObjectByType<FuelModeAddon>();

        BindUI();
    }

    private void OnEnable()
    {
        // UI Toolkit can need one tick before styles/elements settle
        SafeHideResultsInstant();

        if (resultsDocument != null && resultsDocument.rootVisualElement != null)
        {
            resultsDocument.rootVisualElement.schedule.Execute(() =>
            {
                BindUI();
                SafeHideResultsInstant();
            }).StartingIn(0);
        }
    }

    private void Start()
    {
        BindUI();
        SafeHideResultsInstant();
    }

    private void BindUI()
    {
        if (resultsDocument == null) return;

        var ve = resultsDocument.rootVisualElement;
        if (ve == null) return;

        root = ve.Q<VisualElement>(rootName);

        lineLanding = ve.Q<Label>("lineLanding");
        lineAlignment = ve.Q<Label>("lineAlignment");
        lineBank = ve.Q<Label>("lineBank");
        lineDescent = ve.Q<Label>("lineDescent");
        lineSpeed = ve.Q<Label>("lineSpeed");
        lineCoins = ve.Q<Label>("lineCoins");   // NEW
        lineTotal = ve.Q<Label>("lineTotal");

        mainMenuBtn = ve.Q<Button>(mainMenuButtonName);

        if (mainMenuBtn != null)
        {
            mainMenuBtn.clicked -= OnMainMenuClicked; // avoid duplicate subscriptions
            mainMenuBtn.clicked += OnMainMenuClicked;
        }
    }

    private void OnMainMenuClicked()
    {
        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
        }

        SceneManager.LoadScene("MainMenuScene");
    }

    private void SafeHideResultsInstant()
    {
        if (root != null)
        {
            root.style.display = DisplayStyle.None;
            root.style.opacity = 0f;
            root.pickingMode = PickingMode.Ignore; // prevent blocking clicks while hidden
        }

        HideLine(lineLanding);
        HideLine(lineAlignment);
        HideLine(lineBank);
        HideLine(lineDescent);
        HideLine(lineSpeed);
        HideLine(lineCoins);   // NEW
        HideLine(lineTotal);

        if (mainMenuBtn != null)
            mainMenuBtn.style.display = DisplayStyle.None;

        // Resume fuel if this gets hidden during gameplay setup (Fuel mode only)
        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
        }
    }

    public void HideAll()
    {
        SafeHideResultsInstant();
    }

    private void ClearAndHideLines()
    {
        HideLine(lineLanding);
        HideLine(lineAlignment);
        HideLine(lineBank);
        HideLine(lineDescent);
        HideLine(lineSpeed);
        HideLine(lineCoins);   // NEW
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

        if (modeManager == null)
            modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();

        if (modeManager != null && modeManager.CurrentMode == ModeManager.ModeType.Fuel)
        {
            if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
            if (fuelAddon != null) fuelAddon.SetFuelPaused(true);
        }

        if (root != null)
        {
            root.style.display = DisplayStyle.Flex;
            root.style.opacity = 1f;
            root.pickingMode = PickingMode.Position;
        }

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

        // COINS line (before total, or move this below total if you prefer)
        if (lineCoins != null)
        {
            yield return new WaitForSecondsRealtime(revealDelay);
            lineCoins.style.display = DisplayStyle.Flex;
            lineCoins.text = "COINS EARNED: +0";
            yield return StartCoroutine(CountCoinsRoutine(d.coinsEarned, d.totalCoinsAfter));
        }

        yield return new WaitForSecondsRealtime(revealDelay);
        ShowLine(lineTotal, $"TOTAL: {d.total}/100  ({d.grade})");

        yield return new WaitForSecondsRealtime(revealDelay * 0.5f);
        if (mainMenuBtn != null)
            mainMenuBtn.style.display = DisplayStyle.Flex;
    }

    private IEnumerator CountCoinsRoutine(int coinsEarned, int totalCoinsAfter)
    {
        if (lineCoins == null) yield break;

        coinsEarned = Mathf.Max(0, coinsEarned);

        float duration = Mathf.Clamp(0.6f + (coinsEarned / 700f), minCoinCountDuration, maxCoinCountDuration);
        float t = 0f;
        int shown = -1;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - p, 3f); // ease out cubic

            int value = Mathf.RoundToInt(Mathf.Lerp(0f, coinsEarned, eased));
            if (value != shown)
            {
                shown = value;
                lineCoins.text = showTotalCoinsAfterReward
                    ? $"COINS EARNED: +{shown}   TOTAL COINS: {totalCoinsAfter}"
                    : $"COINS EARNED: +{shown}";
            }

            yield return null;
        }

        lineCoins.text = showTotalCoinsAfterReward
            ? $"COINS EARNED: +{coinsEarned}   TOTAL COINS: {totalCoinsAfter}"
            : $"COINS EARNED: +{coinsEarned}";
    }
}