using UnityEngine;

public class EmergencyStarter : MonoBehaviour
{
    [SerializeField] private EmergencyLandingMode emergency;
    [SerializeField] private float triggerAfterSeconds = 10f;

    void Start()
    {
        Invoke(nameof(StartEmergency), triggerAfterSeconds);
    }

    void StartEmergency()
    {
        if (emergency != null)
            emergency.ActivateEmergency();
        else
            Debug.LogWarning("EmergencyStarter: No EmergencyLandingMode assigned.");
    }
}