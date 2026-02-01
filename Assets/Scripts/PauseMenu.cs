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
    private Slider volumeSlider;

    [Header("Pause")]
    [SerializeField] private bool pauseTimeScale = true;

    private bool isPaused;
    private PlaneInputActions input;

    private void Awake()
    {
        input = new PlaneInputActions();

        var root = pauseMenuDocument.rootVisualElement;

        optionsOverlay = root.Q<VisualElement>("OptionsMenu");
        pauseOverlay = root.Q<VisualElement>("PauseMenu");

        optionsBtn = pauseOverlay.Q<Button>("optionsBtn");
        mainMenuBtn = pauseOverlay.Q<Button>("mainMenuBtn");
        backBtn = optionsOverlay.Q<Button>("backBtn");
        volumeSlider = optionsOverlay.Q<Slider>("Slider");

        optionsBtn.clicked += OptionsBtn_clicked;
        mainMenuBtn.clicked += MainMenuBtn_clicked;
        backBtn.clicked += BackBtn_clicked;

        HideAll();
    }

    private void OnEnable()
    {
        input.Enable();

        // IMPORTANT: this requires a "Pause" action in the Flight action map
        input.Flight.Pause.performed += OnPausePerformed;
    }

    private void OnDisable()
    {
        if (input != null)
            input.Flight.Pause.performed -= OnPausePerformed;

        input?.Disable();

        // safety if object disables while paused
        if (pauseTimeScale) Time.timeScale = 1f;
        isPaused = false;
    }

    private void OnPausePerformed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    private void TogglePause()
    {
        if (!isPaused) Pause();
        else Resume();
    }

    public void Pause()
    {
        isPaused = true;

        // show pause menu
        pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        pauseOverlay.style.display = DisplayStyle.Flex;
        optionsOverlay.style.display = DisplayStyle.None;

        if (pauseTimeScale) Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;
        HideAll();

        if (pauseTimeScale) Time.timeScale = 1f;
    }

    private void HideAll()
    {
        if (optionsOverlay != null) optionsOverlay.style.display = DisplayStyle.None;
        if (pauseOverlay != null) pauseOverlay.style.display = DisplayStyle.None;

        if (pauseMenuDocument != null)
            pauseMenuDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void BackBtn_clicked()
    {
        optionsOverlay.style.display = DisplayStyle.None;
        pauseOverlay.style.display = DisplayStyle.Flex;
    }

    private void MainMenuBtn_clicked()
    {
        if (pauseTimeScale) Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OptionsBtn_clicked()
    {
        optionsOverlay.style.display = DisplayStyle.Flex;
        pauseOverlay.style.display = DisplayStyle.None;
    }
}
