using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class LocalizationManagerInitializer
{
    static LocalizationManagerInitializer()
    {
        AddLocalizationManagerToScene();
    }

    private static void AddLocalizationManagerToScene()
    {
        LocalizationManager existingManager = Object.FindObjectOfType<LocalizationManager>();

        if (existingManager == null)
        {
            GameObject localizationManagerObject = new GameObject("LocalizationManager");
            localizationManagerObject.AddComponent<LocalizationManager>();
            localizationManagerObject.GetComponent<LocalizationManager>().Initialization();
            Debug.Log("LocalizationManager added to the scene.");
        }
    }
}