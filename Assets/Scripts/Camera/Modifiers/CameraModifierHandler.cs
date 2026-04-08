using UnityEngine;
using UnityEngine.InputSystem;

namespace Metroma.CameraTool.Modifiers
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    [DefaultExecutionOrder(100)]
    public class CameraModifierHandler : MonoBehaviour
    {
        private UnityEngine.Camera _cam;

        [Header("Haptics")]
        public bool enableGamepadHaptics = true;
        [Range(0f, 2.0f)] public float hapticMultiplier = 0.5f;

        [Tooltip("If true, all effects (shakes, etc.) run at full speed even if the game is slowed down via Time.timeScale.")]
        public bool useUnscaledTime = true;

        // ── Effect Data Structures ──
        private struct ShakeData { public float intensity, duration, roughness, time; public bool fadeOut, active; }
        private struct ImpactData { public Vector3 direction; public float intensity, duration, time; public bool active; }
        private struct PosOffsetData { public Vector3 offset; public float mainDuration, returnDuration, time; public AnimationCurve mainCurve, returnCurve; public bool invertReturn, active; }
        private struct RotOffsetData { public Vector3 euler; public float mainDuration, returnDuration, time; public AnimationCurve mainCurve, returnCurve; public bool invertReturn, active; }
        private struct WobbleData { public float amplitude, frequency, duration, time; public bool active; }
        private struct TempLookAtData { public Transform target; public float duration, time; public AnimationCurve curve; public bool active; }

        // ── Pools (Array-based, zero alloc) ──
        private ShakeData[] _shakes = new ShakeData[4];
        private ImpactData[] _impacts = new ImpactData[4];
        private PosOffsetData[] _posOffsets = new PosOffsetData[4];
        private RotOffsetData[] _rotOffsets = new RotOffsetData[4];
        private WobbleData[] _wobbles = new WobbleData[2];
        private TempLookAtData _tempLookAt;

        private bool _handheldActive;
        private float _handheldAmp, _handheldFreq;

        // ── FOV Transitions ──
        private bool _fovActive;
        private float _fovStart, _fovTarget, _fovTime, _fovDuration;
        private AnimationCurve _fovCurve;

        private bool _fovPulseActive;
        private float _fovPulseAmp, _fovPulseFreq, _fovPulseTime, _fovPulseDur;
        private float _baseFov;

        private bool _dollyActive;
        private float _dollyDist, _dollyFovTarget, _dollyTime, _dollyDur;
        private AnimationCurve _dollyCurve;
        private Vector3 _dollyStartPos;
        private float _dollyStartFov;

        // ── Transform Tracking ──
        private Vector3 _lastWorldPos;
        private Quaternion _lastWorldRot;
        private Vector3 _appliedLocalPos;
        private Quaternion _appliedLocalRot = Quaternion.identity;

        // ── Clear Routine ──
        private bool _clearing;
        private float _clearTime, _clearDuration;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _baseFov = _cam.fieldOfView;
            
            _lastWorldPos = transform.position;
            _lastWorldRot = transform.rotation;
        }

        // ══════════════════════════════════════════════════════════════
        // API (Transform & FOV)
        // ══════════════════════════════════════════════════════════════

        public void AddShake(float intensity, float duration, float roughness, bool fadeOut)
        {
            for (int i = 0; i < _shakes.Length; i++)
                if (!_shakes[i].active)
                {
                    _shakes[i] = new ShakeData { intensity = intensity, duration = duration, roughness = roughness, fadeOut = fadeOut, time = 0f, active = true };
                    return;
                }
        }

        public void AddImpact(Vector3 direction, float intensity, float duration)
        {
            for (int i = 0; i < _impacts.Length; i++)
                if (!_impacts[i].active)
                {
                    _impacts[i] = new ImpactData { direction = direction.normalized, intensity = intensity, duration = duration, time = 0f, active = true };
                    return;
                }
        }

        /// <summary> Adds a smooth position offset using a dual-phase (attack/return) animation. </summary>
        public void AddPositionOffset(Vector3 offset, float mainDuration, AnimationCurve mainCurve, float returnDuration, AnimationCurve returnCurve, bool invertReturn)
        {
            for (int i = 0; i < _posOffsets.Length; i++)
            {
                if (!_posOffsets[i].active)
                {
                    _posOffsets[i] = new PosOffsetData { 
                        offset = offset, 
                        mainDuration = mainDuration, 
                        returnDuration = returnDuration, 
                        time = 0f, 
                        mainCurve = mainCurve, 
                        returnCurve = returnCurve, 
                        invertReturn = invertReturn,
                        active = true 
                    };
                    return;
                }
            }
        }

        public void AddPositionOffset(Vector3 offset, float duration, AnimationCurve curve)
        {
            AddPositionOffset(offset, duration, curve, 0.25f, AnimationCurve.Linear(0, 1, 1, 0), false);
        }

        /// <summary> Adds a smooth rotation offset (Euler) using a dual-phase (attack/return) animation. </summary>
        public void AddRotationOffset(Vector3 euler, float mainDuration, AnimationCurve mainCurve, float returnDuration, AnimationCurve returnCurve, bool invertReturn)
        {
            for (int i = 0; i < _rotOffsets.Length; i++)
            {
                if (!_rotOffsets[i].active)
                {
                    _rotOffsets[i] = new RotOffsetData { 
                        euler = euler, 
                        mainDuration = mainDuration, 
                        returnDuration = returnDuration, 
                        time = 0f, 
                        mainCurve = mainCurve, 
                        returnCurve = returnCurve, 
                        invertReturn = invertReturn,
                        active = true 
                    };
                    return;
                }
            }
        }

        public void AddRotationOffset(Vector3 euler, float duration, AnimationCurve curve)
        {
            AddRotationOffset(euler, duration, curve, 0.25f, AnimationCurve.Linear(0, 1, 1, 0), false);
        }

        public void SetHandheld(bool active, float amplitude = 1f, float frequency = 1f)
        {
            _handheldActive = active;
            _handheldAmp = amplitude;
            _handheldFreq = frequency;
        }

        public void AddWobble(float amplitude, float frequency, float duration)
        {
            for (int i = 0; i < _wobbles.Length; i++)
                if (!_wobbles[i].active)
                {
                    _wobbles[i] = new WobbleData { amplitude = amplitude, frequency = frequency, duration = duration, time = 0f, active = true };
                    return;
                }
        }

        public void SetTemporaryLookAt(Transform target, float duration, AnimationCurve focusCurve)
        {
            _tempLookAt = new TempLookAtData { target = target, duration = duration, time = 0f, curve = focusCurve, active = true };
        }

        public void SetFovTransition(float targetFOV, float duration, AnimationCurve curve)
        {
            _fovActive = true;
            _fovStart = _cam.fieldOfView;
            _fovTarget = targetFOV;
            _fovDuration = duration;
            _fovTime = 0f;
            _fovCurve = curve;
            _baseFov = targetFOV;
        }

        public void SetFovPulse(float amplitude, float frequency, float duration)
        {
            _fovPulseActive = true;
            _fovPulseAmp = amplitude;
            _fovPulseFreq = frequency;
            _fovPulseDur = duration;
            _fovPulseTime = 0f;
        }

        public void SetDollyZoom(float pushDistance, float targetFOV, float duration, AnimationCurve curve)
        {
            _dollyActive = true;
            _dollyDist = pushDistance;
            _dollyFovTarget = targetFOV;
            _dollyDur = duration;
            _dollyTime = 0f;
            _dollyCurve = curve;
            _dollyStartPos = transform.position;
            _dollyStartFov = _cam.fieldOfView;
        }

        public void StopAll()
        {
            for (int i = 0; i < _shakes.Length; i++) 
                _shakes[i].active = false;
            
            for (int i = 0; i < _impacts.Length; i++) 
                _impacts[i].active = false;
            
            for (int i = 0; i < _posOffsets.Length; i++) 
                _posOffsets[i].active = false;
            
            for (int i = 0; i < _rotOffsets.Length; i++)
                _rotOffsets[i].active = false;
            
            for (int i = 0; i < _wobbles.Length; i++)
                _wobbles[i].active = false;
            
            _fovActive = _fovPulseActive = _dollyActive = _tempLookAt.active = _clearing = _handheldActive = false;
            
            RevertBaseTransform();
            _appliedLocalPos = Vector3.zero;
            _appliedLocalRot = Quaternion.identity;
        }

        public void ClearOffsetsSmoothly(float duration)
        {
            _clearing = true;
            _clearDuration = duration;
            _clearTime = 0f;
        }

        // ══════════════════════════════════════════════════════════════
        // Execution Loop
        // ══════════════════════════════════════════════════════════════

        private void LateUpdate()
        {
            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; 

            if (_clearing)
            {
                _clearTime += dt;
                float t = Mathf.Clamp01(_clearTime / _clearDuration);
                _appliedLocalPos = Vector3.Lerp(_appliedLocalPos, Vector3.zero, t);
                _appliedLocalRot = Quaternion.Slerp(_appliedLocalRot, Quaternion.identity, t);

                if (t >= 1f)
                {
                    StopAll();
                }
                
                return;
            }

            Vector3 basePos = transform.position;
            Quaternion baseRot = transform.rotation;

            bool movedExternally = (basePos - _lastWorldPos).sqrMagnitude > 0.0001f || Quaternion.Angle(baseRot, _lastWorldRot) > 0.1f;
            
            if (!movedExternally && !_dollyActive)
            {
                basePos = transform.TransformPoint(-_appliedLocalPos);
                baseRot = transform.rotation * Quaternion.Inverse(_appliedLocalRot);
            }
            else if (_dollyActive)
            {
                basePos = _dollyStartPos;
            }

            if (_tempLookAt.active && _tempLookAt.target != null)
            {
                _tempLookAt.time += dt;
                float t = Mathf.Clamp01(_tempLookAt.time / _tempLookAt.duration);
                float weight = _tempLookAt.curve != null ? _tempLookAt.curve.Evaluate(t) : (Mathf.Sin(t * Mathf.PI)); // default 0->1->0 curve
                
                Vector3 targetDir = _tempLookAt.target.position - basePos;
                if (targetDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(targetDir, baseRot * Vector3.up);
                    baseRot = Quaternion.Slerp(baseRot, targetRot, weight);
                }

                if (_tempLookAt.time >= _tempLookAt.duration) _tempLookAt.active = false;
            }

            Vector3 locPos = Vector3.zero;
            Vector3 locRot = Vector3.zero;

            // Handheld Breathing
            if (_handheldActive)
            {
                float sx = Time.time * _handheldFreq;
                float sy = Time.time * _handheldFreq + 100f;
                
                locRot += new Vector3(
                     (Mathf.PerlinNoise(sx, 0f) - 0.5f) * 2f,
                     (Mathf.PerlinNoise(0f, sy) - 0.5f) * 2f, 0f
                ) * _handheldAmp;
                
                locPos += new Vector3(
                    (Mathf.PerlinNoise(sx, sy) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(sy, sx) - 0.5f) * 2f, 0f
                ) * (_handheldAmp * 0.1f);
            }

            // Wobbles (Z axis sine)
            for (int i = 0; i < _wobbles.Length; i++)
            {
                ref WobbleData w = ref _wobbles[i];
                if (!w.active) continue;
                w.time += dt;
                float fade = 1f - Mathf.Clamp01(w.time / w.duration);
                locRot += new Vector3(0, 0, Mathf.Sin(w.time * w.frequency * Mathf.PI * 2f) * w.amplitude * fade);
                if (w.time >= w.duration) w.active = false;
            }

            // Shakes
            float totalIntensity = 0f;
            for (int i = 0; i < _shakes.Length; i++)
            {
                ref ShakeData s = ref _shakes[i];
                if (!s.active) continue;

                s.time += dt;
                float fade = s.fadeOut ? (1f - Mathf.Clamp01(s.time / s.duration)) : 1f;
                float seed = Time.time * s.roughness * 10f + i * 100f;

                float frameIntensity = s.intensity * fade;
                totalIntensity += frameIntensity;
                
                locPos += new Vector3(
                    (Mathf.PerlinNoise(seed, 0f) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(0f, seed) - 0.5f) * 2f,
                    (Mathf.PerlinNoise(seed, seed) - 0.5f) * 2f
                ) * frameIntensity;

                if (s.time >= s.duration) s.active = false;
            }

            if (enableGamepadHaptics && Application.isPlaying)
            {
                UpdateHaptics(totalIntensity);
            }

            // Impacts
            for (int i = 0; i < _impacts.Length; i++)
            {
                ref ImpactData d = ref _impacts[i];
                if (!d.active) continue;

                d.time += dt;
                float t = Mathf.Clamp01(d.time / d.duration);
                float spring = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 3f);

                locPos += d.direction * (d.intensity * spring);

                if (d.time >= d.duration)
                    d.active = false;
            }

            // Pos Offsets
            for (int i = 0; i < _posOffsets.Length; i++)
            {
                ref PosOffsetData p = ref _posOffsets[i];
                if (!p.active)
                    continue;

                p.time += dt;
                float weight = 0f;

                // Main Movement
                if (p.time <= p.mainDuration)
                {
                    float t = Mathf.Clamp01(p.time / p.mainDuration);
                    weight = p.mainCurve != null ? p.mainCurve.Evaluate(t) : (1f - t);
                }
                // Return to Base
                else
                {
                    float t = Mathf.Clamp01((p.time - p.mainDuration) / p.returnDuration);
                    float lastMainWeight = p.mainCurve != null ? p.mainCurve.Evaluate(1f) : 1f;
                    float returnFactor = p.returnCurve != null ? p.returnCurve.Evaluate(t) : (1f - t);
                    
                    if (p.invertReturn)
                        returnFactor = 1f - returnFactor;
                    
                    weight = lastMainWeight * returnFactor;
                }

                locPos += p.offset * weight;

                if (p.time >= p.mainDuration + p.returnDuration)
                    p.active = false;
            }

            // Rot Offsets
            for (int i = 0; i < _rotOffsets.Length; i++)
            {
                ref RotOffsetData r = ref _rotOffsets[i];
                if (!r.active)
                    continue;

                r.time += dt;
                float weight = 0f;

                // Main Movement
                if (r.time <= r.mainDuration)
                {
                    float t = Mathf.Clamp01(r.time / r.mainDuration);
                    weight = r.mainCurve != null ? r.mainCurve.Evaluate(t) : (1f - t);
                }
                // Return to Base
                else
                {
                    float t = Mathf.Clamp01((r.time - r.mainDuration) / r.returnDuration);
                    float lastMainWeight = r.mainCurve != null ? r.mainCurve.Evaluate(1f) : 1f;
                    float returnFactor = r.returnCurve != null ? r.returnCurve.Evaluate(t) : (1f - t);
                    
                    if (r.invertReturn) returnFactor = 1f - returnFactor;
                    
                    weight = lastMainWeight * returnFactor;
                }

                locRot += r.euler * weight;

                if (r.time >= r.mainDuration + r.returnDuration)
                    r.active = false;
            }

            // Dolly Zoom
            if (_dollyActive)
            {
                _dollyTime += dt;
                float t = Mathf.Clamp01(_dollyTime / _dollyDur);
                float weight = _dollyCurve != null ? _dollyCurve.Evaluate(t) : t;

                locPos += Vector3.forward * (_dollyDist * weight);
                _cam.fieldOfView = Mathf.Lerp(_dollyStartFov, _dollyFovTarget, weight);

                if (_dollyTime >= _dollyDur)
                    _dollyActive = false;
            }
            
            // 3. Update FOV
            if (!_dollyActive)
            {
                float currentFov = _cam.fieldOfView;
                if (_fovActive)
                {
                    _fovTime += dt;
                    float t = Mathf.Clamp01(_fovTime / _fovDuration);
                    float w = _fovCurve != null ? _fovCurve.Evaluate(t) : t;
                    currentFov = Mathf.Lerp(_fovStart, _fovTarget, w);
                    
                    if (_fovTime >= _fovDuration)
                        _fovActive = false;
                }
                if (_fovPulseActive)
                {
                    _fovPulseTime += dt;
                    float t = Mathf.Clamp01(_fovPulseTime / _fovPulseDur);
                    float pulse = Mathf.Sin(_fovPulseTime * Mathf.PI * 2f * _fovPulseFreq) * (1f - t);
                    currentFov = _baseFov + (_fovPulseAmp * pulse);
                    
                    if (_fovPulseTime >= _fovPulseDur)
                        _fovPulseActive = false;
                }
                _cam.fieldOfView = currentFov;
            }

            
            // Apply
            _appliedLocalPos = locPos;
            _appliedLocalRot = Quaternion.Euler(locRot);

            transform.position = basePos + baseRot * _appliedLocalPos;
            transform.rotation = baseRot * _appliedLocalRot;

            _lastWorldPos = transform.position;
            _lastWorldRot = transform.rotation;
        }

        private void RevertBaseTransform()
        {
            if (_appliedLocalPos != Vector3.zero || _appliedLocalRot != Quaternion.identity)
            {
                transform.position = transform.TransformPoint(-_appliedLocalPos);
                transform.rotation = transform.rotation * Quaternion.Inverse(_appliedLocalRot);
            }
        }

        private void UpdateHaptics(float intensity)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
                return;
            
            float motorLevel = Mathf.Clamp01(intensity * hapticMultiplier);
            gamepad.SetMotorSpeeds(motorLevel * 0.8f, motorLevel); // Balanced low/high freq
        }

        private void StopHaptics()
        {
            if (Gamepad.current != null)
                Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        private void OnDisable() => StopHaptics();
        private void OnDestroy() => StopHaptics();
    }
}
