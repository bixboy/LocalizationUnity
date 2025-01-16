using System;
using TMPro;
using UnityEngine;

public class ExempleChangeTranslate : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI textMesh;
    
    public DialogSO currentDialog;

    public void Start()
    {
        if (textMesh)
        {
            textMesh.text = "Boop " + LocalizationManager.Instance.GetTranslation("Txt-play");
        }

        if (currentDialog)
        {
            DialogueManager.Instance.StartDialogue(currentDialog);   
        }
    }

    public void ChangeLanguage(TranslationComponent compLang)
    {
        Debug.Log(compLang);
        LocalizationManager.Instance.SetLanguage(compLang.GetTextComponent().text);
    }
}
