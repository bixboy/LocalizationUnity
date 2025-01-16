using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class DialogTool : EditorWindow
{
    private DialogSO currentDialog;

    [MenuItem("Tools/Dialogue Editor")]
    public static void ShowWindow()
    {
        GetWindow<DialogTool>("Dialogue Editor");
    }

    private void OnGUI()
    {
        GUILayout.Label("Dialogue Editor", EditorStyles.boldLabel);

        currentDialog = (DialogSO)EditorGUILayout.ObjectField("Dialog ScriptableObject", currentDialog, typeof(DialogSO), false);

        if (currentDialog != null)
        {
            DisplayDialogueLines();
            DisplayAddButton();
        }
        else
        {
            GUILayout.Label("Please assign a DialogSO.");
        }

        if (GUILayout.Button("Save Changes"))
        {
            SavingChanges();
        }
    }

    private void DisplayDialogueLines()
    {
        for (int i = 0; i < currentDialog.dialogueLines.Count; i++)
        {
            var line = currentDialog.dialogueLines[i];
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                currentDialog.dialogueLines.RemoveAt(i);
                return;
            }

            EditorGUILayout.LabelField($"Line {i + 1}:");
            line.dialogueText = EditorGUILayout.TextField(line.dialogueText);

            // Affichage conditionnel des paramètres en fonction du type de dialogue
            line.displayMode = (DialogSO.DisplayMode)EditorGUILayout.EnumPopup("Display Mode", line.displayMode);
            switch (line.displayMode)
            {
                case DialogSO.DisplayMode.Popup:
                    line.popupPrefab = (GameObject)EditorGUILayout.ObjectField("Popup Prefab", line.popupPrefab, typeof(GameObject), false);
                    if (GUILayout.Button("Create Popup Prefab"))
                    {
                        CreatePopupPrefab(line);
                    }
                    break;
                case DialogSO.DisplayMode.Panel2D:
                    line.panelPrefab = (GameObject)EditorGUILayout.ObjectField("Panel Prefab", line.panelPrefab, typeof(GameObject), false);
                    if (GUILayout.Button("Create Panel2D Prefab"))
                    {
                        CreatePanel2DPrefab(line);
                    }
                    break;
                case DialogSO.DisplayMode.Bulle3D:
                    line.bubblePrefab = (GameObject)EditorGUILayout.ObjectField("Bubble Prefab", line.bubblePrefab, typeof(GameObject), false);
                    if (GUILayout.Button("Create Bulle3D Prefab"))
                    {
                        CreateBubble3DPrefab(line);
                    }
                    break;
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DisplayAddButton()
    {
        if (GUILayout.Button("Add Dialogue Line"))
        {
            currentDialog.dialogueLines.Add(new DialogSO.DialogueLine());
        }
    }

    private void SavingChanges()
    {
        EditorUtility.SetDirty(currentDialog);
        AssetDatabase.SaveAssets();
    }

   private void CreatePopupPrefab(DialogSO.DialogueLine line)
    {
        // Canva
        GameObject popup = new GameObject("Popup");
        Canvas canvas = popup.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        // Panel
        GameObject panel = new GameObject("Group");
        panel.transform.SetParent(popup.transform);
        RectTransform panelRectTransform = panel.AddComponent<RectTransform>();
        panelRectTransform.anchorMin = new Vector2(0, 0);
        panelRectTransform.anchorMax = new Vector2(0, 0);
        panelRectTransform.pivot = new Vector2(0, 0);
        panelRectTransform.anchoredPosition = Vector2.zero;
        panelRectTransform.sizeDelta = new Vector2(290, 90);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = Color.gray;
        
        // Texte dans le panel
        GameObject textObject = new GameObject("Text-Popup");
        textObject.transform.SetParent(panel.transform);

        RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0, 0);
        textRectTransform.anchorMax = new Vector2(0, 0);
        textRectTransform.pivot = new Vector2(0, 0);
        textRectTransform.anchoredPosition = Vector2.zero;
        textRectTransform.sizeDelta = new Vector2(290, 90);
        textRectTransform.position = Vector2.zero;

        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.text = line.dialogueText;
        textMesh.enableAutoSizing = true;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.margin = new Vector4(5, 5, 88, 5);
    
        // Bouton pour fermer le popup
        GameObject closeButton = new GameObject("Close-Button");
        closeButton.transform.SetParent(panel.transform);

        RectTransform buttonRectTransform = closeButton.AddComponent<RectTransform>();
        buttonRectTransform.anchorMin = new Vector2(1, 0);
        buttonRectTransform.anchorMax = new Vector2(1, 0);
        buttonRectTransform.pivot = new Vector2(1,0);
        buttonRectTransform.sizeDelta = new Vector2(78, 25);

        Button button = closeButton.AddComponent<Button>();
        button.onClick.AddListener(() => Destroy(popup));

        Image buttonImage = closeButton.AddComponent<Image>();
        buttonImage.color = Color.white;
        
        // Texte du bouton
        GameObject textButton = new GameObject("Text-Button");
        textButton.transform.SetParent(closeButton.transform, true);

        RectTransform textButtonRectTransform = textButton.AddComponent<RectTransform>();
        textButtonRectTransform.anchorMin = new Vector2(0, 0);
        textButtonRectTransform.anchorMax = new Vector2(1, 1);
        textButtonRectTransform.pivot = new Vector2(0.5f, 0.5f);
        textButtonRectTransform.offsetMin = Vector2.zero;
        textButtonRectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI textBtnMesh = textButton.AddComponent<TextMeshProUGUI>();
        textBtnMesh.text = "Close";
        textBtnMesh.color = Color.black;
        textBtnMesh.alignment = TextAlignmentOptions.Center;
        textBtnMesh.enableAutoSizing = true;
    
        string folderPath = "Assets/Dialogs/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Dialogs", "Prefabs");
        }
    
        string path = folderPath + "/Popup.prefab";
        PrefabUtility.SaveAsPrefabAsset(popup, path);
        DestroyImmediate(popup);
    
        line.popupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
    
    private void CreatePanel2DPrefab(DialogSO.DialogueLine line)
    {
        // Panel
        GameObject panel = new GameObject("Panel");
        RectTransform panelRectTransform = panel.AddComponent<RectTransform>();
        panelRectTransform.anchorMin = new Vector2(0, 0);
        panelRectTransform.anchorMax = new Vector2(1, 1);
        panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
        panelRectTransform.position = Vector2.zero;
        panelRectTransform.offsetMin = Vector2.zero;
        panelRectTransform.offsetMax = Vector2.zero;
        
        Canvas canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        Image image = panel.AddComponent<Image>();
        image.color = new Color(128f / 255f, 128f / 255f, 128f / 255f, 70f / 255f);
        
        // Text
        GameObject textObject = new GameObject("Text Object");
        textObject.transform.SetParent(panel.transform);
        
        RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
        textRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        textRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        textRectTransform.pivot = new Vector2(0.5f, 0.5f);
        textRectTransform.sizeDelta = new Vector2(450, 150);
        
        TextMeshProUGUI textMesh = textObject.AddComponent<TextMeshProUGUI>();
        textMesh.enableAutoSizing = true;
        textMesh.fontSizeMax = 38;
        textMesh.fontSizeMin = 18;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.text = "Sample Text";
    
        string folderPath = "Assets/Dialogs/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Dialogs", "Prefabs");
        }
    
        string path = folderPath + "/Panel2D.prefab";
        PrefabUtility.SaveAsPrefabAsset(panel, path);
        DestroyImmediate(panel);
        
        line.panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
    
    private void CreateBubble3DPrefab(DialogSO.DialogueLine line)
    {
        GameObject bubble = new GameObject("Bubble3D");
        MeshRenderer meshRenderer = bubble.AddComponent<MeshRenderer>();
        TextMeshPro textMesh = bubble.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.enableAutoSizing = true;
        
        Material tmpMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF");
        if (tmpMaterial != null)
        {
            meshRenderer.material = tmpMaterial;
        }
        else
        {
            Debug.LogWarning("LiberationSans SDF Material not found! Make sure the TextMeshPro package is properly imported.");
        }
        
        textMesh.text = "Sample Text";
    
        string folderPath = "Assets/Dialogs/Prefabs";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Dialogs", "Prefabs");
        }
    
        string path = folderPath + "/Bubble3D.prefab";
        PrefabUtility.SaveAsPrefabAsset(bubble, path);
        DestroyImmediate(bubble);
        
        line.bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private Mesh CreateBubbleMesh()
    {
        // Create a simple 3D bubble (sphere)
        return GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().mesh;
    }
}