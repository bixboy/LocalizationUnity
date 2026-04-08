using System;
using UnityEngine;
using UnityEngine.Playables;
using Dreamteck.Splines;
using NaughtyAttributes;
using Metroma.CameraTool.Timeline;
using UnityEngine.Events;
using System.Collections.Generic;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool
{
    
    [DisallowMultipleComponent]
    public class CameraTool : MonoBehaviour, INotificationReceiver
    {
        
        #region Service Access
        
        /// <summary> The currently active CameraTool instance in the scene. </summary>
        public static CameraTool Active { get; private set; }

        /// <summary> Triggered when a camera transition starts. </summary>
        public event Action<CameraPose> OnTransitionStarted;
        
        #endregion

        
        #region Serialized Fields
        
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

        [Foldout("Chapters")]
        [SerializeField] private List<CameraChapter> chapters = new List<CameraChapter>();

        [Foldout("Events")]
        public UnityEvent<CameraChapter> onChapterStart;
        [Foldout("Events")]
        public UnityEvent<CameraChapter> onChapterEnd;
        [Foldout("Events")]
        public UnityEvent<string> onSplineNotified;

        public event Action<CameraState> OnStateChanged;
        public event Action<CameraChapter> OnChapterStarted;
        public event Action<CameraChapter> OnChapterActive;
        public event Action<CameraPose> OnPoseEventReached;
        public event Action<CameraMarkerBase> OnMarkerEventHit;

        [Foldout("Animation Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float splineProgress;

        [Foldout("Animation Settings")]
        [Range(0f, 1f)]
        [Tooltip("Blend between spline rotation (0) and LookAt rotation (1).")]
        [SerializeField] private float lookAtWeight = 1f;

        [HideInInspector] [SerializeField] private List<CameraSplineSegment> segments = new List<CameraSplineSegment>();
        
        #endregion

        
        #region Runtime State
        
        private Transform _cameraTransform;
        private bool _isInitialized;

        private SplineComputer[] _chainRails;
        private int[] _chainSegCounts;
        private int _chainTotalSegments;

        private bool _singleRailMode;
        private int _activeRailIndex;

        private Transform _lookAtTarget;
        private Transform _lookAtFrom;
        private float _lookAtLerp;
        private float _lookAtDuration;

        private CameraState _state = CameraState.FollowRail;
        private CameraChapter? _activeChapter;
        private CameraPose _targetPose;
        private CameraPose _startPose;
        private float _transitionTime;
        private float _transitionDuration;
        private AnimationCurve _transitionCurve;
        private float _transitionAlpha;
        private DirectorAction _arrivalAction;

        public CameraState State 
        { 
            get => _state; 
            private set
            {
                if (_state == value)
                    return;
                
                _state = value;
                OnStateChanged?.Invoke(_state);
            }
        }
        #endregion

        
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
            if (Active == this)
                Active = null;
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
                return;

            UpdateEffectsTimers();
            UpdateTransitionState();

            CameraPose railPose = SampleRailPose();

            ApplyCameraPose(railPose);
        }
        #endregion

        
        #region Internal Logic
        
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
            
            foreach (var r in splineRails)
            {
                if (r)
                    return true;
            }
            
            return false;
        }

        private void RebuildChainCache()
        {
            int validCount = 0;
            foreach (var r in splineRails)
            {
                if (r)
                    validCount++;
            }

            _chainRails = new SplineComputer[validCount];
            _chainSegCounts = new int[validCount];
            _chainTotalSegments = 0;

            int idx = 0;
            foreach (var r in splineRails)
            {
                if (!r)
                    continue;
                
                _chainRails[idx] = r;
                _chainSegCounts[idx] = Mathf.Max(1, r.pointCount - 1);
                _chainTotalSegments += _chainSegCounts[idx];
                idx++;
            }
        }

        private void UpdateEffectsTimers()
        {
            if (_lookAtLerp < 1f && _lookAtDuration > 0)
            {
                _lookAtLerp += Time.deltaTime / _lookAtDuration;
            }
        }

        private void UpdateTransitionState()
        {
            if (State != CameraState.Transitioning && State != CameraState.ReturningToRail)
                return;

            _transitionTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_transitionTime / _transitionDuration);
            _transitionAlpha = _transitionCurve != null ? _transitionCurve.Evaluate(t) : Mathf.SmoothStep(0, 1, t);

            if (t >= 1f)
            {
                HandleTransitionArrival();
            }
        }

        private void HandleTransitionArrival()
        {
            if (State == CameraState.Transitioning)
            {
                State = CameraState.StaticPose;
                
                if (playableDirector)
                {
                    if (_arrivalAction == DirectorAction.Pause)
                    {
                        playableDirector.Pause();
                    }
                    else if (_arrivalAction == DirectorAction.Stop)
                    {
                        playableDirector.Stop();
                    }
                }

                OnPoseEventReached?.Invoke(_targetPose);
            }
            else if (State == CameraState.ReturningToRail)
            {
                State = CameraState.FollowRail;
                
                if (_activeChapter.HasValue)
                {
                    OnChapterActive?.Invoke(_activeChapter.Value);
                    _activeChapter = null; // Consume
                }
            }

            _arrivalAction = DirectorAction.None;
        }

        /// <summary> Decoupled sampling of the current rail position/rotation/fov. </summary>
        private CameraPose SampleRailPose()
        {
            SplineSample result = EvaluateChainAt(splineProgress);
            
            CameraPose pose = new CameraPose
            {
                position = result.position,
                rotation = result.rotation,
                fov = targetCamera.fieldOfView,
                distance = 0f
            };

            // Apply LookAt logic
            Transform currentTarget = _lookAtTarget;
            if (currentTarget && lookAtWeight > 0.001f)
            {
                Vector3 targetPos = currentTarget.position;
                if (_lookAtLerp < 1f && _lookAtFrom)
                    targetPos = Vector3.Lerp(_lookAtFrom.position, currentTarget.position, Mathf.SmoothStep(0, 1, _lookAtLerp));

                Vector3 direction = targetPos - pose.position;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(direction, result.up);
                    pose.rotation = Quaternion.Slerp(pose.rotation, lookRot, lookAtWeight);
                }
            }

            return pose;
        }

        /// <summary> Final application and blending to camera transform. </summary>
        private void ApplyCameraPose(CameraPose railPose)
        {
            CameraPose finalPose = railPose;

            switch (State)
            {
                case CameraState.Transitioning:
                    finalPose = CameraPose.Lerp(railPose, _targetPose, _transitionAlpha);
                    break;
                
                case CameraState.ReturningToRail:
                    finalPose = CameraPose.Lerp(_startPose, railPose, _transitionAlpha);
                    break;
                
                case CameraState.StaticPose:
                    finalPose = _targetPose;
                    break;
            }

            _cameraTransform.SetPositionAndRotation(finalPose.position, finalPose.rotation);
            targetCamera.fieldOfView = finalPose.fov;

            if (finalPose.distance != 0)
                _cameraTransform.position -= _cameraTransform.forward * finalPose.distance;
        }

        private SplineSample EvaluateChainAt(float globalProgress)
        {
            if (_chainRails == null || _chainRails.Length == 0)
                return default;

            if (_singleRailMode)
            {
                int rIdx = Mathf.Clamp(_activeRailIndex, 0, _chainRails.Length - 1);
                return _chainRails[rIdx].Evaluate(Mathf.Clamp01(globalProgress));
            }

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

        
        #region Public API
        
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
            _arrivalAction = action;

            if (playableDirector)
            {
                if (_arrivalAction == DirectorAction.Pause)
                {
                    playableDirector.Pause();
                }
                else if (_arrivalAction == DirectorAction.Stop)
                {
                    playableDirector.Stop();
                }
            }
            
            State = CameraState.Transitioning;
            OnTransitionStarted?.Invoke(target);
        }

        /// <summary> Returns control to the spline rail from a static pose. </summary>
        public void ReturnToRail(float duration, AnimationCurve curve = null)
        {
            if (State == CameraState.FollowRail)
                return;

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
                playableDirector.Play();
            }

            State = CameraState.ReturningToRail;
        }

        /// <summary> Plays a specific chapter with automated rail capture and smooth transition. </summary>
        public void PlayChapter(int index, float blendDuration = 1.5f)
        {
            if (index < 0 || index >= chapters.Count)
                return;

            var chapter = chapters[index];
            if (!chapter.director)
                return;

            if (_activeChapter.HasValue) 
                onChapterEnd?.Invoke(_activeChapter.Value);

            if (playableDirector && playableDirector.state == PlayState.Playing)
                playableDirector.Stop();

            playableDirector = chapter.director;
            playableDirector.time = 0;
            playableDirector.Evaluate();
            
            CameraPose targetPose = SampleRailPose();

            _activeChapter = chapter;
            OnChapterStarted?.Invoke(chapter);
            onChapterStart?.Invoke(chapter);

            playableDirector.Play();
            TransitionToPose(targetPose, blendDuration, AnimationCurve.EaseInOut(0, 0, 1, 1));
        }

        public void PlayChapter(string chapterName, float blendDuration = 1.5f)
        {
            int idx = chapters.FindIndex(c => c.name.Equals(chapterName, StringComparison.OrdinalIgnoreCase));
            
            if (idx >= 0)
                PlayChapter(idx, blendDuration);
        }

        public void SwitchToRail(int idx)
        {
            _singleRailMode = true; _activeRailIndex = idx;
        }
        

        public void ResetToChainMode()
        {
            _singleRailMode = false;
        }

        /// <summary> Handles a LookAt switch triggered by a Timeline marker. </summary>
        public void HandleLookAtSwitch(CameraLookAtSwitchMarker m)
        {
            int idx = m.TargetIndex;
            Transform next = (idx >= 0 && idx < lookAtTargets.Count) ? lookAtTargets[idx] : null;
            SetLookAt(next, m.TransitionDuration);
        }

        /// <summary> Immediate LookAt switch with optional duration. </summary>
        public void SetLookAt(Transform target, float duration = 1f)
        {
            if (_lookAtTarget == target)
                return;
            
            _lookAtFrom = _lookAtTarget;
            _lookAtTarget = target;
            _lookAtDuration = duration;
            _lookAtLerp = duration > 0 ? 0f : 1f;
        }

        /// <summary> Generic event handler for legacy markers or external triggers. </summary>
        public void TriggerTimelineEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return;
            
            string lowerEvent = eventName.ToLower();

            if (lowerEvent == "flash")
            {
                targetCamera.DoFlash(Color.white, 0.5f);
            }
            else if (lowerEvent == "wobble")
            {
                targetCamera.DoWobble(5f, 1f, 2f);
            }

            onSplineNotified?.Invoke(eventName);
        }
        #endregion

        
        #region Public Properties
        public float SplineProgress { get => splineProgress; set => splineProgress = Mathf.Clamp01(value); }
        public float LookAtWeight { get => lookAtWeight; set => lookAtWeight = Mathf.Clamp01(value); }

        public UnityEngine.Camera TargetCamera => targetCamera;
        public List<Transform> LookAtTargets => lookAtTargets;
        public Transform CurrentLookAtTarget => _lookAtTarget;
        #endregion

        
        #region INotificationReceiver
        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (origin.IsValid())
            {
                var director = origin.GetGraph().GetResolver() as PlayableDirector;
                
                if (director != null && playableDirector != director)
                    playableDirector = director;
            }

            if (notification is CameraMarkerBase marker)
            {
                marker.Execute(this);
                OnMarkerEventHit?.Invoke(marker);
            }
        }
        #endregion

#if UNITY_EDITOR
        
        // ── Editor-only API ──────────────────────────────────────────

        public List<SplineComputer> EditorSplineRails => splineRails;
        public UnityEngine.Camera EditorCamera => targetCamera;
        public PlayableDirector EditorDirector => playableDirector;
        public Transform EditorLookAtTarget => lookAtTarget;
        public List<Transform> EditorLookAtTargets => lookAtTargets;
        public List<CameraSplineSegment> EditorSegments => segments;

        public int EditorRailCount
        {
            get
            {
                if (splineRails == null)
                    return 0;
                
                int count = 0;
                foreach (var r in splineRails)
                {
                    if (r != null) 
                        count++;
                }
                
                return count;
            }
        }

        public int EditorPointCount
        {
            get
            {
                if (splineRails == null)
                    return 0;
                
                int total = 0;
                foreach (var r in splineRails)
                {
                    if (r)
                        total += r.pointCount;
                }
                
                return total;
            }
        }

        public int EditorTotalSegmentCount
        {
            get
            {
                if (splineRails == null)
                    return 0;
                
                int total = 0;
                foreach (var r in splineRails)
                {
                    if (r)
                        total += Mathf.Max(0, r.pointCount - 1);
                }
                
                return total;
            }
        }

        public SplineSample EditorSampleAt(float progress)
        {
            if (!HasValidRails())
                return new SplineSample();
            
            RebuildChainCache();
            return EvaluateChainAt(Mathf.Clamp01(progress));
        }

        public void EditorEvaluateAt(float progress)
        {
            if (!HasValidRails() || !targetCamera)
                return;
            
            splineProgress = progress;
            RebuildChainCache();
            CameraPose p = SampleRailPose();
            targetCamera.transform.SetPositionAndRotation(p.position, p.rotation);
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
            {
                segments.Add(new CameraSplineSegment
                {
                    duration = 1f, easing = AnimationCurve.EaseInOut(0, 0, 1, 1)
                });
            }
            
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
