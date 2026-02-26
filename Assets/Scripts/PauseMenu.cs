using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument pauseMenuDocument;

    private VisualElement optionsOverlay;
    private VisualElement pauseOverlay;

    private Button optionsBtn;
    private Button mainMenuBtn;
    private Button backBtn;

    // New: two sliders
    private Slider musicSlider;
    private Slider sfxSlider;

    [Header("Pause")]
    [SerializeField] private bool pauseTimeScale = true;

    private bool isPaused;
    private bool suppressCallbacks;
    private PlaneInputActions input;

    private const string MusicVolumeKey = MusicManager.MusicVolumeKey;
    private const string SfxVolumeKey = PlaneEngineSound.SfxVolumeKey;

    private void Awake()
    {
        input = new PlaneInputActions();

        if (pauseMenuDocument == null)
        {
            pauseMenuDocument = GetComponent<UIDocument>();
            if (pauseMenuDocument == null)
            {
                Debug.LogError("PauseMenu: UIDocument is missing.");
                enabled = false;
                return;
            }
        }

        var root = pauseMenuDocument.rootVisualElement;

        optionsOverlay = root.Q<VisualElement>("OptionsMenu");
        pauseOverlay = root.Q<VisualElement>("PauseMenu");

        optionsBtn = pauseOverlay?.Q<Button>("optionsBtn");
        mainMenuBtn = pauseOverlay?.Q<Button>("mainMenuBtn");
        backBtn = optionsOverlay?.Q<Button>("backBtn");

        if (optionsBtn != null) optionsBtn.clicked += OptionsBtn_clicked;
        if (mainMenuBtn != null) mainMenuBtn.clicked += MainMenuBtn_clicked;
        if (backBtn != null) backBtn.clicked += BackBtn_clicked;

        BindSliders(root);
        SetupSliders();

        HideAll();
    }

    private void OnEnable()
    {
        input.Enable();
        input.Flight.Pause.performed += OnPausePerformed;

        RegisterSliderCallbacks();
        RefreshSlidersFromSaved();
    }

    private void OnDisable()
    {
        input.Flight.Pause.performed -= OnPausePerformed;
        input.Disable();

        UnregisterSliderCallbacks();

        SetPlaneEnginePaused(false);
        if (pauseTimeScale) Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnDestroy()
    {
        if (optionsBtn != null) optionsBtn.clicked -= OptionsBtn_clicked;
        if (mainMenuBtn != null) mainMenuBtn.clicked -= MainMenuBtn_clicked;
        if (backBtn != null) backBtn.clicked -= BackBtn_clicked;
    }

    private void BindSliders(VisualElement root)
    {
        // Your screenshot shows #musicVolume and #sfxVolume containers
        var musicGroup = root.Q<VisualElement>("musicVolume");
        var sfxGroup = root.Q<VisualElement>("sfxVolume");

        musicSlider = musicGroup != null ? musicGroup.Q<Slider>() : null;
        sfxSlider = sfxGroup != null ? sfxGroup.Q<Slider>() : null;

        if (musicSlider == null)
            Debug.LogWarning("PauseMenu: Could not find Slider under #musicVolume.");

        if (sfxSlider == null)
            Debug.LogWarning("PauseMenu: Could not find Slider under #sfxVolume.");
    }

    private void SetupSliders()
    {
        // Both sliders are 0..1
        if (musicSlider != null) { musicSlider.lowValue = 0f; musicSlider.highValue = 1f; }
        if (sfxSlider != null) { sfxSlider.lowValue = 0f; sfxSlider.highValue = 1f; }
    }

    private void RegisterSliderCallbacks()
    {
        if (musicSlider != null)
        {
            musicSlider.UnregisterValueChangedCallback(OnMusicSliderChanged);
            musicSlider.RegisterValueChangedCallback(OnMusicSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);
            sfxSlider.RegisterValueChangedCallback(OnSfxSliderChanged);
        }
    }

    private void UnregisterSliderCallbacks()
    {
        if (musicSlider != null) musicSlider.UnregisterValueChangedCallback(OnMusicSliderChanged);
        if (sfxSlider != null) sfxSlider.UnregisterValueChangedCallback(OnSfxSliderChanged);
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx) => TogglePause();

    private void TogglePause()
    {
        if (!isPaused) Pause();
        else Resume();
    }

    public void Pause()
    {
        isPaused = true;

        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        pauseOverlay.style.display = DisplayStyle.Flex;
        optionsOverlay.style.display = DisplayStyle.None;

        RefreshSlidersFromSaved();

        SetPlaneEnginePaused(true);
        if (pauseTimeScale) Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        SetPlaneEnginePaused(false);
        HideAll();

        if (pauseTimeScale) Time.timeScale = 1f;
    }

    private void HideAll()
    {
        optionsOverlay.style.display = DisplayStyle.None;
        pauseOverlay.style.display = DisplayStyle.None;
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void BackBtn_clicked()
    {
        optionsOverlay.style.display = DisplayStyle.None;
        pauseOverlay.style.display = DisplayStyle.Flex;
    }

    private void MainMenuBtn_clicked()
    {
        SetPlaneEnginePaused(false);

        if (pauseTimeScale) Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OptionsBtn_clicked()
    {
        optionsOverlay.style.display = DisplayStyle.Flex;
        pauseOverlay.style.display = DisplayStyle.None;
        RefreshSlidersFromSaved();
    }

    // --------- Slider sync ---------

    private void RefreshSlidersFromSaved()
    {
        suppressCallbacks = true;

        if (musicSlider != null)
        {
            float music01 = (MusicManager.Instance != null)
                ? MusicManager.Instance.GetVolume()
                : PlayerPrefs.GetFloat(MusicVolumeKey, 1f);

            musicSlider.SetValueWithoutNotify(Mathf.Clamp01(music01));
        }

        if (sfxSlider != null)
        {
            float sfx01 = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            sfxSlider.SetValueWithoutNotify(Mathf.Clamp01(sfx01));
        }

        suppressCallbacks = false;
    }

    private void OnMusicSliderChanged(ChangeEvent<float> evt)
    {
        if (suppressCallbacks) return;

        float v01 = Mathf.Clamp01(evt.newValue);

        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(v01);
        else
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, v01);
            PlayerPrefs.Save();
        }
    }

    private void OnSfxSliderChanged(ChangeEvent<float> evt)
    {
        if (suppressCallbacks) return;

        float v01 = Mathf.Clamp01(evt.newValue);

        PlayerPrefs.SetFloat(SfxVolumeKey, v01);
        PlayerPrefs.Save();

        // Apply immediately to any active plane engine sound(s)
        var engines = FindObjectsByType<PlaneEngineSound>(FindObjectsSortMode.None);
        foreach (var e in engines)
            e.SetSfxVolume(v01);
    }

    private void SetPlaneEnginePaused(bool paused)
    {
        var engines = FindObjectsByType<PlaneEngineSound>(FindObjectsSortMode.None);
        foreach (var e in engines)
            e.SetPausedByMenu(paused);
    }
}