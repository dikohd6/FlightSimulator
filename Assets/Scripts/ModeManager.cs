using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeManager : MonoBehaviour
{
    public enum ModeType { Standard, Emergency, Fuel }

    [SerializeField] private ModeType[] modes = { ModeType.Standard, ModeType.Emergency, ModeType.Fuel };
    [SerializeField] private int currentModeIndex = 0;
    [SerializeField] private int selectedPlaneIndex = 0; // NEW: Track selected plane

    private PlaneController plane;
    private EmergencyLandingMode emergency;

    public int CurrentModeIndex => currentModeIndex;
    public int ModeCount => modes.Length;
    public ModeType CurrentMode => modes[currentModeIndex];
    public int SelectedPlaneIndex => selectedPlaneIndex; // NEW

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // NEW: Called by PlaneSelection in main menu
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
        TryHookAndApply();
    }

    private void TryHookAndApply()
    {
        if (plane == null)
            plane = FindFirstObjectByType<PlaneController>();
        if (emergency == null)
            emergency = FindFirstObjectByType<EmergencyLandingMode>();

        if (plane == null) return;

        ApplyMode(modes[currentModeIndex]);
    }

    private void ApplyMode(ModeType m)
    {
        switch (m)
        {
            case ModeType.Standard:
                SetStandardMode();
                break;
            case ModeType.Emergency:
                SetEmergencyMode();
                break;
            case ModeType.Fuel:
                SetStandardMode();
                break;
        }
    }

    private void SetStandardMode()
    {
        plane.maxSpeedCap = 1f;
        plane.accelMultiplier = 1f;
        plane.speedBleed = 0f;
        plane.thrustMultiplier = 1f;
        if (emergency != null)
            emergency.enabled = false;
    }

    private void SetEmergencyMode()
    {
        plane.maxSpeedCap = 0.55f;
        plane.accelMultiplier = 0.4f;
        plane.speedBleed = 1.0f;
        plane.thrustMultiplier = 1f;

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