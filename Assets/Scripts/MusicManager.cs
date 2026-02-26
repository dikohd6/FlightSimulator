using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuAndGameMusic;

    [Header("Defaults")]
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool loop = true;

    public const string MusicVolumeKey = "music_volume";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null) musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            Debug.LogError("MusicManager: Missing AudioSource.");
            enabled = false;
            return;
        }

        musicSource.enabled = true;
        musicSource.loop = loop;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D music
        musicSource.mute = false;
        musicSource.ignoreListenerPause = true; // if you ever pause SFX listener later

        if (menuAndGameMusic != null)
            musicSource.clip = menuAndGameMusic;

        ApplySavedVolume();

        if (playOnAwake)
            EnsurePlaying();
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolume();
        EnsurePlaying();
    }

    public void EnsurePlaying()
    {
        if (musicSource == null) return;

        if (!musicSource.enabled) musicSource.enabled = true;

        if (musicSource.clip == null && menuAndGameMusic != null)
            musicSource.clip = menuAndGameMusic;

        if (musicSource.clip != null && !musicSource.isPlaying)
            musicSource.Play();
    }

    public void SetVolume(float value)
    {
        value = Mathf.Clamp01(value);

        if (musicSource != null)
            musicSource.volume = value;

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
        PlayerPrefs.Save();
    }

    public float GetVolume()
    {
        if (musicSource != null) return musicSource.volume;
        return PlayerPrefs.GetFloat(MusicVolumeKey, defaultVolume);
    }

    public void ApplySavedVolume()
    {
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, defaultVolume));
        if (musicSource != null) musicSource.volume = saved;
    }
}