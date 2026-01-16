using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "LanguageDatabase", menuName = "Localization/Language Database")]
public class LanguageDatabase : ScriptableObject
{
    [System.Serializable]
    public class Language
    {
        public string languageCode; 
        public string displayName; 
        public List<string> values = new List<string>(); 
    }


    public List<string> keys = new List<string>();
    public List<Language> languages = new List<Language>();


    // Ensure every language's values list matches keys count
    public void EnsureConsistency()
    {
        foreach (var lang in languages)
        {
            while (lang.values.Count < keys.Count) lang.values.Add("");
            while (lang.values.Count > keys.Count) lang.values.RemoveAt(lang.values.Count - 1);
        }
    }


    public string GetValue(string key, string languageCode)
    {
        int idx = keys.IndexOf(key);
        if (idx < 0) return "[Missing Key: " + key + "]";
        var lang = languages.Find(l => l.languageCode == languageCode);
        if (lang == null) return "[Missing Lang: " + languageCode + "]";
        if (idx >= 0 && idx < lang.values.Count) return lang.values[idx];
        return "";
    }


    public void AddKey(string key)
    {
        if (keys.Contains(key)) return;
        keys.Add(key);
        foreach (var l in languages) l.values.Add("");
    }


    public void RemoveKeyAt(int index)
    {
        if (index < 0 || index >= keys.Count) return;
        keys.RemoveAt(index);
        foreach (var l in languages)
        {
            if (index < l.values.Count) l.values.RemoveAt(index);
        }
    }


    public void AddLanguage(string code, string displayName)
    {
        if (languages.Exists(x => x.languageCode == code)) return;
        var lang = new Language() { languageCode = code, displayName = displayName };
        for (int i = 0; i < keys.Count; i++) lang.values.Add("");
        languages.Add(lang);
    }


    public void RemoveLanguageAt(int index)
    {
        if (index < 0 || index >= languages.Count) return;
        languages.RemoveAt(index);
    }
}