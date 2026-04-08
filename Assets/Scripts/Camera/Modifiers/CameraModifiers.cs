using UnityEngine;

namespace Metroma.CameraTool.Modifiers
{

    public static class CameraModifiers
    {
        // ══════════════════════════════════════════════════════════════
        // Shakes & Vibrations
        // ══════════════════════════════════════════════════════════════

        /// <summary> Applies a continuous Perlin-noise shake to the camera. </summary>
        public static void DoShake(this Camera camera, float intensity, float duration, float roughness = 1f, bool fadeOut = true)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddShake(intensity, duration, roughness, fadeOut);
        }

        /// <summary> Applies a shake using settings from a profile. </summary>
        public static void DoShake(this Camera camera, CameraShakeProfile profile, float duration)
        {
            if (profile == null)
                return;

            DoShake(camera, profile.intensity, duration, profile.roughness, profile.fadeOut);
        }

        /// <summary> Applies a shake that scales down based on distance from the source. </summary>
        public static void DoShakeAt(this Camera camera, Vector3 sourcePos, float maxIntensity, float duration, float radius, float falloff = 1f)
        {
            float dist = Vector3.Distance(camera.transform.position, sourcePos);
            if (dist > radius)
                return;

            float t = Mathf.Clamp01(1f - (dist / radius));
            float intensity = maxIntensity * Mathf.Pow(t, falloff);

            DoShake(camera, intensity, duration);
        }

        /// <summary> Applies a profile-based shake attenuated by distance. </summary>
        public static void DoShakeAt(this Camera camera, Vector3 sourcePos, CameraShakeProfile profile, float duration, float radius, float falloff = 1f)
        {
            if (profile == null)
                return;
            
            DoShakeAt(camera, sourcePos, profile.intensity, duration, radius, falloff);
        }

        /// <summary> Applies a strong directional impact (e.g., weapon recoil, landing) that drops off smoothly. </summary>
        public static void DoImpact(this Camera camera, Vector3 direction, float intensity, float duration)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddImpact(direction, intensity, duration);
        }

        // ══════════════════════════════════════════════════════════════
        // Lens & FOV
        // ══════════════════════════════════════════════════════════════

        /// <summary> Smoothly transitions the Field of View. </summary>
        public static void DoFOVTransition(this Camera camera, float targetFOV, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.SetFovTransition(targetFOV, duration, curve);
        }

        /// <summary> Repeatedly pulses the Field of View back and forth (e.g., breathing effect). </summary>
        public static void DoFOVPulse(this Camera camera, float amplitude, float frequency, float duration)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.SetFovPulse(amplitude, frequency, duration);
        }

        /// <summary> Pushes the camera position while expanding the FOV to keep the subject framed (Vertigo effect). </summary>
        public static void DoDollyZoom(this Camera camera, float pushDistance, float targetFOV, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.SetDollyZoom(pushDistance, targetFOV, duration, curve);
        }

        /// <summary> Pushes the camera using settings from a profile. </summary>
        public static void DoDollyZoom(this Camera camera, float pushDistance, float targetFOV, float duration, CameraDollyProfile profile)
        {
            if (profile == null)
                return;

            DoDollyZoom(camera, pushDistance, targetFOV, duration, profile.fovCurve);
        }

        // ══════════════════════════════════════════════════════════════
        // Transforms & Offsets
        // ══════════════════════════════════════════════════════════════

        /// <summary> Adds a temporary positional offset that reverts back to zero when duration expires. </summary>
        public static void AddPositionOffset(this Camera camera, Vector3 localOffset, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddPositionOffset(localOffset, duration, curve);
        }

        /// <summary> Adds a dual-phase positional offset (Move + Return). </summary>
        public static void AddPositionOffset(this Camera camera, Vector3 localOffset, float mainDuration, AnimationCurve mainCurve, float returnDuration, AnimationCurve returnCurve, bool invertReturn = true)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddPositionOffset(localOffset, mainDuration, mainCurve, returnDuration, returnCurve, invertReturn);
        }

        /// <summary> Adds a temporary rotational offset (Euler angles) that reverts back to zero when duration expires. </summary>
        public static void AddRotationOffset(this Camera camera, Vector3 localEulerAngles, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddRotationOffset(localEulerAngles, duration, curve);
        }

        /// <summary> Adds a dual-phase rotational offset (Move + Return). </summary>
        public static void AddRotationOffset(this Camera camera, Vector3 localEulerAngles, float mainDuration, AnimationCurve mainCurve, float returnDuration, AnimationCurve returnCurve, bool invertReturn = true)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddRotationOffset(localEulerAngles, mainDuration, mainCurve, returnDuration, returnCurve, invertReturn);
        }

        /// <summary> Activates a continuous organic "Hand-held" breathing effect on the camera. </summary>
        public static void SetHandheld(this Camera camera, bool active, float amplitude = 1f, float frequency = 1f)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.SetHandheld(active, amplitude, frequency);
        }

        /// <summary> Sets handheld effect using a profile. </summary>
        public static void SetHandheld(this Camera camera, bool active, CameraHandheldProfile profile)
        {
            if (profile == null)
                return;
                
            SetHandheld(camera, active, profile.intensity, profile.speed);
        }

        /// <summary> Adds a temporary continuous roll wave (Z-axis Sine) to the camera (Drunk/Poison Wobble). </summary>
        public static void DoWobble(this Camera camera, float amplitude, float frequency, float duration)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddWobble(amplitude, frequency, duration);
        }

        /// <summary> Forces the camera to temporarily Look At a specific target, overriding base behavior, then nicely releases it. </summary>
        public static void SetTemporaryLookAt(this Camera camera, Transform target, float duration, AnimationCurve focusCurve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.SetTemporaryLookAt(target, duration, focusCurve);
        }

        public static void SetDutchAngle(this Camera camera, float targetAngleZ, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddModifierHandler(camera);
            if (h != null)
                h.AddRotationOffset(new Vector3(0, 0, targetAngleZ), duration, curve);
        }

        // ══════════════════════════════════════════════════════════════
        // Screen & Time Effects (Visuals & Game Feel)
        // ══════════════════════════════════════════════════════════════

        /// <summary> Performs a solid color screen flash (e.g. Damage, Camera Flash). Uses a hidden UI Overlay. </summary>
        public static void DoFlash(this Camera camera, Color flashColor, float duration)
        {
            var h = GetOrAddVfxHandler(camera);
            if (h != null)
                h.DoFlash(flashColor, duration);
        }

        public static void DoHitStop(this Camera camera, float duration, float timeScale = 0.05f)
        {
            var h = GetOrAddVfxHandler(camera);
            if (h != null)
                h.DoHitStop(duration, timeScale);
        }

        /// <summary> Performs a smooth Depth of Field transition (Rack Focus). Requires URP. </summary>
        public static void DoDepthOfFieldFocus(this Camera camera, float targetDistance, float duration, AnimationCurve curve = null)
        {
            var h = GetOrAddVfxHandler(camera);
            if (h != null)
                h.DoDepthOfFieldFocus(targetDistance, duration, curve);
        }

        /// <summary> Performs a chromatic aberration impact pulse. Requires URP. </summary>
        public static void DoChromaticAberration(this Camera camera, float intensity, float duration)
        {
            var h = GetOrAddVfxHandler(camera);
            if (h != null)
                h.DoChromaticAberrationPulse(intensity, duration);
        }

        /// <summary> Enables real-time depth of field sync to a target transform. </summary>
        public static void SetAutoFocus(this Camera camera, bool enabled, Transform target = null)
        {
            var h = GetOrAddVfxHandler(camera);
            if (h != null)
                h.SetAutoFocus(enabled, target);
        }

        // ══════════════════════════════════════════════════════════════
        // Utilities
        // ══════════════════════════════════════════════════════════════

        /// <summary> Instantly stops all active effects and zeros out offsets. </summary>
        public static void StopAllCameraModifiers(this Camera camera)
        {
            if (camera == null)
                camera = Camera.main;
            
            if (camera == null)
                return;

            if (camera.TryGetComponent<CameraModifierHandler>(out var h1))
                h1.StopAll();
            
            if (camera.TryGetComponent<CameraVisualEffectsHandler>(out var h2))
                h2.StopAll();
        }

        /// <summary> Smoothly interpolates active offsets back to zero over a specified duration. </summary>
        public static void ClearOffsetsSmoothly(this Camera camera, float fadeDuration)
        {
            if (camera.TryGetComponent<CameraModifierHandler>(out var handler))
                handler.ClearOffsetsSmoothly(fadeDuration);
        }

        private static CameraModifierHandler GetOrAddModifierHandler(this Camera camera)
        {
            if (camera == null)
                camera = Camera.main;
            
            if (camera == null)
                return null;

            if (!camera.TryGetComponent<CameraModifierHandler>(out var handler))
                handler = camera.gameObject.AddComponent<CameraModifierHandler>();
            
            return handler;
        }

        private static CameraVisualEffectsHandler GetOrAddVfxHandler(this Camera camera)
        {
            if (camera == null)
                camera = Camera.main;
            
            if (camera == null)
                return null;

            if (!camera.TryGetComponent<CameraVisualEffectsHandler>(out var handler))
                handler = camera.gameObject.AddComponent<CameraVisualEffectsHandler>();
            
            return handler;
        }
    }
}
