using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirSpawnGrace : MonoBehaviour
{
    [Header("Grace")]
    [SerializeField] private float countdownSeconds = 3f; // 3..2..1

    [Header("Start Motion")]
    [SerializeField] private float initialForwardSpeed = 35f;
    [SerializeField] private bool setInitialSpeed = true;

    private Rigidbody rb;
    private FuelModeAddon fuelAddon;
    private HUDController hud;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        fuelAddon = GetComponent<FuelModeAddon>();
        hud = FindFirstObjectByType<HUDController>();
    }

    private void Start()
    {
        StartCoroutine(GraceRoutine());
    }

    private IEnumerator GraceRoutine()
    {
        // Pause fuel during grace (only if fuel mode)
        if (fuelAddon != null) fuelAddon.SetFuelPaused(true);

        // Freeze physics
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;

        int t = Mathf.CeilToInt(countdownSeconds);
        while (t > 0)
        {
            // Show countdown using your existing HUD info text
            if (hud != null) hud.SetInfoOverride(t.ToString()); // <-- add this method (below)

            yield return new WaitForSecondsRealtime(1f);
            t--;
        }

        if (hud != null) hud.SetInfoOverride("GO!");
        yield return new WaitForSecondsRealtime(0.5f);

        // Clear override
        if (hud != null) hud.ClearInfoOverride();

        // Release physics
        rb.isKinematic = false;
        rb.useGravity = true;

        if (setInitialSpeed)
            rb.linearVelocity = transform.forward * initialForwardSpeed;

        // Resume fuel
        if (fuelAddon != null) fuelAddon.SetFuelPaused(false);
    }
}