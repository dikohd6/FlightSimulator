using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaneSelection : MonoBehaviour
{
    [SerializeField] private PlaneManager planeManager;
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private UIDocument gameMenuDocument;

    [Header("Where the plane UI groups live in UXML")]
    [SerializeField] private string namesContainerUxmlName = "PlanesStatsGroup";
    // If you don't have this parent, set this to "" and it will search the whole document.

    [Header("UXML Names")]
    [SerializeField] private string purchaseButtonName = "purchaseBtn";
    [SerializeField] private string playButtonName = "playBtn";
    [SerializeField] private string coinLabelName = "coinTxt";
    [SerializeField] private string priceElementChildName = "price"; // child inside each plane group
    [SerializeField] private string leftButtonName = "leftBtn";
    [SerializeField] private string rightButtonName = "rightBtn";

    [Header("Purchase Feedback")]
    [SerializeField] private string purchaseFeedbackLabelName = "purchaseFeedbackTxt";
    [SerializeField] private float feedbackFadeDuration = 1.2f;

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float spinSpeed = 30f;

    private VisualElement root;
    private VisualElement overlayRoot;

    private Button leftBtn;
    private Button rightBtn;
    private Button purchaseBtn;
    private Button playBtn;

    private Label coinTxt;
    private Label purchaseFeedbackTxt;

    private int currentIndex = 0;
    private GameObject currentPreview;
    private Coroutine feedbackRoutine;
    private bool economySubscribed = false;

    // Plane UI groups keyed by plane index (ex: #Boeing737, #LearJet45 group)
    private readonly Dictionary<int, VisualElement> planeGroups = new();

    // Price UI child inside each plane group (optional, can be Label/Image/etc)
    private readonly Dictionary<int, VisualElement> priceElements = new();

    private void Awake()
    {
        // IMPORTANT: only use preexisting singleton instances if inspector refs are missing/destroyed
        TryRebindManagers();

        if (gameMenuDocument == null)
        {
            Debug.LogError("PlaneSelection: gameMenuDocument is not set.");
            enabled = false;
            return;
        }

        root = gameMenuDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("PlaneSelection: UIDocument rootVisualElement is null.");
            enabled = false;
            return;
        }

        // Buttons may be inside missionSelect
        overlayRoot = root.Q<VisualElement>("missionSelect") ?? root;

        leftBtn = overlayRoot.Q<Button>(leftButtonName) ?? root.Q<Button>(leftButtonName);
        rightBtn = overlayRoot.Q<Button>(rightButtonName) ?? root.Q<Button>(rightButtonName);

        purchaseBtn = root.Q<Button>(purchaseButtonName);
        playBtn = overlayRoot.Q<Button>(playButtonName) ?? root.Q<Button>(playButtonName);
        coinTxt = root.Q<Label>(coinLabelName);
        purchaseFeedbackTxt = root.Q<Label>(purchaseFeedbackLabelName);

        if (leftBtn != null) leftBtn.clicked += OnLeftButtonClicked;
        else Debug.LogWarning($"PlaneSelection: Could not find left button '{leftButtonName}'.");

        if (rightBtn != null) rightBtn.clicked += OnRightButtonClicked;
        else Debug.LogWarning($"PlaneSelection: Could not find right button '{rightButtonName}'.");

        if (purchaseBtn != null) purchaseBtn.clicked += OnPurchaseButtonClicked;
        else Debug.LogWarning($"PlaneSelection: Could not find purchase button '{purchaseButtonName}'.");

        if (purchaseFeedbackTxt != null)
        {
            purchaseFeedbackTxt.style.display = DisplayStyle.None;
            purchaseFeedbackTxt.style.opacity = 0f;
        }
        else
        {
            Debug.LogWarning($"PlaneSelection: Could not find feedback label '{purchaseFeedbackLabelName}'.");
        }

        HookEconomyEventIfPossible();
    }

    private void Start()
    {
        // Rebind again in case singleton init order caused nulls during Awake
        TryRebindManagers();
        HookEconomyEventIfPossible();

        // Delay one tick so UXML is fully ready
        root.schedule.Execute(() =>
        {
            // Rebind AGAIN here because this callback runs after current frame
            TryRebindManagers();
            HookEconomyEventIfPossible();

            if (planeManager == null)
            {
                Debug.LogError("PlaneSelection: PlaneManager singleton not found.");
                return;
            }

            if (planeManager.planes == null || planeManager.planes.Length == 0)
            {
                Debug.LogError("PlaneSelection: PlaneManager has no planes configured.");
                return;
            }

            CachePlaneGroupsAndPriceElements();

            int startIndex = 0;
            if (modeManager != null)
                startIndex = Mathf.Clamp(modeManager.SelectedPlaneIndex, 0, planeManager.planes.Length - 1);

            SetIndex(startIndex, true);
            UpdateCoinText();
        }).StartingIn(0);
    }

    private void OnDestroy()
    {
        if (leftBtn != null) leftBtn.clicked -= OnLeftButtonClicked;
        if (rightBtn != null) rightBtn.clicked -= OnRightButtonClicked;
        if (purchaseBtn != null) purchaseBtn.clicked -= OnPurchaseButtonClicked;

        if (economySubscribed && planeManager != null)
        {
            planeManager.OnEconomyChanged -= OnEconomyChanged;
            economySubscribed = false;
        }
    }

    private void Update()
    {
        if (currentPreview != null)
            currentPreview.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void TryRebindManagers()
    {
        // Only use the preexisting singletons (what you asked for)
        if (planeManager == null) planeManager = PlaneManager.Instance;
        if (modeManager == null) modeManager = ModeManager.Instance;
    }

    private void HookEconomyEventIfPossible()
    {
        if (economySubscribed) return;
        if (planeManager == null) return;

        planeManager.OnEconomyChanged += OnEconomyChanged;
        economySubscribed = true;
    }

    private void OnEconomyChanged()
    {
        UpdateCoinText();
        RefreshPurchaseUI();
    }

    private void CachePlaneGroupsAndPriceElements()
    {
        planeGroups.Clear();
        priceElements.Clear();

        if (planeManager == null || planeManager.planes == null)
            return;

        VisualElement container = null;
        if (!string.IsNullOrEmpty(namesContainerUxmlName))
            container = root.Q<VisualElement>(namesContainerUxmlName);

        for (int i = 0; i < planeManager.planes.Length; i++)
        {
            string uxmlName = planeManager.planes[i].uiNameElement;
            if (string.IsNullOrEmpty(uxmlName))
                continue;

            // Search inside container first (preferred), then fallback to whole document
            VisualElement group =
                (container != null ? container.Q<VisualElement>(uxmlName) : null)
                ?? root.Q<VisualElement>(uxmlName);

            if (group == null)
            {
                Debug.LogWarning($"PlaneSelection: Could not find UXML element named '{uxmlName}'.");
                continue;
            }

            // Hide all plane groups initially
            group.style.display = DisplayStyle.None;

            // Prevent the decorative plane UI group from blocking button clicks
            group.pickingMode = PickingMode.Ignore;

            planeGroups[i] = group;

            // Optional price child (Label/Image/VisualElement etc) inside the plane group
            VisualElement priceVE = group.Q<VisualElement>(priceElementChildName);
            if (priceVE != null)
            {
                priceElements[i] = priceVE;
            }
            else
            {
                Debug.LogWarning($"PlaneSelection: Price element '{priceElementChildName}' not found inside '{uxmlName}'.");
            }
        }
    }

    private void ShowPlaneGroupForIndex(int index)
    {
        foreach (var kv in planeGroups)
            kv.Value.style.display = DisplayStyle.None;

        if (planeGroups.TryGetValue(index, out var group))
            group.style.display = DisplayStyle.Flex;
    }

    private void SetIndex(int newIndex, bool force = false)
    {
        TryRebindManagers();

        if (planeManager == null || planeManager.planes == null || planeManager.planes.Length == 0)
        {
            Debug.LogError("PlaneSelection: PlaneManager has no planes configured.");
            return;
        }

        if (!force && newIndex == currentIndex) return;

        int count = planeManager.planes.Length;
        currentIndex = ((newIndex % count) + count) % count;

        SpawnPreview(currentIndex);
        ShowPlaneGroupForIndex(currentIndex);
        RefreshPurchaseUI();

        // Keep selected plane index synced
        if (modeManager != null)
            modeManager.SetSelectedPlane(currentIndex);
    }

    private void SpawnPreview(int index)
    {
        if (currentPreview != null)
            Destroy(currentPreview);

        if (planeManager == null || !planeManager.IsValidPlaneIndex(index))
        {
            Debug.LogError("PlaneSelection: Invalid plane index.");
            return;
        }

        GameObject prefab = planeManager.planes[index].planePrefab;
        if (prefab == null)
        {
            Debug.LogError($"PlaneSelection: Missing plane prefab for index {index}.");
            return;
        }

        // IMPORTANT: don't break the UI if preview anchor is missing
        if (previewAnchor == null)
        {
            Debug.LogWarning("PlaneSelection: previewAnchor is not assigned. Skipping preview spawn.");
            return;
        }

        currentPreview = Instantiate(prefab, previewAnchor);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
    }

    private void RefreshPurchaseUI()
    {
        if (planeManager == null || planeManager.planes == null || planeManager.planes.Length == 0)
            return;

        bool purchased = planeManager.IsPlanePurchased(currentIndex);

        // Purchase button hidden if plane already owned (including first/free plane)
        if (purchaseBtn != null)
            purchaseBtn.style.display = purchased ? DisplayStyle.None : DisplayStyle.Flex;

        // Show only the current plane's price element if not purchased
        foreach (var kv in priceElements)
        {
            bool isCurrent = kv.Key == currentIndex;
            bool shouldShow = isCurrent && !purchased;

            kv.Value.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Lock play button if current plane is not purchased
        if (playBtn != null)
            playBtn.SetEnabled(purchased);

        UpdateCoinText();
    }

    private void UpdateCoinText()
    {
        if (coinTxt == null || planeManager == null) return;

        // If your coin image already says "COINS", just display the number
        coinTxt.text = planeManager.Coins.ToString();
    }

    private void OnPurchaseButtonClicked()
    {
        TryRebindManagers();

        if (planeManager == null) return;

        // Shouldn't happen if button is hidden, but safe check
        if (planeManager.IsPlanePurchased(currentIndex))
        {
            ShowPurchaseFeedback("ALREADY PURCHASED", new Color(0.95f, 0.95f, 0.95f));
            RefreshPurchaseUI();
            return;
        }

        int cost = planeManager.GetPlanePrice(currentIndex);
        bool purchased = planeManager.TryPurchasePlane(currentIndex);

        if (!purchased)
        {
            ShowPurchaseFeedback("NOT ENOUGH COINS!!", new Color(1f, 0.35f, 0.35f));
            Debug.Log($"❌ Purchase failed. Need {cost}, have {planeManager.Coins}");
            return;
        }

        ShowPurchaseFeedback("PURCHASED!", new Color(1f, 0.9f, 0.35f)); // gold-ish
        Debug.Log($"✅ Purchased plane index {currentIndex}");
        RefreshPurchaseUI();
    }

    private void OnLeftButtonClicked()
    {
        if (planeManager == null || planeManager.planes == null || planeManager.planes.Length == 0) return;

        int next = (currentIndex - 1 + planeManager.planes.Length) % planeManager.planes.Length;
        SetIndex(next);
    }

    private void OnRightButtonClicked()
    {
        if (planeManager == null || planeManager.planes == null || planeManager.planes.Length == 0) return;

        int next = (currentIndex + 1) % planeManager.planes.Length;
        SetIndex(next);
    }

    private void ShowPurchaseFeedback(string message, Color color)
    {
        if (purchaseFeedbackTxt == null) return;

        purchaseFeedbackTxt.text = message;
        purchaseFeedbackTxt.style.color = new StyleColor(color);
        purchaseFeedbackTxt.style.display = DisplayStyle.Flex;
        purchaseFeedbackTxt.style.opacity = 1f;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(FadePurchaseFeedback());
    }

    private IEnumerator FadePurchaseFeedback()
    {
        if (purchaseFeedbackTxt == null) yield break;

        float holdTime = 0.55f;
        float t = 0f;

        // Hold
        while (t < holdTime)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // Fade
        t = 0f;
        while (t < feedbackFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, t / feedbackFadeDuration);
            purchaseFeedbackTxt.style.opacity = alpha;
            yield return null;
        }

        purchaseFeedbackTxt.style.opacity = 0f;
        purchaseFeedbackTxt.style.display = DisplayStyle.None;
        feedbackRoutine = null;
    }
}