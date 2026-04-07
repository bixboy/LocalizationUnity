using UnityEngine;
using UnityEditor;
using System.IO;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera.Editor
{
    public static class CameraPresetsHelper
    {
        [MenuItem("Tools/Camera/Generate Default Presets")]
        public static void GeneratePresets()
        {
            string folderPath = "Assets/Settings/Camera/Presets";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // 1. Shake Presets
            CreateShakeProfile(folderPath, "Shake_HeavyExplosion", 1.2f, 2.5f);
            CreateShakeProfile(folderPath, "Shake_LightTremor", 0.2f, 0.8f);

            // 2. Handheld Presets
            CreateHandheldProfile(folderPath, "Handheld_Natural", 0.05f, 0.5f);
            CreateHandheldProfile(folderPath, "Handheld_Tension", 0.3f, 1.2f);
            
            // 3. Dolly Presets
            CreateDollyProfile(folderPath, "Dolly_Smooth", AnimationCurve.EaseInOut(0, 0, 1, 1));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"[CameraTool] Generated specialized presets in {folderPath}");
        }

        private static void CreateShakeProfile(string path, string name, float intensity, float roughness)
        {
            string assetPath = $"{path}/{name}.asset";
            if (File.Exists(Application.dataPath + assetPath.Substring(6)))
                return;

            var profile = ScriptableObject.CreateInstance<CameraShakeProfile>();
            profile.intensity = intensity;
            profile.roughness = roughness;
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        private static void CreateHandheldProfile(string path, string name, float intensity, float speed)
        {
            string assetPath = $"{path}/{name}.asset";
            if (File.Exists(Application.dataPath + assetPath.Substring(6)))
                return;

            var profile = ScriptableObject.CreateInstance<CameraHandheldProfile>();
            profile.intensity = intensity;
            profile.speed = speed;
            AssetDatabase.CreateAsset(profile, assetPath);
        }

        private static void CreateDollyProfile(string path, string name, AnimationCurve curve)
        {
            string assetPath = $"{path}/{name}.asset";
            if (File.Exists(Application.dataPath + assetPath.Substring(6)))
                return;

            var profile = ScriptableObject.CreateInstance<CameraDollyProfile>();
            profile.fovCurve = curve;
            AssetDatabase.CreateAsset(profile, assetPath);
        }
    }
}
