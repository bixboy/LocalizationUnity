using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class LocalizationManagerInitializer
{
    static LocalizationManagerInitializer()
    {
        EditorApplication.delayCall += AddLocalizationManagerToScene;
    }

    private static void AddLocalizationManagerToScene()
    {
        LocalizationManager existingManager = Object.FindObjectOfType<LocalizationManager>();
        if (existingManager != null)
        {
            existingManager.Initialization();
        }
        else
        {
            GameObject localizationManagerObject = new GameObject("LocalizationManager");
            LocalizationManager newManager = localizationManagerObject.AddComponent<LocalizationManager>();

            newManager.Initialization();
            Debug.Log("LocalizationManager added to the scene.");
        }
    }
}