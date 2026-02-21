using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class EndGameUIController : MonoBehaviour
{
    [SerializeField] private UIDocument doc;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float revealDelay = 0.75f;

    private VisualElement root;
    private VisualElement resultsRoot;

    private Label titleTxt;
    private Label lineLanding, lineAlignment, lineBank, lineDescent, lineSpeed, lineTotal;
    private Button mainMenuBtn;

    private FuelModeAddon fuelAddon;

    void Awake()
    {
        if (doc == null) doc = GetComponent<UIDocument>();

        root = doc.rootVisualElement;

        resultsRoot = root.Q<VisualElement>("ResultsRoot");

        titleTxt = root.Q<Label>("titleTxt");
        lineLanding = root.Q<Label>("lineLanding");
        lineAlignment = root.Q<Label>("lineAlignment");
        lineBank = root.Q<Label>("lineBank");
        lineDescent = root.Q<Label>("lineDescent");
        lineSpeed = root.Q<Label>("lineSpeed");
        lineTotal = root.Q<Label>("lineTotal");

        mainMenuBtn = root.Q<Button>("mainMenuBtn");

        fuelAddon = FindFirstObjectByType<FuelModeAddon>();

        if (mainMenuBtn != null)
            mainMenuBtn.clicked += OnMainMenuClicked;

        HideAll();
    }

    private void OnMainMenuClicked()
    {
        if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void HideAll()
    {
        if (resultsRoot != null) resultsRoot.style.display = DisplayStyle.None;

        Hide(lineLanding);
        Hide(lineAlignment);
        Hide(lineBank);
        Hide(lineDescent);
        Hide(lineSpeed);
        Hide(lineTotal);

        if (mainMenuBtn != null) mainMenuBtn.style.display = DisplayStyle.None;

        if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
    }

    private void Hide(Label l)
    {
        if (l == null) return;
        l.text = "";
        l.style.display = DisplayStyle.None;
    }

    private void Show(Label l, string text)
    {
        if (l == null) return;
        l.text = text;
        l.style.display = DisplayStyle.Flex;
    }

    public void PlayResultsSequence(LandingScoreData d)
    {
        StopAllCoroutines();

        if (fuelAddon == null) fuelAddon = FindFirstObjectByType<FuelModeAddon>();
        if (fuelAddon != null) fuelAddon.SetFuelPaused(true);

        if (resultsRoot != null) resultsRoot.style.display = DisplayStyle.Flex;
        if (titleTxt != null) titleTxt.text = d.success ? "LANDING SUCCESS" : "LANDING FAILED";

        Hide(lineLanding);
        Hide(lineAlignment);
        Hide(lineBank);
        Hide(lineDescent);
        Hide(lineSpeed);
        Hide(lineTotal);
        if (mainMenuBtn != null) mainMenuBtn.style.display = DisplayStyle.None;

        StartCoroutine(RevealRoutine(d));
    }

    private IEnumerator RevealRoutine(LandingScoreData d)
    {
        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineLanding, $"LANDING: {(d.success ? "SUCCESS" : "FAILED")}");

        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineAlignment, $"ALIGNMENT: {d.yawPts}/{d.maxYawPts}");

        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineBank, $"WINGS LEVEL: {d.bankPts}/{d.maxBankPts}");

        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineDescent, $"SMOOTHNESS: {d.descentPts}/{d.maxDescentPts}");

        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineSpeed, $"SPEED: {d.speedPts}/{d.maxSpeedPts}");

        yield return new WaitForSecondsRealtime(revealDelay);
        Show(lineTotal, $"TOTAL: {d.total}/100  ({d.grade})");

        yield return new WaitForSecondsRealtime(revealDelay);
        if (mainMenuBtn != null) mainMenuBtn.style.display = DisplayStyle.Flex;

        // Keep fuel paused until they leave / HideAll is called
    }
}