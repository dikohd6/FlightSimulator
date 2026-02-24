using UnityEngine;
using WrightAngle.Waypoint;

public class RingManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HUDController hud;
    [SerializeField] private Transform ringsParent;              // Parent containing all rings in order
    [SerializeField] private WaypointTarget runwayWaypoint;      // Drag the runway WaypointTarget directly here
    [SerializeField] private GameObject[] runwayColliders;       // Colliders to enable after all rings are collected

    private Ring[] rings;
    private int currentRingIndex = 0;
    private bool ringsCollected = false;

    public bool RingsCollected => ringsCollected;

    private void Start()
    {
        if (ringsParent == null)
        {
            Debug.LogError("RingManager: ringsParent is not assigned.");
            enabled = false;
            return;
        }

        // Includes inactive children too (safer)
        rings = ringsParent.GetComponentsInChildren<Ring>(true);

        if (rings == null || rings.Length == 0)
        {
            Debug.LogError("RingManager: No Ring components found under ringsParent.");
            enabled = false;
            return;
        }

        // Initialize each ring with this manager
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] != null)
            {
                rings[i].Initialize(this);
            }
        }

        // Ensure runway waypoint starts hidden (manual activation mode)
        if (runwayWaypoint != null)
        {
            runwayWaypoint.DeactivateWaypoint();
        }
        else
        {
            Debug.LogWarning("RingManager: runwayWaypoint is not assigned.");
        }

        // Start runway colliders disabled
        SetRunwayColliders(false);

        currentRingIndex = 0;
        ringsCollected = false;

        // Highlight / activate only the first ring waypoint
        RefreshRingStates();

        Debug.Log($"RingManager: Initialized with {rings.Length} rings.");
    }

    public void EnterRing(Ring enteredRing)
    {
        if (ringsCollected) return;
        if (enteredRing == null) return;

        if (currentRingIndex < 0 || currentRingIndex >= rings.Length)
        {
            Debug.LogWarning("RingManager: currentRingIndex out of range.");
            return;
        }

        Ring expectedRing = rings[currentRingIndex];

        if (enteredRing != expectedRing)
        {
            Debug.Log($"❌ Wrong ring. Entered: {enteredRing.name}, Expected: {expectedRing.name}");
            return;
        }

        // Correct ring
        enteredRing.MarkEntered();

        if (hud != null)
        {
            hud.AddScore(10);
        }

        currentRingIndex++;

        if (currentRingIndex >= rings.Length)
        {
            ringsCollected = true;
            ActivateRunway();
            Debug.Log("✅ All rings collected! Runway activated.");
            return;
        }

        RefreshRingStates();
    }

    private void RefreshRingStates()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null) continue;

            bool isCurrent = (i == currentRingIndex);
            rings[i].SetHighlight(isCurrent);
        }
    }

    private void ActivateRunway()
    {
        if (runwayWaypoint != null)
        {
            runwayWaypoint.ActivateWaypoint();
        }

        SetRunwayColliders(true);
    }

    private void SetRunwayColliders(bool active)
    {
        if (runwayColliders == null) return;

        foreach (GameObject col in runwayColliders)
        {
            if (col != null)
            {
                col.SetActive(active);
            }
        }
    }

    public bool GetRingsCollected()
    {
        return ringsCollected;
    }
}