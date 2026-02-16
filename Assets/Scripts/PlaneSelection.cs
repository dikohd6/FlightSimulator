using UnityEngine;
using UnityEngine.UIElements;

public class PlaneSelection : MonoBehaviour
{
    [SerializeField] private PlaneManager planeManager;
    [SerializeField] private ModeManager modeManager;
    [SerializeField] private UIDocument gameMenuDocument;

    [Header("Preview")]
    [SerializeField] private Transform previewAnchor;
    [SerializeField] private float spinSpeed = 30f;

    private VisualElement gameMenuOverlay;
    private Button leftBtn;
    private Button rightBtn;

    private int currentIndex = 0;
    private GameObject currentPreview;

    private void Awake()
    {
        if (planeManager == null)
            planeManager = PlaneManager.Instance != null ? PlaneManager.Instance : FindFirstObjectByType<PlaneManager>();

        if (modeManager == null)
            modeManager = ModeManager.Instance != null ? ModeManager.Instance : FindFirstObjectByType<ModeManager>();

        var root = gameMenuDocument.rootVisualElement;
        gameMenuOverlay = root.Q<VisualElement>("missionSelect");

        leftBtn = gameMenuOverlay.Q<Button>("leftBtn");
        rightBtn = gameMenuOverlay.Q<Button>("rightBtn");

        leftBtn.clicked += OnLeftButtonClicked;
        rightBtn.clicked += OnRightButtonClicked;

        SpawnPreview(currentIndex);
        UpdateModeManager();
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

    private void SpawnPreview(int index)
    {
        if (currentPreview) Destroy(currentPreview);

        var prefab = planeManager.planes[index].planePrefab;
        if (!prefab) { Debug.LogError("Missing plane prefab on PlaneManager."); return; }

        currentPreview = Instantiate(prefab, previewAnchor);
        currentPreview.transform.localPosition = Vector3.zero;
        currentPreview.transform.localRotation = Quaternion.identity;
    }

    private void OnLeftButtonClicked()
    {
        currentIndex = (currentIndex - 1 + planeManager.planes.Length) % planeManager.planes.Length;
        SpawnPreview(currentIndex);
        UpdateModeManager();
    }

    private void OnRightButtonClicked()
    {
        currentIndex = (currentIndex + 1) % planeManager.planes.Length;
        SpawnPreview(currentIndex);
        UpdateModeManager();
    }

    private void UpdateModeManager()
    {
        if (modeManager) modeManager.SetSelectedPlane(currentIndex);
    }
}
