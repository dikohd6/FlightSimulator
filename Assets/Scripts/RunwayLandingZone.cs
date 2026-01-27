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
        if (judge == null) return;

        var plane = other.GetComponentInParent<PlaneController>();
        if (plane != null)
            judge.NotifyEnteredRunway(plane);
    }

    void OnTriggerExit(Collider other)
    {
        if (judge == null) return;

        var plane = other.GetComponentInParent<PlaneController>();
        if (plane != null)
            judge.NotifyExitedRunway(plane);
    }
}
