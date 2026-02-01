using System.Collections;
using UnityEngine;

public class LandingCinematicController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LandingResultsUI resultsUI;
    [SerializeField] private OrbitCamera orbitCamera;         // the script that orbits
    [SerializeField] private Camera orbitCamComponent;        // the camera that renders the orbit
    [SerializeField] private Camera gameplayCamera;           // <-- assign your normal main camera here
    [SerializeField] private PlaneController plane;
    [SerializeField] private Rigidbody planeRb;
    [SerializeField] private HUDController hud;

    [Header("Stop Before UI")]
    [SerializeField] private float stopSpeedThreshold = 0.25f;
    [SerializeField] private float stoppedHoldSeconds = 0.6f;
    [SerializeField] private float maxWaitSeconds = 6f;

    [Header("Rollout Braking")]
    [SerializeField] private float rolloutLinearDamping = 6f;
    [SerializeField] private float rolloutAngularDamping = 6f;

    [Header("Freeze After Stop")]
    [SerializeField] private bool disablePlaneController = true;
    [SerializeField] private bool makeRigidbodyKinematic = true;

    private Coroutine playRoutine;

    void Awake()
    {
        // ---- Resolve references FIRST ----
        if (hud == null) hud = FindFirstObjectByType<HUDController>();
        if (plane == null) plane = FindFirstObjectByType<PlaneController>();
        if (planeRb == null && plane != null) planeRb = plane.GetComponent<Rigidbody>();

        if (orbitCamera == null) orbitCamera = FindFirstObjectByType<OrbitCamera>();
        if (resultsUI == null) resultsUI = FindFirstObjectByType<LandingResultsUI>();

        if (orbitCamComponent == null && orbitCamera != null)
            orbitCamComponent = orbitCamera.GetComponentInChildren<Camera>(true);

        // ---- Disable orbit at start (so it doesn't run during gameplay) ----
        if (orbitCamera != null) orbitCamera.enabled = false;
        if (orbitCamComponent != null) orbitCamComponent.enabled = false;

        // Optional: If you forgot to assign gameplayCamera, try to grab Camera.main
        if (gameplayCamera == null) gameplayCamera = Camera.main;
    }

    public void PlayEnding(LandingScoreData data)
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PlayEndingRoutine(data));
    }

    private IEnumerator PlayEndingRoutine(LandingScoreData data)
    {
        // Wait for full stop (rollout)
        if (planeRb != null)
        {
            float originalLin = planeRb.linearDamping;
            float originalAng = planeRb.angularDamping;

            planeRb.linearDamping = rolloutLinearDamping;
            planeRb.angularDamping = rolloutAngularDamping;

            float stoppedTimer = 0f;
            float timer = 0f;

            while (timer < maxWaitSeconds)
            {
                timer += Time.fixedDeltaTime;

                float speed = planeRb.linearVelocity.magnitude;

                if (speed <= stopSpeedThreshold)
                {
                    stoppedTimer += Time.fixedDeltaTime;
                    if (stoppedTimer >= stoppedHoldSeconds)
                        break;
                }
                else
                {
                    stoppedTimer = 0f;
                }

                yield return new WaitForFixedUpdate();
            }

            planeRb.linearDamping = originalLin;
            planeRb.angularDamping = originalAng;
        }

        FreezePlane();
        StartOrbitAndUI(data);
    }

    private void FreezePlane()
    {
        if (disablePlaneController && plane != null)
            plane.enabled = false;

        if (planeRb != null)
        {
            planeRb.linearVelocity = Vector3.zero;
            planeRb.angularVelocity = Vector3.zero;
            if (makeRigidbodyKinematic) planeRb.isKinematic = true;
        }
    }

    private void StartOrbitAndUI(LandingScoreData data)
    {
        hud?.StopTimer();
        // Make sure orbit camera is the one you SEE
        if (gameplayCamera != null) gameplayCamera.enabled = false;

        if (orbitCamera != null) orbitCamera.enabled = true;
        if (orbitCamComponent != null) orbitCamComponent.enabled = true;

        if (orbitCamera != null && plane != null)
            orbitCamera.SetTarget(plane.transform);

        resultsUI?.PlaySequence(data);
    }
}
