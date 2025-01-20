using UnityEditor;
using UnityEngine;

public static class LocalizationManagerInitializer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AddLocalizationManagerToScene()
    {
        LocalizationManager existingManager = Object.FindFirstObjectByType<LocalizationManager>();
        if (existingManager != null)
        {
            Debug.Log("LocalizationManager already exists in the scene.");
            return;
        }

        GameObject localizationManagerObject = new GameObject("LocalizationManager");
        LocalizationManager newManager = localizationManagerObject.AddComponent<LocalizationManager>();
        Debug.Log("LocalizationManager added to the scene.");
    }
}