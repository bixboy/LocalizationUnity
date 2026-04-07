using UnityEngine;

namespace Metroma.Camera.Modifiers
{
    [CreateAssetMenu(fileName = "NewHandheldProfile", menuName = "Camera/Profiles/Handheld")]
    public class CameraHandheldProfile : ScriptableObject
    {
        public float intensity = 0.1f;
        public float speed = 0.5f;
    }
}
