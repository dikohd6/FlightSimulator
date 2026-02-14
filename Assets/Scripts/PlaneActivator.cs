using UnityEngine;

public class PlaneActivator : MonoBehaviour
{
    [SerializeField] private Transform planeParent; // Your scene's "Plane" GameObject
    [SerializeField] private PlaneController planeController;

    private ModeManager modeManager;
    private PlaneManager planeManager;

    void Start()
    {
        // REMOVE ANIMATORS FIRST (fixes the error)
        RemoveAnimators();

        // Then do your normal setup...
        modeManager = FindFirstObjectByType<ModeManager>();
        planeManager = FindFirstObjectByType<PlaneManager>();

        if (planeParent == null)
        {
            Debug.LogError("PlaneActivator: Plane parent not assigned!");
            return;
        }

        if (planeController == null)
        {
            Debug.LogError("PlaneActivator: PlaneController not found!");
            return;
        }

        int selectedIndex = modeManager != null ? modeManager.SelectedPlaneIndex : 0;
        ActivateScenePlane(selectedIndex);
        ApplyPlaneStats(selectedIndex);
    }

    // NEW METHOD - Add this
    private void RemoveAnimators()
    {
        if (planeParent == null) return;

        Animator[] animators = planeParent.GetComponentsInChildren<Animator>(true);
        foreach (Animator anim in animators)
        {
            Destroy(anim);
        }

        Debug.Log($"🧹 Removed {animators.Length} Animator components from planes");
    }

    private void ActivateScenePlane(int selectedIndex)
    {
        // Activate only the selected plane child in YOUR SCENE
        for (int i = 0; i < planeParent.childCount; i++)
        {
            Transform child = planeParent.GetChild(i);

            if (i == selectedIndex)
            {
                child.gameObject.SetActive(true);
                Debug.Log($"✅ Activated scene plane: {child.name} (index {i})");
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void ApplyPlaneStats(int selectedIndex)
    {
        if (planeManager == null || planeManager.planes == null)
        {
            Debug.LogWarning("PlaneActivator: PlaneManager not found. Using default stats.");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= planeManager.planes.Length)
        {
            Debug.LogWarning($"PlaneActivator: Invalid plane index {selectedIndex}");
            return;
        }

        PlaneManager.PlaneData planeData = planeManager.planes[selectedIndex];

        // LOG THE STATS BEFORE APPLYING
        Debug.Log($"🔍 PlaneData from menu - Speed: {planeData.speed}, Accel: {planeData.acceleration}, Rotation: {planeData.rotation}");

        // Apply to your scene's PlaneController
        planeController.maxSpeed = planeData.speed;
        planeController.maxThrustAccel = planeData.acceleration;
        planeController.pitchTorque = planeData.rotation;
        planeController.rollTorque = planeData.rotation * 0.75f;
        planeController.yawTorque = planeData.rotation * 0.5f;

        Debug.Log($"✅ Applied to PlaneController - MaxSpeed: {planeController.maxSpeed}, MaxThrustAccel: {planeController.maxThrustAccel}");
    }
}