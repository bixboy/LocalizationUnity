using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
// Puissance pour le slow/fast motion
    [Range(0.1f, 5f)] public float slowMotionScale = 0.5f; // Puissance du slow motion
    [Range(1.0f, 5f)] public float fastMotionScale = 2f;   // Puissance du fast motion
    public float transitionSpeed = 1f;
    private float targetTimeScale = 1f;

    // Inversion du temps
    private bool isReversing = false;
    private List<RecordedState> recordedStates = new List<RecordedState>();
    public float maxReverseDuration = 5f;

    private void Update()
    {
        Time.timeScale = Mathf.Lerp(Time.timeScale, targetTimeScale, Time.unscaledDeltaTime * transitionSpeed);

        if (isReversing)
        {
            ReverseTime();
        }
        else
        {
            RecordState();
        }

        if (Input.GetKeyDown(KeyCode.S)) StartSlowMotion();
        if (Input.GetKeyDown(KeyCode.F)) StartFastMotion();
        if (Input.GetKeyDown(KeyCode.R)) StartReverseMotion();
        if (Input.GetKeyDown(KeyCode.N)) ResetTimeScale();
    }

    public void StartSlowMotion()
    {
        targetTimeScale = slowMotionScale;
    }

    public void StartFastMotion()
    {
        targetTimeScale = fastMotionScale;
    }

    public void StartReverseMotion()
    {
        isReversing = true;
        targetTimeScale = 0f;
    }

    public void ResetTimeScale()
    {
        targetTimeScale = 1f;
        isReversing = false;
    }

    private void RecordState()
    {
        if (Time.timeScale == 1f)
        {
            recordedStates.Add(new RecordedState(transform.position, transform.rotation));

            if (recordedStates.Count > Mathf.CeilToInt(maxReverseDuration / Time.fixedDeltaTime))
            {
                recordedStates.RemoveAt(0);
            }
        }
    }

    // Inverser le temps
    private void ReverseTime()
    {
        if (recordedStates.Count > 0)
        {
            RecordedState lastState = recordedStates[recordedStates.Count - 1];
            transform.position = lastState.Position;
            transform.rotation = lastState.Rotation;

            recordedStates.RemoveAt(recordedStates.Count - 1);
        }
        else
        {
            ResetTimeScale();
        }
    }

    // Classe pour stocker les états
    private class RecordedState
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public RecordedState(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
