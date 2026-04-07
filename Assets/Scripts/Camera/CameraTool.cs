using System;
using UnityEngine;
using UnityEngine.Playables;
using Dreamteck.Splines;
using NaughtyAttributes;
using Metroma.Camera.Timeline;
using UnityEngine.Events;
using System.Collections.Generic;
using Metroma.Camera.Modifiers;

namespace Metroma.Camera
{
    /// <summary> Defines a static camera view with position, rotation, FOV and distance offset. </summary>
    [System.Serializable]
    public struct CameraPose
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fov;
        public float distance;

        public static CameraPose Default => new CameraPose { position = Vector3.zero, rotation = Quaternion.identity, fov = 60f, distance = 0f };
    }

    public enum CameraState { FollowRail, Transitioning, StaticPose, ReturningToRail }
    public enum DirectorAction { None, Pause, Stop }

    [System.Serializable]
    public struct CameraChapter
    {
        public string name;
        public PlayableDirector director;
    }

    [DisallowMultipleComponent]
    public class CameraTool : MonoBehaviour, INotificationReceiver
    {
        // ── Service Access ───────────────────────────────────────────
        /// <summary> The currently active CameraTool in the scene. </summary>
        public static CameraTool Active { get; private set; }

        /// <summary> Triggered when a transition to a Pose starts. </summary>
        public event System.Action<CameraPose> OnTransitionStarted;
        
        // ── References ───────────────────────────────────────────────
        [Foldout("References")]
        [SerializeField] private List<SplineComputer> splineRails = new List<SplineComputer>();

        [Foldout("References")]
        [Required("Assign the Camera to drive along the spline.")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Foldout("References")]
        [SerializeField] private PlayableDirector playableDirector;

        [Foldout("References")]
        [Tooltip("Optional: default target to look at.")]
        [SerializeField] private Transform lookAtTarget;

        [Foldout("References")]
        [Tooltip("Optional: list of potential targets to look at. Switch via markers.")]
        [SerializeField] private List<Transform> lookAtTargets = new List<Transform>();

        [SerializeField] private List<CameraChapter> chapters = new List<CameraChapter>();

        // ── Events ───────────────────────────────────────────────────
        [Foldout("Events")]
        public UnityEvent<CameraChapter> onChapterStart;
        [Foldout("Events")]
        public UnityEvent<CameraChapter> onChapterEnd;

        public event Action<CameraState> OnStateChanged;
        public event Action<CameraChapter> OnChapterEventStarted;
        public event Action<CameraChapter> OnChapterEventActive;
        public event Action<CameraPose> OnPoseEventReached;
        public event Action<CameraMarkerBase> OnMarkerEventHit;

        // ── Animation Settings ───────────────────────────────────────
        [Foldout("Animation Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float splineProgress;

        [Foldout("Animation Settings")]
        [Range(0f, 1f)]
        [Tooltip("Blend between spline rotation (0) and LookAt rotation (1).")]
        [SerializeField] private float lookAtWeight = 1f;

        // ── Hidden / Serialized ──────────────────────────────────────
        [HideInInspector] public UnityEvent<string> onSplineNotified;
        [HideInInspector] [SerializeField] private List<CameraSplineSegment> segments = new List<CameraSplineSegment>();

        // ── Cached Runtime State ─────────────────────────────────────
        private Transform _cameraTransform;
        private bool _isInitialized;

        private SplineComputer[] _chainRails;
        private int[] _chainSegCounts;
        private int _chainTotalSegments;

        private bool _singleRailMode;
        private int _activeRailIndex;

        // ── Effects State ───────────────────────────────────────────
        private Transform _lookAtTarget;
        private Transform _lookAtFrom;
        private float _lookAtLerp;
        private float _lookAtDuration;

        // ── Pose Transition State ─────────────────────────────────────
        public CameraState State 
        { 
            get => _state; 
            private set
            {
                if (_state == value) return;
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }
        private CameraState _state = CameraState.FollowRail;
        private CameraChapter? _activeChapter;
        private CameraPose _targetPose;
        private CameraPose _startPose;
        private float _transitionTime;
        private float _transitionDuration;
        private AnimationCurve _transitionCurve;
        private float _transitionAlpha;
        private DirectorAction _directorActionOnArrival;

        // ══════════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════════

        #region Lifecycle

        private void Awake()
        {
            Active = this;
            CacheReferences();
            _lookAtTarget = lookAtTarget;
        }

        private void OnDisable()
        {
            CameraTimeHandler.ResetTimeScale();
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        private void LateUpdate()
        {
            if (!_isInitialized) return;

            UpdateEffects();
            UpdateTransition();
            EvaluateAndApply();
        }

        private void UpdateTransition()
        {
            if (State != CameraState.Transitioning) return;

            _transitionTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_transitionTime / _transitionDuration);
            _transitionAlpha = _transitionCurve != null ? _transitionCurve.Evaluate(t) : Mathf.SmoothStep(0, 1, t);

            if (t >= 1f)
            {
                if (State == CameraState.Transitioning)
                {
                    State = CameraState.StaticPose;
                    OnPoseEventReached?.Invoke(_targetPose);
                }
                else if (State == CameraState.ReturningToRail)
                {
                    State = CameraState.FollowRail;
                    
                    if (_activeChapter.HasValue)
                    {
                        var ch = _activeChapter.Value;
                        OnChapterEventActive?.Invoke(ch);
                        _activeChapter = null; // Consume
                    }
                }
            }
        }

        #endregion


        // ══════════════════════════════════════════════════════════════
        // Core Logic
        // ══════════════════════════════════════════════════════════════

        #region Core Logic

        private void CacheReferences()
        {
            if (targetCamera != null)
                _cameraTransform = targetCamera.transform;

            _isInitialized = HasValidRails() && _cameraTransform != null;

            if (_isInitialized)
                RebuildChainCache();
        }

        private bool HasValidRails()
        {
            if (splineRails == null || splineRails.Count == 0)
                return false;

            for (int i = 0; i < splineRails.Count; i++)
                if (splineRails[i] != null)
                    return true;

            return false;
        }

        private void RebuildChainCache()
        {
            int validCount = 0;
            for (int i = 0; i < splineRails.Count; i++)
                if (splineRails[i] != null)
                    validCount++;

            _chainRails = new SplineComputer[validCount];
            _chainSegCounts = new int[validCount];
            _chainTotalSegments = 0;

            int idx = 0;
            for (int i = 0; i < splineRails.Count; i++)
            {
                if (splineRails[i] == null)
                    continue;

                _chainRails[idx] = splineRails[i];
                _chainSegCounts[idx] = Mathf.Max(1, splineRails[i].pointCount - 1);
                _chainTotalSegments += _chainSegCounts[idx];
                idx++;
            }
        }

        private void UpdateEffects()
        {
            // ── LookAt Lerp ──
            if (_lookAtLerp < 1f && _lookAtDuration > 0)
            {
                _lookAtLerp += Time.deltaTime / _lookAtDuration;
            }
        }

        private void EvaluateAndApply()
        {
            // ── Rail Calculation ──
            SplineSample result = EvaluateChainAt(splineProgress);
            Vector3 railPos = (Vector3)result.position;
            Quaternion railRot = result.rotation;
            float railFov = targetCamera.fieldOfView;

            // ── LookAt Calculation ──
            Transform currentTarget = _lookAtTarget;
            if (currentTarget != null && lookAtWeight > 0.001f)
            {
                Vector3 targetPos = currentTarget.position;
                if (_lookAtLerp < 1f && _lookAtFrom != null)
                    targetPos = Vector3.Lerp(_lookAtFrom.position, currentTarget.position, Mathf.SmoothStep(0, 1, _lookAtLerp));

                Vector3 direction = targetPos - railPos;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction, (Vector3)result.up);
                    railRot = Quaternion.Slerp(result.rotation, lookRot, lookAtWeight);
                }
            }

            // ── Pose Blending ──
            Vector3 finalPos;
            Quaternion finalRotation;
            float finalFov;

            if (State == CameraState.FollowRail)
            {
                finalPos = railPos;
                finalRotation = railRot;
                finalFov = railFov;
            }
            else
            {
                // Calculate Target Pose including distance offset
                Vector3 targetRealPos = _targetPose.position + (_targetPose.rotation * Vector3.back * _targetPose.distance);
                
                if (State == CameraState.StaticPose)
                {
                    finalPos = targetRealPos;
                    finalRotation = _targetPose.rotation;
                    finalFov = _targetPose.fov;
                }
                else if (State == CameraState.Transitioning)
                {
                    finalPos = Vector3.Lerp(_startPose.position, targetRealPos, _transitionAlpha);
                    finalRotation = Quaternion.Slerp(_startPose.rotation, _targetPose.rotation, _transitionAlpha);
                    finalFov = Mathf.Lerp(_startPose.fov, _targetPose.fov, _transitionAlpha);
                }
                else // ReturningToRail
                {
                    finalPos = Vector3.Lerp(_startPose.position, railPos, _transitionAlpha);
                    finalRotation = Quaternion.Slerp(_startPose.rotation, railRot, _transitionAlpha);
                    finalFov = Mathf.Lerp(_startPose.fov, railFov, _transitionAlpha);
                }
            }

            _cameraTransform.SetPositionAndRotation(finalPos, finalRotation);
            targetCamera.fieldOfView = finalFov;
        }

        // ── Public API ──────────────────────────────────────────────
        
        /// <summary> Smoothly transitions the camera away from the rail to a fixed pose. </summary>
        public void TransitionToPose(CameraPose target, float duration, AnimationCurve curve = null, DirectorAction action = DirectorAction.None)
        {
            _startPose = new CameraPose
            {
                position = _cameraTransform.position,
                rotation = _cameraTransform.rotation,
                fov = targetCamera.fieldOfView,
                distance = 0f
            };

            _targetPose = target;
            _transitionDuration = duration;
            _transitionCurve = curve;
            _transitionTime = 0f;
            _directorActionOnArrival = action;

            if (_directorActionOnArrival == DirectorAction.Pause && playableDirector != null)
            {
                Debug.Log($"[CameraTool] Transition started. Pausing Timeline immediately: {playableDirector.name}", this);
                playableDirector.Pause();
            }
            else if (_directorActionOnArrival == DirectorAction.Stop && playableDirector != null)
            {
                Debug.Log($"[CameraTool] Transition started. Stopping Timeline immediately: {playableDirector.name}", this);
                playableDirector.Stop();
            }
            
            State = CameraState.Transitioning;
            OnTransitionStarted?.Invoke(target);
        }

        public void TransitionToPose(CameraPose target, float duration, AnimationCurve curve = null)
        {
            TransitionToPose(target, duration, curve, DirectorAction.Pause);
        }

        /// <summary> Returns the camera control back to the spline rail smoothly and resumes Timeline. </summary>
        public void ReturnToRail(float duration, AnimationCurve curve = null)
        {
            if (State == CameraState.FollowRail) return;

            _startPose = new CameraPose
            {
                position = _cameraTransform.position,
                rotation = _cameraTransform.rotation,
                fov = targetCamera.fieldOfView,
                distance = 0f
            };

            _transitionDuration = duration;
            _transitionCurve = curve;
            _transitionTime = 0f;

            if (playableDirector != null && playableDirector.state != PlayState.Playing)
            {
                Debug.Log($"[CameraTool] Resuming Timeline: {playableDirector.name}", this);
                playableDirector.Play();
            }

            State = CameraState.ReturningToRail;
        }

        /// <summary> Plays a specific chapter from the list by index, with a smooth transition. </summary>
        public void PlayChapter(int index, float blendDuration = 1.5f)
        {
            if (index < 0 || index >= chapters.Count)
            {
                Debug.LogError($"[CameraTool] Chapter index {index} out of range.");
                return;
            }

            var chapter = chapters[index];
            if (chapter.director == null)
            {
                Debug.LogError($"[CameraTool] Chapter '{chapter.name}' has no PlayableDirector assigned.");
                return;
            }

            // 1. Stop current director if any
            if (playableDirector != null && playableDirector.state == PlayState.Playing)
            {
                if (_activeChapter.HasValue)
                    onChapterEnd?.Invoke(_activeChapter.Value);

                playableDirector.Stop();
            }

            // 2. Set new active director
            playableDirector = chapter.director;

            // 3. Sample the first frame to get the target rail position
            playableDirector.time = 0;
            playableDirector.Evaluate();
            
            // Re-evaluate rail manually to ensure we have the frame 0 position
            EvaluateAndApply(); 

            // 4. Record target pose (where we want to be)
            CameraPose target = new CameraPose
            {
                position = _cameraTransform.position,
                rotation = _cameraTransform.rotation,
                fov = targetCamera.fieldOfView,
                distance = 0f
            };

            // 5. Jump back to "previous" state to start the blend
            _activeChapter = chapter;
            OnChapterEventStarted?.Invoke(chapter);
            onChapterStart?.Invoke(chapter);

            playableDirector.Play(); // Start it
            TransitionToPose(target, blendDuration, AnimationCurve.EaseInOut(0,0,1,1), DirectorAction.None);
        }

        public void PlayChapter(string chapterName, float blendDuration = 1.5f)
        {
            int idx = chapters.FindIndex(c => c.name.Equals(chapterName, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) PlayChapter(idx, blendDuration);
            else Debug.LogError($"[CameraTool] Chapter '{chapterName}' not found.");
        }

        private SplineSample EvaluateChainAt(float globalProgress)
        {
            if (_chainRails == null || _chainRails.Length == 0)
                return default;

            if (_singleRailMode)
            {
                int idx = Mathf.Clamp(_activeRailIndex, 0, _chainRails.Length - 1);
                return _chainRails[idx].Evaluate(Mathf.Clamp01(globalProgress));
            }

            if (_chainRails.Length == 1)
                return _chainRails[0].Evaluate(Mathf.Clamp01(globalProgress));

            if (_chainTotalSegments == 0)
                return _chainRails[0].Evaluate(Mathf.Clamp01(globalProgress));

            float segWidth = 1f / _chainTotalSegments;
            int targetSeg = Mathf.Clamp((int)(globalProgress / segWidth), 0, _chainTotalSegments - 1);
            float segLocalT = Mathf.Clamp01((globalProgress - targetSeg * segWidth) / segWidth);

            int running = 0;
            for (int r = 0; r < _chainRails.Length; r++)
            {
                int count = _chainSegCounts[r];
                if (targetSeg < running + count)
                {
                    int localSeg = targetSeg - running;
                    float localStart = (float)localSeg / count;
                    float localEnd = (float)(localSeg + 1) / count;
                    float localProgress = Mathf.Lerp(localStart, localEnd, segLocalT);
                    return _chainRails[r].Evaluate(Mathf.Clamp01(localProgress));
                }
                running += count;
            }

            return _chainRails[_chainRails.Length - 1].Evaluate(1f);
        }

        #endregion


        // ══════════════════════════════════════════════════════════════
        // Timeline Signals
        // ══════════════════════════════════════════════════════════════

        #region Timeline Signal Handlers

        public void TriggerTimelineEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return;
            
            string lowerEvent = eventName.ToLower();

            // 1. Rail switching
            if (lowerEvent.StartsWith("rail:") && int.TryParse(eventName.Substring(5), out int railIndex))
            {
                SwitchToRail(railIndex);
                return;
            }

            // 2. HitStop
            if (lowerEvent.StartsWith("hitstop:") && float.TryParse(eventName.Substring(8), out float duration))
            {
                targetCamera.DoHitStop(duration);
                return;
            }

            // 3. Flash
            if (lowerEvent.StartsWith("flash:"))
            {
                targetCamera.DoFlash(Color.white, 0.5f);
                return;
            }

            // 4. Handheld Toggle
            if (lowerEvent == "handheld:on")
            {
                targetCamera.SetHandheld(true);
                return;
            }

            if (lowerEvent == "handheld:off")
            {
                targetCamera.SetHandheld(false);
                return;
            }

            // 5. Wobble
            if (lowerEvent == "wobble")
            {
                targetCamera.DoWobble(5f, 1f, 2f);
                return;
            }

            Debug.Log($"[CameraTool] Generic event handled: '{eventName}'", this);
        }

        public void HandleLookAtSwitch(CameraLookAtSwitchMarker m)
        {
            int idx = m.TargetIndex;
            Transform next = (idx >= 0 && idx < lookAtTargets.Count) ? lookAtTargets[idx] : null;

            if (_lookAtTarget == next)
                return;

            _lookAtFrom = _lookAtTarget;
            _lookAtTarget = next;
            _lookAtDuration = m.TransitionDuration;
            _lookAtLerp = (_lookAtDuration > 0) ? 0f : 1f;

            Debug.Log($"[CameraTool] LookAt switched to: {(next != null ? next.name : "None")}", this);
        }

        #endregion


        #region INotificationReceiver

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            // Auto-capture or refresh the director from the notifier
            if (origin.IsValid())
            {
                var director = origin.GetGraph().GetResolver() as PlayableDirector;
                if (director != null && playableDirector != director)
                {
                    playableDirector = director;
                    Debug.Log($"[CameraTool] Switched/Captured active PlayableDirector: {playableDirector.name}", this);
                }
            }

            // NEW COMMAND PATTERN: All CameraMarkerBase markers execute their own logic.
            if (notification is CameraMarkerBase cameraMarker)
            {
                cameraMarker.Execute(this);
                OnMarkerEventHit?.Invoke(cameraMarker);
            }
            // Fallback for generic event markers or third party notifications
            else if (notification is CameraEventMarker marker)
            {
                TriggerTimelineEvent(marker.EventName);
                onSplineNotified?.Invoke(marker.EventName);
            }
        }

        #endregion


        // ══════════════════════════════════════════════════════════════
        // Public API
        // ══════════════════════════════════════════════════════════════

        #region Public API

        public float SplineProgress { get => splineProgress; set => splineProgress = Mathf.Clamp01(value); }
        public float LookAtWeight { get => lookAtWeight; set => lookAtWeight = Mathf.Clamp01(value); }

        public UnityEngine.Camera TargetCamera => targetCamera;
        public List<Transform> LookAtTargets => lookAtTargets;
        public Transform CurrentLookAtTarget => _lookAtTarget;

        public void SwitchToRail(int railIndex)
        {
            if (_chainRails == null || railIndex < 0 || railIndex >= _chainRails.Length)
                return;

            _singleRailMode = true;
            _activeRailIndex = railIndex;
        }

        public void ResetToChainMode() { _singleRailMode = false; }

        public void SetTargetCamera(UnityEngine.Camera newCamera)
        {
            if (newCamera == null)
                return;
                
            targetCamera = newCamera;
            CacheReferences();
        }

        #endregion


        // ══════════════════════════════════════════════════════════════
        // Editor
        // ══════════════════════════════════════════════════════════════

#if UNITY_EDITOR

        public List<SplineComputer> EditorSplineRails => splineRails;
        public UnityEngine.Camera EditorCamera => targetCamera;
        public PlayableDirector EditorDirector => playableDirector;
        public Transform EditorLookAtTarget => lookAtTarget;
        public List<Transform> EditorLookAtTargets => lookAtTargets;
        public Transform EditorCurrentLookAt => _lookAtTarget;
        public List<CameraSplineSegment> EditorSegments => segments;

        public int EditorRailCount
        {
            get
            {
                if (splineRails == null) 
                    return 0;
                
                int c = 0;
                for (int i = 0; i < splineRails.Count; i++)
                {
                    if (splineRails[i]) 
                        c++;
                }

                return c;
            }
        }

        public int EditorPointCount
        {
            get
            {
                if (splineRails == null) 
                    return 0;
                
                int t = 0; 
                for (int i=0; i<splineRails.Count; i++) 
                {
                    if (splineRails[i]) 
                        t += splineRails[i].pointCount;
                }
                
                return t;
            }
        }

        public int EditorTotalSegmentCount
        {
            get
            {
                if (splineRails == null)
                    return 0;
                
                int t = 0;
                for (int i=0; i<splineRails.Count; i++)
                {
                    if (splineRails[i]) 
                        t += Mathf.Max(0, splineRails[i].pointCount - 1);
                }
                
                return t;
            }
        }

        public void EditorEvaluateAt(float progress)
        {
            if (!HasValidRails() || !targetCamera)
                return;
            
            splineProgress = Mathf.Clamp01(progress);
            RebuildChainCache();
            SplineSample result = EvaluateChainAt(splineProgress);
            Quaternion rot = result.rotation;
            
            Transform activeTarget = _lookAtTarget ? _lookAtTarget : lookAtTarget;
            if (activeTarget && lookAtWeight > 0.001f)
            {
                Vector3 dir = activeTarget.position - result.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    rot = Quaternion.Slerp(result.rotation, Quaternion.LookRotation(dir, result.up), lookAtWeight);
                }
            }
            targetCamera.transform.SetPositionAndRotation(result.position, rot);
        }

        public SplineSample EditorSampleAt(float progress)
        {
            if (!HasValidRails())
                return new SplineSample();
            
            RebuildChainCache();
            return EvaluateChainAt(Mathf.Clamp01(progress));
        }

        private int _lastKnownPointCount = -1;
        private void OnValidate()
        {
            splineProgress = Mathf.Clamp01(splineProgress);
            lookAtWeight = Mathf.Clamp01(lookAtWeight);
            
            int cp = EditorPointCount;
            if (_lastKnownPointCount != cp && cp > 0)
            {
                _lastKnownPointCount = cp;
                if (segments.Count != EditorTotalSegmentCount)
                    EditorSyncSegments();
            }
        }

        public void EditorSyncSegments()
        {
            if (splineRails == null)
                return;
            
            int total = EditorTotalSegmentCount;
            while (segments.Count > total)
            {
                segments.RemoveAt(segments.Count - 1);
            }
            
            while (segments.Count < total) 
                segments.Add(new CameraSplineSegment
                {
                    duration = 1f, easing = AnimationCurve.EaseInOut(0, 0, 1, 1)
                });
            
            int idx = 0;
            for (int r = 0; r < splineRails.Count; r++)
            {
                if (!splineRails[r])
                    continue;
                
                string prefix = splineRails.Count > 1 ? $"R{r} " : "";
                
                for (int n = 0; n < splineRails[r].pointCount - 1; n++) 
                {
                    if (idx < segments.Count)
                        segments[idx++].label = $"{prefix}Node {n} → {n + 1}";
                }
            }
        }
#endif
    }
}
