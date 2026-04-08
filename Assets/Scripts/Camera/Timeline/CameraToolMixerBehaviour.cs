using UnityEngine.Playables;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Mixer that blends overlapping <see cref="CameraToolClip"/> clips
    /// and writes the final spline progress + lookAt weight to the bound <see cref="CameraTool"/>.
    /// Zero-allocation per frame.
    /// </summary>
    public class CameraToolMixerBehaviour : PlayableBehaviour
    {
        private CameraTool _boundCameraTool;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            _boundCameraTool = playerData as CameraTool;
            if (_boundCameraTool == null)
                return;

            int inputCount = playable.GetInputCount();
            float blendedProgress = 0f;
            float blendedLookAtWeight = 0f;
            float totalWeight = 0f;

            for (int i = 0; i < inputCount; i++)
            {
                float inputWeight = playable.GetInputWeight(i);
                if (inputWeight <= 0f)
                    continue;

                ScriptPlayable<CameraToolBehaviour> inputPlayable =
                    (ScriptPlayable<CameraToolBehaviour>)playable.GetInput(i);

                CameraToolBehaviour behaviour = inputPlayable.GetBehaviour();

                float normalizedTime = (float)(inputPlayable.GetTime() / inputPlayable.GetDuration());

                float easedTime = behaviour.easingCurve.Evaluate(normalizedTime);

                float clipProgress = behaviour.startProgress + (behaviour.endProgress - behaviour.startProgress) * easedTime;

                blendedProgress += clipProgress * inputWeight;
                blendedLookAtWeight += behaviour.lookAtWeight * inputWeight;
                totalWeight += inputWeight;
            }

            if (totalWeight > 0f)
            {
                float invWeight = 1f / totalWeight;
                _boundCameraTool.SplineProgress = blendedProgress * invWeight;
                _boundCameraTool.LookAtWeight = blendedLookAtWeight * invWeight;
            }
        }
    }
}
