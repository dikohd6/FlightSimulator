using UnityEngine;

public class HelicopterRotorSpin : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Rotor Transforms")]
    [SerializeField] private Transform mainRotor;
    [SerializeField] private Transform tailRotor;

    [Header("Spin Axes (set to match your model)")]
    [SerializeField] private Axis mainAxis = Axis.Y;
    [SerializeField] private Axis tailAxis = Axis.X;

    [Header("Speeds (degrees per second)")]
    [SerializeField] private float mainRotorSpeed = 1800f;
    [SerializeField] private float tailRotorSpeed = 3600f;

    [Header("Only spin when helicopter is the selected plane")]
    [SerializeField] private bool requireSelectedIndexMatch = true;
    [SerializeField] private int helicopterPlaneIndex = 2; // set this to your helicopter index

    private ModeManager modeManager;

    private void Awake()
    {
        // ModeManager is optional — if you don't have one in-scene, it still works via activeInHierarchy.
        modeManager = FindFirstObjectByType<ModeManager>();
    }

    private void Update()
    {
        if (!IsHelicopterActive())
            return;

        float dt = Time.deltaTime;

        if (mainRotor != null)
            mainRotor.Rotate(GetAxis(mainAxis), mainRotorSpeed * dt, Space.Self);

        if (tailRotor != null)
            tailRotor.Rotate(GetAxis(tailAxis), tailRotorSpeed * dt, Space.Self);
    }

    private bool IsHelicopterActive()
    {
        // Must be enabled/active in hierarchy
        if (!gameObject.activeInHierarchy) return false;

        if (!requireSelectedIndexMatch) return true;

        // If ModeManager exists, check selection index
        if (modeManager != null)
            return modeManager.SelectedPlaneIndex == helicopterPlaneIndex;

        // If ModeManager doesn't exist, fallback to active state only
        return true;
    }

    private static Vector3 GetAxis(Axis axis)
    {
        return axis switch
        {
            Axis.X => Vector3.right,
            Axis.Y => Vector3.up,
            Axis.Z => Vector3.forward,
            _ => Vector3.up
        };
    }
}