using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaneSelection : MonoBehaviour
{
    [SerializeField] private PlaneManager planeManager;
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private UIDocument gameMenuDocument;

    [Header("Where the name images live in UXML")]
    [SerializeField] private string namesContainerUxmlName = "PlanesStatsGroup";
    // If you don't have this parent, set this to "" and it will search the whole document.

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float spinSpeed = 30f;

    private VisualElement root;
    private VisualElement overlayRoot;
    private Button leftBtn;
    private Button rightBtn;

    private int currentIndex = 0;
    private GameObject currentPreview;

    private readonly Dictionary<int, VisualElement> nameElements = new();

    private void Awake()
    {
        if (planeManager == null)
            planeManager = PlaneManager.Instance != null ? PlaneManager.Instance : FindFirstObjectByType<PlaneManager>();

        if (modeManager == null)
            modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();

        if (gameMenuDocument == null)
        {
            Debug.LogError("PlaneSelection: gameMenuDocument is not set.");
            enabled = false;
            return;
        }

        root = gameMenuDocument.rootVisualElement;

        // Your buttons are inside missionSelect, but names might NOT be.
        overlayRoot = root.Q<VisualElement>("missionSelect") ?? root;

        leftBtn = overlayRoot.Q<Button>("leftBtn");
        rightBtn = overlayRoot.Q<Button>("rightBtn");

        if (leftBtn != null) leftBtn.clicked += OnLeftButtonClicked;
        if (rightBtn != null) rightBtn.clicked += OnRightButtonClicked;
    }

    private void Start()
    {
        // Wait one tick so UI Toolkit has fully built the visual tree.
        root.schedule.Execute(() =>
        {
            CacheNameElements();
            SetIndex(0, true);
        }).StartingIn(0);
    }

    private void OnDestroy()
    {
        if (leftBtn != null) leftBtn.clicked -= OnLeftButtonClicked;
        if (rightBtn != null) rightBtn.clicked -= OnRightButtonClicked;
    }

    private void Update()
    {
        if (currentPreview)
            currentPreview.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void CacheNameElements()
    {
        nameElements.Clear();

        VisualElement container = null;
        if (!string.IsNullOrEmpty(namesContainerUxmlName))
            container = root.Q<VisualElement>(namesContainerUxmlName);

        for (int i = 0; i < planeManager.planes.Length; i++)
        {
            var uxmlName = planeManager.planes[i].uiNameElement;
            if (string.IsNullOrEmpty(uxmlName))
                continue;

            // Search inside container first (best), then fallback to whole doc.
            VisualElement ve =
                (container != null ? container.Q<VisualElement>(uxmlName) : null)
                ?? root.Q<VisualElement>(uxmlName);

            if (ve == null)
            {
                Debug.LogWarning($"PlaneSelection: Could not find UXML element named '{uxmlName}'.");
                continue;
            }

            // Hide everything by default, so no stacking.
            ve.style.display = DisplayStyle.None;

            // Make sure these images never block clicks.
            ve.pickingMode = PickingMode.Ignore;

            nameElements[i] = ve;
        }
    }

    private void ShowNameForIndex(int index)
    {
        foreach (var kv in nameElements)
            kv.Value.style.display = DisplayStyle.None;

        if (nameElements.TryGetValue(index, out var ve))
            ve.style.display = DisplayStyle.Flex;
    }

    private void SetIndex(int newIndex, bool force = false)
    {
        if (!force && newIndex == currentIndex) return;

        currentIndex = Mathf.Clamp(newIndex, 0, planeManager.planes.Length - 1);

        SpawnPreview(currentIndex);
        ShowNameForIndex(currentIndex);

        if (modeManager) modeManager.SetSelectedPlane(currentIndex);
    }

    private void SpawnPreview(int index)
    {
        if (currentPreview) Destroy(currentPreview);

        var prefab = planeManager.planes[index].planePrefab;
        if (!prefab)
        {
            Debug.LogError("PlaneSelection: Missing plane prefab on PlaneManager.");
            return;
        }

        currentPreview = Instantiate(prefab, previewAnchor);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
    }

    private void OnLeftButtonClicked()
    {
        int next = (currentIndex - 1 + planeManager.planes.Length) % planeManager.planes.Length;
        SetIndex(next);
    }

    private void OnRightButtonClicked()
    {
        int next = (currentIndex + 1) % planeManager.planes.Length;
        SetIndex(next);
    }
}