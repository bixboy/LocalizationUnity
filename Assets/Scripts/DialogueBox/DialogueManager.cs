using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;
    public static GameObject currentDialog;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
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
        if (line.popupPrefab)
        {
            currentDialog = Instantiate(line.popupPrefab);
            if (currentDialog)
            {
                currentDialog.transform.position = Vector3.zero;

                TextMeshProUGUI textComponent = currentDialog.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent)
                {
                    textComponent.text = line.dialogueText;
                }

                if (line.duration <= 0)
                {
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));   
                }
                else
                {
                    yield return new WaitForSeconds(line.duration);
                }

                Destroy(currentDialog);   
            }
        }
        else
        {
            Debug.LogWarning("Popup prefab is missing!");
        }
    }

    private System.Collections.IEnumerator ShowPanel2D(DialogSO.DialogueLine line)
    {
        if (line.panelPrefab)
        {
            currentDialog = Instantiate(line.panelPrefab);
            if (currentDialog)
            {
                currentDialog.transform.position = Vector3.zero;

                TextMeshProUGUI textComponent = currentDialog.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent)
                {
                    textComponent.text = line.dialogueText;
                }

                if (line.duration <= 0)
                {
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));   
                }
                else
                {
                    yield return new WaitForSeconds(line.duration);
                }

                Destroy(currentDialog);   
            }
        }
        else
        {
            Debug.LogWarning("Panel2D prefab is missing!");
        }
    }

    private System.Collections.IEnumerator ShowBubble3D(DialogSO.DialogueLine line)
    {
        if (line.bubblePrefab)
        {
            currentDialog = Instantiate(line.bubblePrefab);
            if (currentDialog)
            {
                currentDialog.transform.position = new Vector3(0, 1, 0);

                Text textComponent = currentDialog.GetComponentInChildren<Text>();
                if (textComponent)
                {
                    textComponent.text = line.dialogueText;
                }

                if (line.duration <= 0)
                {
                    yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));   
                }
                else
                {
                    yield return new WaitForSeconds(line.duration);
                }

                Destroy(currentDialog);   
            }
        }
        else
        {
            Debug.LogWarning("Bubble3D prefab is missing!");
        }
    }
}