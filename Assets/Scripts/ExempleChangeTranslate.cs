using TMPro;
using UnityEngine;

public class ExempleChangeTranslate : MonoBehaviour
{

    public void ChangeLanguage(TranslationComponent CompLang)
    {
        LocalizationManager.Instance.SetLanguage(CompLang.GetTextComponent().text);
    }
}
