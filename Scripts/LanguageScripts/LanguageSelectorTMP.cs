using UnityEngine;
using TMPro;


[RequireComponent(typeof(TMP_Dropdown))]
public class LanguageSelectorTMP : MonoBehaviour
{
    private TMP_Dropdown dropdown;


    void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        var db = (LanguageManager.Instance != null) ? LanguageManager.Instance.database : null;
        if (db == null) return;


        dropdown.ClearOptions();
        var options = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
        foreach (var l in db.languages)
            options.Add(new TMP_Dropdown.OptionData(l.displayName));


        dropdown.AddOptions(options);


        int idx = Mathf.Max(0, db.languages.FindIndex(x => x.languageCode == LanguageManager.Instance.activeLanguage));
        dropdown.value = idx;
        dropdown.onValueChanged.AddListener(OnSelected);
    }


    void OnSelected(int i)
    {
        var db = LanguageManager.Instance.database;
        if (db == null) return;


        if (i >= 0 && i < db.languages.Count)
        {
            LanguageManager.Instance.SetLanguage(db.languages[i].languageCode);
        }
    }
}