using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Metroma.Camera.Modifiers
{
    /// <summary>
    /// Handles camera-centric visual effects like Screen Flashes and Hit Stops (TimeScale).
    /// Kept separate from Transform modifiers to maintain single responsibility.
    /// </summary>
    public class CameraVisualEffectsHandler : MonoBehaviour
    {
        private Canvas _flashCanvas;
        private Image _flashImage;
        private Coroutine _flashRoutine;
        private Coroutine _hitStopRoutine;
        private Coroutine _dofRoutine;
        private Coroutine _chromaticRoutine;

        private Volume _localVolume;
        private DepthOfField _dof;
        private ChromaticAberration _chromatic;
        
        private Transform _autoFocusTarget;
        private bool _autoFocusEnabled;

        private void EnsureCanvasExists()
        {
            if (_flashCanvas != null)
                return;
            
            GameObject canvasGo = new GameObject("Camera_FlashCanvas");
            canvasGo.transform.SetParent(this.transform, false);
            _flashCanvas = canvasGo.AddComponent<Canvas>();
            _flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _flashCanvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            
            GameObject imageGo = new GameObject("FlashImage");
            imageGo.transform.SetParent(canvasGo.transform, false);
            _flashImage = imageGo.AddComponent<Image>();
            _flashImage.color = new Color(0, 0, 0, 0);
            _flashImage.raycastTarget = false;
            
            RectTransform rect = _flashImage.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void EnsureVolumeExists()
        {
            if (_localVolume != null)
                return;
            
            _localVolume = gameObject.AddComponent<Volume>();
            _localVolume.isGlobal = true;
            _localVolume.priority = 100;
            
            _localVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _localVolume.profile.name = "CameraModifiers_DynamicProfile";
            
            _dof = _localVolume.profile.Add<DepthOfField>();
            _dof.active = false;
            
            _chromatic = _localVolume.profile.Add<ChromaticAberration>();
            _chromatic.active = false;
        }

        // ══════════════════════════════════════════════════════════════
        // Screen Flash
        // ══════════════════════════════════════════════════════════════

        public void DoFlash(Color flashColor, float duration)
        {
            EnsureCanvasExists();
            
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
            
            _flashRoutine = StartCoroutine(FlashRoutine(flashColor, duration));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            float time = 0f;
            _flashImage.color = color;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, time / duration);
                _flashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            _flashImage.color = new Color(color.r, color.g, color.b, 0f);
        }

        // ══════════════════════════════════════════════════════════════
        // URP Post-Processing Effects
        // ══════════════════════════════════════════════════════════════

        public void DoDepthOfFieldFocus(float targetDistance, float duration, AnimationCurve curve)
        {
            EnsureVolumeExists();
            if (_dofRoutine != null)
                StopCoroutine(_dofRoutine);
            
            _dofRoutine = StartCoroutine(DofRoutine(targetDistance, duration, curve));
        }

        private IEnumerator DofRoutine(float targetDist, float duration, AnimationCurve curve)
        {
            _dof.active = true;
            _dof.mode.Override(DepthOfFieldMode.Bokeh);
            float startDist = _dof.focusDistance.value;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = curve != null ? curve.Evaluate(time / duration) : time / duration;
                _dof.focusDistance.Override(Mathf.Lerp(startDist, targetDist, t));
                yield return null;
            }
            _dof.focusDistance.Override(targetDist);
        }

        public void DoChromaticAberrationPulse(float intensity, float duration)
        {
            EnsureVolumeExists();
            if (_chromaticRoutine != null)
                StopCoroutine(_chromaticRoutine);
            
            _chromaticRoutine = StartCoroutine(ChromaticRoutine(intensity, duration));
        }

        private IEnumerator ChromaticRoutine(float intensity, float duration)
        {
            _chromatic.active = true;
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float t = 1f - (time / duration);
                _chromatic.intensity.Override(intensity * t);
                yield return null;
            }
            _chromatic.active = false;
        }

        public void SetAutoFocus(bool enabled, Transform target)
        {
            EnsureVolumeExists();
            _autoFocusEnabled = enabled;
            _autoFocusTarget = target;
            
            if (_dof != null)
                _dof.active = enabled;
        }

        private void LateUpdate()
        {
            if (_autoFocusEnabled && _autoFocusTarget != null && _dof != null)
            {
                float dist = Vector3.Distance(transform.position, _autoFocusTarget.position);
                _dof.focusDistance.Override(dist);
            }
        }

        public void DoHitStop(float duration, float timeScale)
        {
            if (_hitStopRoutine != null)
                StopCoroutine(_hitStopRoutine);
            
            _hitStopRoutine = StartCoroutine(HitStopRoutine(duration, timeScale));
        }

        private IEnumerator HitStopRoutine(float duration, float targetTimeScale)
        {
            float originalTimeScale = Time.timeScale;
            Time.timeScale = targetTimeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = originalTimeScale;
        }

        public void StopAll()
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
            
            if (_hitStopRoutine != null)
                StopCoroutine(_hitStopRoutine); Time.timeScale = 1f;
            
            if (_dofRoutine != null)
                StopCoroutine(_dofRoutine);
            
            if (_chromaticRoutine != null)
                StopCoroutine(_chromaticRoutine);

            if (_flashImage != null)
                _flashImage.color = new Color(0, 0, 0, 0);
            
            if (_dof != null)
                _dof.active = false;
            
            if (_chromatic != null)
                _chromatic.active = false;
        }

        private void OnDestroy()
        {
            if (_flashCanvas != null)
                Destroy(_flashCanvas.gameObject);
            
            if (_localVolume != null)
            {
                if (_localVolume.profile != null)
                    Destroy(_localVolume.profile);
                
                Destroy(_localVolume);
            }
            if (_hitStopRoutine != null)
                Time.timeScale = 1f; 
        }
    }
}
