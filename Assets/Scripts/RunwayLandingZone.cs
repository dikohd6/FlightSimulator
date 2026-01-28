using UnityEngine;

public class RunwayLandingZone : MonoBehaviour
{
    [SerializeField] private LandingJudge judge;

    void Awake()
    {
        if (judge == null) judge = FindFirstObjectByType<LandingJudge>();
    }

    void OnTriggerEnter(Collider other)
    {
        var plane = other.GetComponentInParent<PlaneController>();
        if (plane != null) judge?.NotifyEnteredRunway(plane);
    }

    void OnTriggerStay(Collider other)
    {
        var plane = other.GetComponentInParent<PlaneController>();
        if (plane != null) judge?.NotifyEnteredRunway(plane);
    }

    void OnTriggerExit(Collider other)
    {
        var plane = other.GetComponentInParent<PlaneController>();
        if (plane != null) judge?.NotifyExitedRunway(plane);
    }
}
