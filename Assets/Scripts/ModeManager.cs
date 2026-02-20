using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeManager : MonoBehaviour
{
    public enum ModeType { Standard, Emergency, Fuel }

    [SerializeField] private ModeType[] modes = { ModeType.Standard, ModeType.Emergency, ModeType.Fuel };
    [SerializeField] private int currentModeIndex = 0;
    [SerializeField] private int selectedPlaneIndex = 0;

    private PlaneController plane;
    private EmergencyLandingMode emergency;

    public int CurrentModeIndex => currentModeIndex;
    public int ModeCount => modes.Length;
    public ModeType CurrentMode => modes[currentModeIndex];
    public int SelectedPlaneIndex => selectedPlaneIndex;

    public static ModeManager Instance { get; private set; }

    private void Awake()
    {
        // Kill duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Only the real instance should clean up + clear Instance
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void SetSelectedPlane(int index)
    {
        selectedPlaneIndex = index;
        Debug.Log($"ModeManager: Selected plane index {index}");
    }

    public void SetModeByIndex(int index)
    {
        if (modes == null || modes.Length == 0) return;
        currentModeIndex = WrapIndex(index, modes.Length);
        TryHookAndApply();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Optional but helps ensure we hook the new scene’s components
        plane = null;
        emergency = null;

        TryHookAndApply();
    }

    private void TryHookAndApply()
    {
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null) emergency = FindFirstObjectByType<EmergencyLandingMode>();
        if (plane == null) return;

        ApplyMode(modes[currentModeIndex]);
    }

    private void ApplyMode(ModeType m)
    {
        switch (m)
        {
            case ModeType.Standard: SetStandardMode(); break;
            case ModeType.Emergency: SetEmergencyMode(); break;
            case ModeType.Fuel: SetStandardMode(); break;
        }
    }

    private void SetStandardMode()
    {
        plane.maxSpeedCap = 1f;
        plane.accelMultiplier = 1f;
        plane.speedBleed = 0f;
        plane.thrustMultiplier = 1f;
        if (emergency != null) emergency.enabled = false;
    }

    private void SetEmergencyMode()
    {

        if (emergency != null)
        {
            emergency.enabled = true;
            emergency.ActivateEmergency();
        }
    }

    private int WrapIndex(int i, int count)
    {
        i %= count;
        if (i < 0) i += count;
        return i;
    }
}
