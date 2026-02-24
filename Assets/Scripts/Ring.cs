using UnityEngine;
using WrightAngle.Waypoint;

[RequireComponent(typeof(Collider))]
public class Ring : MonoBehaviour
{
    private RingManager manager;
    private Renderer rend;
    private WaypointTarget waypoint;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        waypoint = GetComponent<WaypointTarget>();

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning($"{name}: Collider was not set as Trigger. Setting it to Trigger automatically.");
            col.isTrigger = true;
        }
    }

    public void Initialize(RingManager ringManager)
    {
        manager = ringManager;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handles child colliders hitting the trigger
        bool isPlayer =
            other.CompareTag("Player") ||
            (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag("Player")) ||
            other.GetComponentInParent<PlaneController>() != null;

        Debug.Log($"[Ring Trigger] ring={name}, hit={other.name}, tag={other.tag}, isPlayer={isPlayer}");

        if (!isPlayer) return;

        if (manager == null)
        {
            Debug.LogError($"{name}: RingManager reference is missing.");
            return;
        }

        manager.EnterRing(this);
    }

    public void MarkEntered()
    {
        // Hide waypoint marker without disabling component
        if (waypoint != null)
        {
            waypoint.DeactivateWaypoint();
        }

        // Hide ring object entirely
        gameObject.SetActive(false);
    }

    public void SetHighlight(bool isActive)
    {
        if (rend != null)
        {
            rend.material.color = isActive ? Color.red : Color.white;
        }

        if (waypoint != null)
        {
            if (isActive)
                waypoint.ActivateWaypoint();
            else
                waypoint.DeactivateWaypoint();
        }

        Debug.Log($"{name} SetHighlight({isActive})");
    }
}