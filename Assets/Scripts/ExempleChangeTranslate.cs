using TMPro;
using UnityEngine;

public class ExempleChangeTranslate : MonoBehaviour
{

    public void ChangeLanguage(TranslationComponent compLang)
    {
        Debug.Log(compLang);
        LocalizationManager.Instance.SetLanguage(compLang.GetTextComponent().text);
    }
}
