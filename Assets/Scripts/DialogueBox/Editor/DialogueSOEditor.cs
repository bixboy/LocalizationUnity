using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogSO))]
public class DialogueSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DialogSO dialog = (DialogSO)target;

        if (dialog.dialogueLines == null)
            dialog.dialogueLines = new List<DialogSO.DialogueLine>();

        if (GUILayout.Button("Add New Line"))
        {
            dialog.dialogueLines.Add(new DialogSO.DialogueLine());
        }

        for (int i = 0; i < dialog.dialogueLines.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            DialogSO.DialogueLine line = dialog.dialogueLines[i];

            line.dialogueText = EditorGUILayout.TextField("Dialogue Text", line.dialogueText);
            line.displayMode = (DialogSO.DisplayMode)EditorGUILayout.EnumPopup("Display Mode", line.displayMode);

            EditorGUILayout.EndVertical();
        }

        EditorUtility.SetDirty(dialog);
    }
}