using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
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

        var pauseRoot = pauseMenuDocument.rootVisualElement;

        optionsOverlay = pauseRoot.Q<VisualElement>("OptionsMenu");
        pauseOverlay = pauseRoot.Q<VisualElement>("PauseMenu");

        optionsBtn = pauseOverlay.Q<Button>("optionsBtn");
        mainMenuBtn = pauseOverlay.Q<Button>("mainMenuBtn");
        backBtn = optionsOverlay.Q<Button>("backBtn");
        volumeSlider = optionsOverlay.Q<Slider>("Slider");

        optionsBtn.clicked += OptionsBtn_clicked;
        mainMenuBtn.clicked += MainMenuBtn_clicked;
        backBtn.clicked += BackBtn_clicked;

        // Start hidden
        HideAll();
    }

    private void OnEnable()
    {
        input.Enable();

        // IMPORTANT: add Flight/Pause action bound to <Keyboard>/escape in your Input Actions asset
        input.Flight.Pause.performed += _ => TogglePause();
    }

    private void OnDisable()
    {
        if (input != null)
            input.Flight.Pause.performed -= _ => TogglePause(); // can't unsubscribe lambdas safely

        input?.Disable();

        // safety
        if (pauseTimeScale) Time.timeScale = 1f;
    }

    // Better unsubscribe-safe version:
    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => TogglePause();

    private void TogglePause()
    {
        if (!isPaused) Pause();
        else Resume();
    }

    public void Pause()
    {
        isPaused = true;

        // Show pause overlay, hide options
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
        
        SceneManager.LoadScene("MainMenuScene");
    }

    private void OptionsBtn_clicked()
    {
        optionsOverlay.style.display = DisplayStyle.Flex;
        pauseOverlay.style.display = DisplayStyle.None;
    }
}
