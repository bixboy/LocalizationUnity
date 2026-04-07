using UnityEngine;

namespace Metroma.Camera.Modifiers
{
    [CreateAssetMenu(fileName = "NewDollyProfile", menuName = "Camera/Profiles/Dolly Zoom")]
    public class CameraDollyProfile : ScriptableObject
    {
        public float weight = 1.0f;
        public AnimationCurve fovCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }
}
