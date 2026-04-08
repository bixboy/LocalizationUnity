using Metroma.CameraTool;
using Metroma.CameraTool.Modifiers;
using UnityEngine;

public class TestAnimCamera : MonoBehaviour
{
    
    [SerializeField] private CameraTool cameraTool;
    
    [SerializeField] private Camera cameraRef;
    
    void Start()
    {
        if (cameraTool)
        {
            cameraTool.onSplineNotified.AddListener(TestNotif);
        }
    }

    private void TestNotif(string eventName)
    {
        Debug.Log($"[TestAnimCamera] Received event: {eventName}");

        if (eventName == "Flash")
        {
            if (cameraRef != null)
            {
                cameraRef.DoChromaticAberration(100f, 5f);
            }
        }
    }
}
