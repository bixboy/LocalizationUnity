using UnityEngine;

public class ExempleDialogue : MonoBehaviour
{
    [SerializeField]
    private DialogSO _currentDialog;
    
    void Start()
    {
        if (_currentDialog)
        {
            DialogueManager.instance.StartDialogue(_currentDialog);   
        }
    }
    
}
