using Unity.Cinemachine;
using UnityEngine;

public class PlaneActivator : MonoBehaviour
{
    [SerializeField] private Transform planeParent;
    [SerializeField] private PlaneController planeController;

    private ModeManager modeManager;
    private PlaneManager planeManager;

    void Start()
    {
        modeManager = FindFirstObjectByType<ModeManager>();
        planeManager = FindFirstObjectByType<PlaneManager>();

        Debug.Log($"🔍 ModeManager found: {modeManager != null}");
        Debug.Log($"🔍 PlaneManager found: {planeManager != null}");

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
        Debug.Log($"🔍 Selected plane index: {selectedIndex}");

        ActivateScenePlane(selectedIndex);
        ApplyPlaneStats(selectedIndex);
        ApplyCameraSettings(selectedIndex);
    }

    private void ActivateScenePlane(int selectedIndex)
    {
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

        planeController.maxSpeed = planeData.speed;
        planeController.maxThrustAccel = planeData.acceleration;
        planeController.pitchTorque = planeData.rotation;
        planeController.rollTorque = planeData.rotation * 0.75f;
        planeController.yawTorque = planeData.rotation * 0.5f;

        Debug.Log($"📊 Applied stats: Speed={planeData.speed}, Accel={planeData.acceleration}");
    }

    private void ApplyCameraSettings(int selectedIndex)
    {
        Debug.Log("🔍 Starting ApplyCameraSettings...");

        if (planeManager == null || planeManager.planes == null)
        {
            Debug.LogWarning("PlaneActivator: PlaneManager or planes is null");
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= planeManager.planes.Length)
        {
            Debug.LogWarning($"PlaneActivator: Invalid plane index {selectedIndex}");
            return;
        }

        PlaneManager.PlaneData planeData = planeManager.planes[selectedIndex];
        Debug.Log($"🔍 Plane data shoulder offset: {planeData.shoulderOffset}");

        // Find the camera GameObject
        GameObject aimCameraObject = GameObject.Find("Third Person Aim Camera");

        if (aimCameraObject == null)
        {
            Debug.LogError("PlaneActivator: Could not find 'Third Person Aim Camera' GameObject");
            return;
        }

        Debug.Log($"🔍 Found camera GameObject: {aimCameraObject.name}");

        // Cinemachine 3.0 uses CinemachineCamera instead of CinemachineVirtualCamera
        CinemachineCamera cmCamera = aimCameraObject.GetComponent<CinemachineCamera>();

        if (cmCamera == null)
        {
            Debug.LogError($"PlaneActivator: No CinemachineCamera found on {aimCameraObject.name}");
            return;
        }

        Debug.Log($"🔍 Found CinemachineCamera component");

        // Get the CinemachineThirdPersonFollow component
        var thirdPersonFollow = cmCamera.GetComponent<CinemachineThirdPersonFollow>();

        if (thirdPersonFollow != null)
        {
            Debug.Log($"🔍 Old shoulder offset: {thirdPersonFollow.ShoulderOffset}");
            thirdPersonFollow.ShoulderOffset = planeData.shoulderOffset;
            Debug.Log($"✅ NEW shoulder offset: {thirdPersonFollow.ShoulderOffset}");
        }
        else
        {
            Debug.LogError("PlaneActivator: CinemachineThirdPersonFollow component not found!");
        }
    }
}