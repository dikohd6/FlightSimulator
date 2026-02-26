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
    [SerializeField] private bool force2D = false;

    [SerializeField] private bool force3DSettings = true;
    [SerializeField] private float minDistance = 15f;
    [SerializeField] private float maxDistance = 800f;

    [Header("Reliability")]
    [Tooltip("If no AudioListener exists yet, wait until one appears (fixes 'starts late').")]
    [SerializeField] private bool waitForAudioListener = true;

    [Tooltip("How often we re-check and restart the audio if it stops (seconds).")]
    [SerializeField] private float watchdogInterval = 0.35f;

    [Tooltip("Turn on for debugging.")]
    [SerializeField] private bool debugLogs = false;

    public const string SfxVolumeKey = "sfx_volume";
    private float sfxVolumeMultiplier = 1f;

    private bool pausedByMenu = false;
    private bool wasPlayingBeforePause = false;

    private Coroutine startupRoutine;
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

        // IMPORTANT: do NOT let the clip stream in late if you want instant sound.
        // We'll also request LoadAudioData in code below.
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

        // audible idle immediately (even before speed updates)
        engineSource.volume = Mathf.Clamp01(idleVolume * sfxVolumeMultiplier * engineBoost);
        engineSource.pitch = idlePitch;

        // Preload clip if possible (prevents late start)
        if (engineSource.clip != null && engineSource.clip.loadState == AudioDataLoadState.Unloaded)
        {
            engineSource.clip.LoadAudioData();
            if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Requested LoadAudioData()");
        }
    }

    private void OnEnable()
    {
        StartStartupRoutine();
    }

    private void Start()
    {
        StartStartupRoutine();
    }

    private void OnDisable()
    {
        if (startupRoutine != null)
        {
            StopCoroutine(startupRoutine);
            startupRoutine = null;
        }
    }

    private void StartStartupRoutine()
    {
        if (startupRoutine != null) return;
        startupRoutine = StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        // 1) Wait for clip
        while (engineSource != null && engineSource.clip == null)
            yield return null;

        if (engineSource == null || engineSource.clip == null)
        {
            startupRoutine = null;
            yield break;
        }

        // 2) Ensure audio data is loaded (prevents late start)
        if (engineSource.clip.loadState == AudioDataLoadState.Unloaded)
            engineSource.clip.LoadAudioData();

        while (engineSource.clip.loadState == AudioDataLoadState.Loading)
            yield return null;

        if (engineSource.clip.loadState == AudioDataLoadState.Failed)
        {
            Debug.LogError($"PlaneEngineSound ({name}): Clip failed to load. Change AudioClip import settings.");
            startupRoutine = null;
            yield break;
        }

        // 3) Wait until an AudioListener exists (THIS is often why it "starts late")
        if (waitForAudioListener)
        {
            while (FindAnyObjectByType<AudioListener>() == null)
            {
                if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Waiting for AudioListener...");
                yield return null;
            }
        }

        // 4) Play immediately
        if (!pausedByMenu && engineSource != null && !engineSource.isPlaying)
        {
            engineSource.Play();
            if (debugLogs) Debug.Log($"PlaneEngineSound ({name}): Started playing.");
        }

        startupRoutine = null;
    }

    private void Update()
    {
        if (engineSource == null || planeRb == null) return;
        if (pausedByMenu) return;

        // Watchdog: restart if stopped
        watchdogTimer += Time.unscaledDeltaTime;
        if (watchdogTimer >= watchdogInterval)
        {
            watchdogTimer = 0f;

            if (engineSource.clip != null)
            {
                // If no listener, sound will "seem late" / not audible. Keep trying.
                if (waitForAudioListener && FindAnyObjectByType<AudioListener>() == null)
                {
                    if (debugLogs) Debug.LogWarning($"PlaneEngineSound ({name}): No AudioListener found (yet).");
                }
                else if (!engineSource.isPlaying && engineSource.clip.loadState == AudioDataLoadState.Loaded)
                {
                    if (debugLogs) Debug.LogWarning($"PlaneEngineSound ({name}): Audio stopped -> restarting.");
                    engineSource.Play();
                }
            }
        }

        // Volume/pitch based on speed
        float speed = planeRb.linearVelocity.magnitude;
        if (useKmh) speed *= 3.6f;

        float t = Mathf.Clamp01(speed / Mathf.Max(0.01f, speedForMaxEffect));

        float targetVol = Mathf.Lerp(idleVolume, maxVolume, t) * sfxVolumeMultiplier * engineBoost;
        float targetPitch = Mathf.Lerp(idlePitch, maxPitch, t);

        float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
        engineSource.volume = Mathf.Lerp(engineSource.volume, Mathf.Clamp01(targetVol), volumeLerpSpeed * dt);
        engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, pitchLerpSpeed * dt);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolumeMultiplier = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolumeMultiplier);
        PlayerPrefs.Save();
    }

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

            if (wasPlayingBeforePause && engineSource.clip != null)
                engineSource.UnPause();

            wasPlayingBeforePause = false;

            // make sure it resumes instantly
            if (!engineSource.isPlaying && engineSource.clip != null && engineSource.clip.loadState == AudioDataLoadState.Loaded)
                engineSource.Play();
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