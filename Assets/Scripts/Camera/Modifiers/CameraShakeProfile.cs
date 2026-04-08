using UnityEngine;

namespace Metroma.CameraTool.Modifiers
{
    [CreateAssetMenu(fileName = "NewShakeProfile", menuName = "Camera/Profiles/Shake")]
    public class CameraShakeProfile : ScriptableObject
    {
        public float intensity = 0.5f;
        public float roughness = 1.0f;
        public bool fadeOut = true;
    }
}
