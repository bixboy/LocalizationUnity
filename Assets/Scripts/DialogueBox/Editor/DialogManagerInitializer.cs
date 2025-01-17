using UnityEditor;
using UnityEngine;

public static class DialogManagerInitializer
{
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AddDialogManagerToScene()
    {
        DialogueManager existingManager = Object.FindFirstObjectByType<DialogueManager>();
        if (existingManager != null)
        {
            Debug.Log("DialogManagerObject already exists in the scene.");
            return;
        }

        GameObject dialogManagerObject = new GameObject("DialogManagerObject");
        DialogueManager newManager = dialogManagerObject.AddComponent<DialogueManager>();
        Debug.Log("DialogManagerObject added to the scene.");
    }
}