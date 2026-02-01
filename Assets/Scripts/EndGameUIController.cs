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
    private VisualElement panel;

    private Label titleTxt;
    private Label lineLanding, lineAlignment, lineBank, lineDescent, lineSpeed, lineTotal;
    private Button mainMenuBtn;

    void Awake()
    {
        if (doc == null) doc = GetComponent<UIDocument>();

        root = doc.rootVisualElement;

        resultsRoot = root.Q<VisualElement>("ResultsRoot");
        panel = root.Q<VisualElement>("Panel");

        titleTxt = root.Q<Label>("titleTxt");
        lineLanding = root.Q<Label>("lineLanding");
        lineAlignment = root.Q<Label>("lineAlignment");
        lineBank = root.Q<Label>("lineBank");
        lineDescent = root.Q<Label>("lineDescent");
        lineSpeed = root.Q<Label>("lineSpeed");
        lineTotal = root.Q<Label>("lineTotal");

        mainMenuBtn = root.Q<Button>("mainMenuBtn");

        if (mainMenuBtn != null)
            mainMenuBtn.clicked += () => SceneManager.LoadScene(mainMenuSceneName);

        HideAll();
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

        if (resultsRoot != null) resultsRoot.style.display = DisplayStyle.Flex;
        if (titleTxt != null) titleTxt.text = d.success ? "LANDING SUCCESS" : "LANDING FAILED";

        // start hidden
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
    }
}
