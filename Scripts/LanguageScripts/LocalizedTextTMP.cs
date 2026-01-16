using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextTMP : MonoBehaviour
{
    public string key;

    private TextMeshProUGUI tmpText;

    void Awake()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
    }

    void Start()
    {
        UpdateText();
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged += UpdateText;
    }

    void OnDestroy()
    {
        if (LanguageManager.Instance != null)
            LanguageManager.Instance.OnLanguageChanged -= UpdateText;
    }

    public void UpdateText()
    {
        if (LanguageManager.Instance == null) return;

        string localized = LanguageManager.Instance.Get(key);
        if (tmpText != null)
            tmpText.text = localized;
    }
}
