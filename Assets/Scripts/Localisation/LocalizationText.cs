using UnityEngine;
using UnityEngine.UI;

public class LocalizationText : Text
{
    [SerializeField] private string localizationKey;

    protected override void OnEnable()
    {
        base.OnEnable();
        LocalizationManager.OnLanguageChanged += UpdateLocalizedText;
        UpdateLocalizedText();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        LocalizationManager.OnLanguageChanged -= UpdateLocalizedText;
    }

    private void UpdateLocalizedText()
    {
        text = LocalizationManager.Instance.GetTranslation(localizationKey);
    }
}
