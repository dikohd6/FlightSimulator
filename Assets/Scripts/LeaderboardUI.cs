using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private UIDocument doc;

    [Header("UXML Names")]
    [SerializeField] private string rootName = "LeaderboardRoot";
    [SerializeField] private string listName = "leaderboardList";      // if you don't have a ListView, see note below
    [SerializeField] private string clearBtnName = "clearBtn";

    private VisualElement root;
    private ListView listView;
    private Button clearBtn;

    private readonly List<LeaderboardStore.Entry> entries = new();

    void Awake()
    {
        if (doc == null) doc = GetComponent<UIDocument>();

        var ve = doc.rootVisualElement;

        root = ve.Q<VisualElement>(rootName);
        listView = ve.Q<ListView>(listName);
        clearBtn = ve.Q<Button>(clearBtnName);

        if (root == null) Debug.LogError($"LeaderboardUI: Missing VisualElement '{rootName}'");
        if (listView == null)
            Debug.LogError($"LeaderboardUI: Missing ListView '{listName}'");

        if (clearBtn != null) clearBtn.clicked += OnClear;
        if (listView != null)
            SetupListView();
        Hide(); // start hidden
    }
    
    private void SetupListView()
    {
        listView.fixedItemHeight = 300;
        listView.selectionType = SelectionType.None;

        listView.makeItem = () =>
        {
            var row = new VisualElement();
            row.AddToClassList("row");

            row.Add(MakeLabel("rank", "col rank"));
            row.Add(MakeLabel("mode", "col mode"));
            row.Add(MakeLabel("time", "col time"));
            row.Add(MakeLabel("score", "col score"));
            row.Add(MakeLabel("grade", "col grade"));
            row.Add(MakeLabel("result", "col result"));

            return row;
        };

        listView.bindItem = (element, index) =>
        {
            if (index < 0 || index >= entries.Count) return;

            var e = entries[index];

            element.Q<Label>("rank").text = (index + 1).ToString();
            element.Q<Label>("mode").text = e.mode ?? "";
            element.Q<Label>("time").text = FormatTime(e.timeSeconds);
            element.Q<Label>("score").text = $"{e.score}/100";
            element.Q<Label>("grade").text = e.grade ?? "";
            var resultLabel = element.Q<Label>("result");
            resultLabel.text = e.success ? "SUCCESS" : "FAILED";
            resultLabel.RemoveFromClassList("success");
            resultLabel.RemoveFromClassList("fail");
            resultLabel.AddToClassList(e.success ? "success" : "fail");

        };

        // Set source ONCE
        listView.itemsSource = entries;
    }

    private Label MakeLabel(string name, string classes)
    {
        var l = new Label { name = name };
        foreach (var c in classes.Split(' '))
            if (!string.IsNullOrWhiteSpace(c)) l.AddToClassList(c);
        return l;
    }

    private string FormatTime(float seconds)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
        int mins = s / 60;
        int secs = s % 60;
        return $"{mins:00}:{secs:00}";
    }

    public void Show()
    {
        Refresh();
        if (root != null) root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (root != null) root.style.display = DisplayStyle.None;
    }

    public void Refresh()
    {
        entries.Clear();

        foreach (var e in LeaderboardStore.GetEntriesSorted())
            entries.Add(e);

        if (listView != null)
            listView.RefreshItems();
    }

    private void OnClear()
    {
        LeaderboardStore.Clear();
        Refresh();
    }
}
