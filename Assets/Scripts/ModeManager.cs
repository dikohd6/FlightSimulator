using UnityEngine;
using UnityEngine.SceneManagement;

public class ModeManager : MonoBehaviour
{
    public enum ModeType { Standard, Emergency }

    [SerializeField] private ModeType[] modes = { ModeType.Standard, ModeType.Emergency };
    [SerializeField] private int currentModeIndex = 0;

    private PlaneController plane;                 // assigned later (game scene)
    private EmergencyLandingMode emergency;         // may be null if not in scene

    public int CurrentModeIndex => currentModeIndex;
    public int ModeCount => modes.Length;
    public ModeType CurrentMode => modes[currentModeIndex];

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Called by your menu selector
    public void SetModeByIndex(int index)
    {
        if (modes == null || modes.Length == 0) return;
        currentModeIndex = WrapIndex(index, modes.Length);

        // If we're already in gameplay and the plane exists, apply immediately.
        TryHookAndApply();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Scene changed -> try to find gameplay components and apply selected mode
        TryHookAndApply();
    }

    private void TryHookAndApply()
    {
        // Find these ONLY if they exist in the current scene
        if (plane == null)
            plane = FindFirstObjectByType<PlaneController>();

        if (emergency == null)
            emergency = FindFirstObjectByType<EmergencyLandingMode>();

        // Still in menu scene? Then do nothing.
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

        // Only enable emergency system if it exists in the scene
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
