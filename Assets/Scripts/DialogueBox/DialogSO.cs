using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New Dialog", menuName = "Dialog/Dialogue")]
public class DialogSO : ScriptableObject
{
    public enum DisplayMode
    {
        Popup,
        Panel2D,
        Bulle3D
    }

    [System.Serializable]
    public class DialogueLine
    {
        public string dialogueText;
        public DisplayMode displayMode;
        public float duration;
        public GameObject popupPrefab;
        public GameObject panelPrefab;
        public GameObject bubblePrefab;
    }

    public List<DialogueLine> dialogueLines;
}