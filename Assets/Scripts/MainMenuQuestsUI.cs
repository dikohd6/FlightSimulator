using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuQuestsUI : MonoBehaviour
{
    [SerializeField] private UIDocument mainMenuDocument;

    [Header("UXML Names")]
    [SerializeField] private string questsRootName = "questsRoot";
    [SerializeField] private string questsTitleName = "questsTitleTxt";
    [SerializeField] private string questsBodyName = "questsBodyTxt";

    private VisualElement root;
    private VisualElement questsRoot;
    private Label questsTitleTxt;
    private Label questsBodyTxt;

    private PlaneManager planeManager;

    private void Awake()
    {
        if (mainMenuDocument == null)
            mainMenuDocument = GetComponent<UIDocument>();

        planeManager = PlaneManager.Instance != null
            ? PlaneManager.Instance
            : FindFirstObjectByType<PlaneManager>();

        BindUI();
    }

    private void OnEnable()
    {
        BindUI();
        Refresh();

        QuestSystem.OnQuestsChanged += Refresh;

        if (planeManager == null)
        {
            planeManager = PlaneManager.Instance != null
                ? PlaneManager.Instance
                : FindFirstObjectByType<PlaneManager>();
        }

        if (planeManager != null)
            planeManager.OnEconomyChanged += Refresh;

        // UI Toolkit sometimes needs one frame after scene/UI loads
        if (mainMenuDocument != null)
        {
            mainMenuDocument.rootVisualElement.schedule.Execute(() =>
            {
                BindUI();
                Refresh();
            }).StartingIn(0);
        }
    }

    private void OnDisable()
    {
        QuestSystem.OnQuestsChanged -= Refresh;

        if (planeManager != null)
            planeManager.OnEconomyChanged -= Refresh;
    }

    private void BindUI()
    {
        if (mainMenuDocument == null) return;

        root = mainMenuDocument.rootVisualElement;
        if (root == null) return;

        questsRoot = root.Q<VisualElement>(questsRootName);
        questsTitleTxt = root.Q<Label>(questsTitleName);
        questsBodyTxt = root.Q<Label>(questsBodyName);

        // Optional safety: keep panel visible if it exists
        if (questsRoot != null)
            questsRoot.style.display = DisplayStyle.Flex;
    }

    public void Refresh()
    {
        if (questsTitleTxt == null || questsBodyTxt == null)
            BindUI();

        if (questsTitleTxt == null || questsBodyTxt == null)
            return;

        questsTitleTxt.text = "QUESTS";

        // ✅ Show ONLY active quests (completed quests are removed from the menu)
        var activeQuests = QuestSystem.GetActiveQuestViews();

        if (activeQuests == null || activeQuests.Count == 0)
        {
            questsBodyTxt.text = "• All quests completed!\n  Nice work.";
            return;
        }

        StringBuilder sb = new StringBuilder();

        foreach (var q in activeQuests)
        {
            sb.Append("• ").AppendLine(q.title);
            sb.Append("  ")
              .Append(q.progress).Append("/").Append(q.target)
              .Append("   Reward: +").Append(q.rewardCoins).AppendLine(" coins");
            sb.AppendLine();
        }

        questsBodyTxt.text = sb.ToString().TrimEnd();
    }

    // Optional helper for testing (call from a debug button if you want)
    public void ForceRefresh() => Refresh();
}