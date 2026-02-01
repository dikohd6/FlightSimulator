using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float orbitSpeedDegPerSec = 25f;
    [SerializeField] private float height = 3f;
    [SerializeField] private float radius = 8f;
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Active Only During Ending")]
    [SerializeField] private bool active = false;

    private float angle;

    public void SetTarget(Transform t)
    {
        target = t;
        // Initialize angle so it doesn't snap weirdly
        if (target != null)
        {
            Vector3 flat = (transform.position - target.position);
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.001f)
                angle = Mathf.Atan2(flat.z, flat.x) * Mathf.Rad2Deg;
        }
    }

    public void SetActive(bool value) => active = value;

    void LateUpdate()
    {
        if (!active || target == null) return;

        angle += orbitSpeedDegPerSec * Time.unscaledDeltaTime;

        float rad = angle * Mathf.Deg2Rad;

        Vector3 center = target.position + Vector3.up * height;
        Vector3 offset = new Vector3(Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);

        transform.position = center + offset;
        transform.LookAt(target.position + lookOffset);

        Debug.Log("Orbit active: " + active + " target: " + (target ? target.name : "null"));

    }
}
