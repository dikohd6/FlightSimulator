using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlaneEngineSound : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private Rigidbody planeRb;

    [Header("Speed -> Sound")]
    [SerializeField] private bool useKmh = true;
    [SerializeField] private float speedForMaxEffect = 300f;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float idleVolume = 0.45f;
    [Range(0f, 1f)][SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private float engineBoost = 1.2f;

    [Header("Pitch")]
    [SerializeField] private float idlePitch = 0.9f;
    [SerializeField] private float maxPitch = 1.4f;

    [Header("Smoothing")]
    [SerializeField] private float volumeLerpSpeed = 10f;
    [SerializeField] private float pitchLerpSpeed = 10f;

    [Header("Audio Space")]
    [Tooltip("Turn ON to test. If you can hear it immediately in 2D, your issue is 3D distance/listener.")]
    [SerializeField] private bool force2D = false;

    [SerializeField] private bool force3DSettings = true;
    [SerializeField] private float minDistance = 15f;
    [SerializeField] private float maxDistance = 800f;

    [Header("Reliability")]
    [Tooltip("How often we re-check and restart the audio if it stops (seconds).")]
    [SerializeField] private float watchdogInterval = 0.35f;

    [Tooltip("Print logs if the clip is unloaded/failed/stops.")]
    [SerializeField] private bool debugLogs = false;

    public const string SfxVolumeKey = "sfx_volume";
    private float sfxVolumeMultiplier = 1f;

    private bool pausedByMenu = false;
    private bool wasPlayingBeforePause = false;

    private Coroutine startRoutine;
    private float watchdogTimer;

    private void Awake()
    {
        if (engineSource == null) engineSource = GetComponent<AudioSource>();
        if (planeRb == null) planeRb = GetComponent<Rigidbody>();
        if (planeRb == null) planeRb = GetComponentInParent<Rigidbody>();

        sfxVolumeMultiplier = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));

        if (engineSource == null)
        {
            Debug.LogError($"PlaneEngineSound ({name}): Missing AudioSource.");
            enabled = false;
            return;
        }

        engineSource.enabled = true;
        engineSource.loop = true;
        engineSource.playOnAwake = false;
        engineSource.mute = false;

        if (force2D)
        {
            engineSource.spatialBlend = 0f;
        }
        else if (force3DSettings)
        {
            engineSource.spatialBlend = 1f;
            engineSource.dopplerLevel = 0f;
            engineSource.rolloffMode = AudioRolloffMode.Linear;
            engineSource.minDistance = minDistance;
            engineSource.maxDistance = maxDistance;
        }

        // audible idle immediately
        engineSource.volume = Mathf.Clamp01(idleVolume * sfxVolumeMultiplier * engineBoost);
        engineSource.pitch = idlePitch;
    }

    private void OnEnable()
    {
        StartOrRestartIfNeeded();
    }

    private void Start()
    {
        StartOrRestartIfNeeded();
    }

    private void OnDisable()
    {
        if (startRoutine != null)
        {
            StopCoroutine(startRoutine);
            startRoutine = null;
        }
    }

    private void StartOrRestartIfNeeded()
    {
        if (pausedByMenu) return;
        if (engineSource == null) return;

        // If no clip assigned, we can’t play
        if (engineSource.clip == null)
        {
            if (debugLogs) Debug.LogWarning($"PlaneEngineSound ({name}): AudioSource has no clip assigned.");
            return;
        }

        // If clip isn’t loaded yet (Streaming / load-in-background), ensure it loads then play
        if (engineSource.clip.loadState == AudioDataLoadState.Unloaded ||
            engineSource.clip.loadState == AudioDataLoadState.Loading)
        {
            if (startRoutine == null)
                startRoutine = StartCoroutine(StartWhenReady());
            return;
        }

        // Loaded: just play
        if (!engineSource.isPlaying)
        {
            engineSource.Play();
            if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Started playing.");
        }
    }

    private IEnumerator StartWhenReady()
    {
        if (engineSource == null || engineSource.clip == null)
        {
            startRoutine = null;
            yield break;
        }

        // If unloaded, request load
        if (engineSource.clip.loadState == AudioDataLoadState.Unloaded)
        {
            engineSource.clip.LoadAudioData();
            if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Loading audio data...");
        }

        // Wait while loading
        while (engineSource != null &&
               engineSource.clip != null &&
               engineSource.clip.loadState == AudioDataLoadState.Loading)
        {
            yield return null;
        }

        if (engineSource == null || engineSource.clip == null)
        {
            startRoutine = null;
            yield break;
        }

        if (engineSource.clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError($"PlaneEngineSound ({name}): AudioClip failed to load. Check import settings.");
            startRoutine = null;
            yield break;
        }

        // Play once ready
        if (!pausedByMenu && !engineSource.isPlaying)
        {
            engineSource.Play();
            if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Started after load.");
        }

        startRoutine = null;
    }

    private void Update()
    {
        if (engineSource == null || planeRb == null) return;
        if (pausedByMenu) return;

        // Watchdog: if the audio stops for any reason, restart it
        watchdogTimer += Time.unscaledDeltaTime;
        if (watchdogTimer >= watchdogInterval)
        {
            watchdogTimer = 0f;

            if (engineSource.clip != null)
            {
                // If Unity unloaded the clip mid-game, load it again and restart
                if (engineSource.clip.loadState == AudioDataLoadState.Unloaded ||
                    engineSource.clip.loadState == AudioDataLoadState.Loading)
                {
                    if (debugLogs) Debug.LogWarning($"PlaneEngineSound ({name}): Clip not ready (state={engineSource.clip.loadState}). Restarting loader.");
                    if (startRoutine == null)
                        startRoutine = StartCoroutine(StartWhenReady());
                }
                else if (engineSource.clip.loadState == AudioDataLoadState.Loaded && !engineSource.isPlaying)
                {
                    if (debugLogs) Debug.LogWarning($"PlaneEngineSound ({name}): Audio stopped unexpectedly -> restarting.");
                    engineSource.Play();
                }
            }
        }

        // Update volume/pitch based on speed (even at idle)
        float speed = planeRb.linearVelocity.magnitude;
        if (useKmh) speed *= 3.6f;

        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForMaxEffect));

        float targetVol = Mathf.Lerp(idleVolume, maxVolume, t) * sfxVolumeMultiplier * engineBoost;
        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, t);

        float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        engineSource.volume = Mathf.Lerp(engineSource.volume, Mathf.Clamp01(targetVol), volumeLerpSpeed * dt);
        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, pitchLerpSpeed * dt);
    }

    // Called by PauseMenu when SFX slider changes
    public void SetSfxVolume(float value)
    {
        sfxVolumeMultiplier = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolumeMultiplier);
        PlayerPrefs.Save();

        if (debugLogs && sfxVolumeMultiplier <= 0.001f)
            Debug.LogWarning($"PlaneEngineSound ({name}): SFX volume is 0 (muted).");
    }

    // Called by PauseMenu when pausing/resuming
    public void SetPausedByMenu(bool paused)
    {
        if (engineSource == null) return;

        if (paused)
        {
            if (pausedByMenu) return;
            pausedByMenu = true;

            wasPlayingBeforePause = engineSource.isPlaying;
            if (wasPlayingBeforePause) engineSource.Pause();
        }
        else
        {
            if (!pausedByMenu) return;
            pausedByMenu = false;

            if (wasPlayingBeforePause && engineSource.clip != null) engineSource.UnPause();
            wasPlayingBeforePause = false;

            // Ensure it resumes immediately
            StartOrRestartIfNeeded();
        }
    }

    private void OnValidate()
    {
        speedForMaxEffect = Mathf.Max(0.01f, speedForMaxEffect);
        if (maxVolume < idleVolume) maxVolume = idleVolume;
        if (maxPitch < idlePitch) maxPitch = idlePitch;
        volumeLerpSpeed = Mathf.Max(0.01f, volumeLerpSpeed);
        pitchLerpSpeed = Mathf.Max(0.01f, pitchLerpSpeed);
        minDistance = Mathf.Max(0.01f, minDistance);
        maxDistance = Mathf.Max(minDistance + 0.01f, maxDistance);
        watchdogInterval = Mathf.Clamp(watchdogInterval, 0.05f, 2f);
    }
}