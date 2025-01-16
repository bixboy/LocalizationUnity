using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject popupPrefab;
    public GameObject panelPrefab;
    public GameObject bubblePrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void StartDialogue(DialogSO dialog)
    {
        StartCoroutine(DisplayDialogue(dialog));
    }

    private System.Collections.IEnumerator DisplayDialogue(DialogSO dialog)
    {
        foreach (var line in dialog.dialogueLines)
        {
            yield return ShowDialogueLine(line);
        }
    }

    private System.Collections.IEnumerator ShowDialogueLine(DialogSO.DialogueLine line)
    {
        switch (line.displayMode)
        {
            case DialogSO.DisplayMode.Popup:
                yield return ShowPopup(line);
                break;
            case DialogSO.DisplayMode.Panel2D:
                yield return ShowPanel2D(line);
                break;
            case DialogSO.DisplayMode.Bulle3D:
                yield return ShowBubble3D(line);
                break;
        }
    }

    private System.Collections.IEnumerator ShowPopup(DialogSO.DialogueLine line)
    {
        if (line.popupPrefab != null)
        {
            GameObject popup = Instantiate(line.popupPrefab);
            popup.transform.position = Vector3.zero;

            Text textComponent = popup.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = line.dialogueText;
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

            Destroy(popup);
        }
        else
        {
            Debug.LogWarning("Popup prefab is missing!");
        }
    }

    private System.Collections.IEnumerator ShowPanel2D(DialogSO.DialogueLine line)
    {
        if (line.panelPrefab != null)
        {
            GameObject panel = Instantiate(line.panelPrefab);
            panel.transform.position = Vector3.zero;

            Text textComponent = panel.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = line.dialogueText;
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

            Destroy(panel);
        }
        else
        {
            Debug.LogWarning("Panel2D prefab is missing!");
        }
    }

    private System.Collections.IEnumerator ShowBubble3D(DialogSO.DialogueLine line)
    {
        if (line.bubblePrefab != null)
        {
            GameObject bubble = Instantiate(line.bubblePrefab);
            bubble.transform.position = new Vector3(0, 1, 0);

            Text textComponent = bubble.GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = line.dialogueText;
            }

            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));

            Destroy(bubble);
        }
        else
        {
            Debug.LogWarning("Bubble3D prefab is missing!");
        }
    }
}