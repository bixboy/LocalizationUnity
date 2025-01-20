using System;
using TMPro;
using UnityEngine;

public class ExempleChangeTranslate : MonoBehaviour
{

    [SerializeField]
    private TextMeshProUGUI textMesh;

    public void Start()
    {
        if (textMesh)
        {
            textMesh.text = LocalizationManager.Instance.GetTranslation("Btn-Languages");
        }
    }

    public void ChangeLanguage()
    {
        LocalizationManager.Instance.SetLanguage(textMesh.text);
    }
}
