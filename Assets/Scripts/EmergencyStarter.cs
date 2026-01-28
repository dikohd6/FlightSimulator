using UnityEngine;

public class EmergencyStarter : MonoBehaviour
{
    [SerializeField] private EmergencyLandingMode emergency;

    void Start()
    {
        // Wait 10 seconds, then trigger the emergency
        Invoke(nameof(StartEmergency), 10f);
    }

    void StartEmergency()
    {
        if (emergency != null)
            emergency.ActivateEmergency();
        else
            Debug.LogWarning("EmergencyStarter: No EmergencyLandingMode assigned.");
    }
}
