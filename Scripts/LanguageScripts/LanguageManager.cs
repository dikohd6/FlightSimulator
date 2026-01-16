using UnityEngine;
using System;


public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }


    [Tooltip("Reference the LanguageDatabase ScriptableObject here")]
    public LanguageDatabase database;


    [Tooltip("Active language code, e.g. 'en'")]
    public string activeLanguage = "en";


    public event Action OnLanguageChanged;


    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
    }


    public void SetLanguage(string languageCode)
    {
        if (database == null) return;
        if (activeLanguage == languageCode) return;
        activeLanguage = languageCode;
        OnLanguageChanged?.Invoke();
    }


    public string Get(string key)
    {
        if (database == null) return "[No DB]";
        return database.GetValue(key, activeLanguage);
    }
}