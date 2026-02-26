using UnityEngine;

public static class GameAudioSettings
{
    private const string MusicVolumeKey = "music_volume";

    // Default volume when player opens game first time
    private const float DefaultMusicVolume = 0.75f;

    public static float MusicVolume
    {
        get => Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume));
        set
        {
            PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static void ResetAudioSettings()
    {
        PlayerPrefs.DeleteKey(MusicVolumeKey);
        PlayerPrefs.Save();
    }
}