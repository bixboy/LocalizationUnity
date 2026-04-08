using UnityEngine;
using UnityEditor;
using Metroma.CameraTool;
using Metroma.CameraTool.Timeline;
using Dreamteck.Splines;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Collections.Generic;
using Metroma.CameraTool.Modifiers;

namespace Metroma.CameraTool.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="CameraTool"/>.
    /// Styled header, status, segment manager, easing presets,
    /// play preview, and timeline generation.
    /// </summary>
    [CustomEditor(typeof(CameraTool))]
    public class CameraToolEditor : UnityEditor.Editor
    {
        // ── Serialized Properties ────────────────────────────────────
        private SerializedProperty _splineRails;
        private SerializedProperty _targetCamera;
        private SerializedProperty _playableDirector;
        private SerializedProperty _lookAtTarget;
        private SerializedProperty _lookAtTargets;
        private SerializedProperty _splineProgress;
        private SerializedProperty _lookAtWeight;
        private SerializedProperty _segments;
        private SerializedProperty _chapters;
        private SerializedProperty _onChapterStart;
        private SerializedProperty _onChapterEnd;

        // ── Editor State ─────────────────────────────────────────────
        private bool _foldReferences = true;
        private bool _foldAnimation = true;
        private bool _foldSegments = true;
        private bool _foldChapters = true;
        private bool _foldEvents = false;
        private bool _foldDebug;
        private bool _isCameraLocked;

        // ── HUD Settings ─────────────────────────────────────────────
        private bool _showHud;
        private bool _showGrid = true;
        private bool _showLetterbox = true;
        private float _letterboxHeight = 0.12f; // 2.35:1 approx on 16:9
        private bool _showHaptics;
        
        // ── Preview State ────────────────────────────────────────────
        private bool _isPreviewing;
        private double _previewStartTime;
        private float _previewTotalDuration;
        private float[] _previewSegStarts;
        private float[] _previewSegDurations;
        private AnimationCurve[] _previewSegCurves;

        // ── Style Cache ──────────────────────────────────────────────
        private static GUIStyle _headerStyle;
        private static GUIStyle _subHeaderStyle;
        private static GUIStyle _statusOkStyle;
        private static GUIStyle _statusBadStyle;
        private static GUIStyle _progressLabelStyle;

        // ── Colors ───────────────────────────────────────────────────
        private static readonly Color HeaderColor = new Color(0.18f, 0.53f, 0.87f);
        private static readonly Color SeparatorColor = new Color(0.18f, 0.53f, 0.87f, 0.6f);
        private static readonly Color OkColor = new Color(0.2f, 0.8f, 0.3f);
        private static readonly Color BadColor = new Color(0.9f, 0.25f, 0.2f);
        private static readonly Color BoxBg = new Color(0.15f, 0.15f, 0.15f, 0.4f);
        private static readonly Color WaitBarColor = new Color(1f, 0.6f, 0.1f, 0.5f);

        // ══════════════════════════════════════════════════════════════

        private void OnEnable()
        {
            _splineRails = serializedObject.FindProperty("splineRails");
            _targetCamera = serializedObject.FindProperty("targetCamera");
            _playableDirector = serializedObject.FindProperty("playableDirector");
            _lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            _lookAtTargets = serializedObject.FindProperty("lookAtTargets");
            _splineProgress = serializedObject.FindProperty("splineProgress");
            _lookAtWeight = serializedObject.FindProperty("lookAtWeight");
            _segments = serializedObject.FindProperty("segments");
            _chapters = serializedObject.FindProperty("chapters");
            _onChapterStart = serializedObject.FindProperty("onChapterStart");
            _onChapterEnd = serializedObject.FindProperty("onChapterEnd");

            SceneView.duringSceneGui += SyncSceneView;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= SyncSceneView;
            StopPreview();
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            CameraTool tool = (CameraTool)target;

            try
            {
                DrawHeader();
                DrawSeparator();
                DrawStatusPanel(tool);
                
                EditorGUILayout.Space(4);
                
                // --- Grouped Sections ---
                DrawSystemConfig(tool);
                DrawAnimationControls(tool);
                DrawChapterManager(tool);
                DrawSegmentManager(tool);
                
                EditorGUILayout.Space(4);
                
                DrawEditorUtilities(tool);
            }
            catch (ExitGUIException) { throw; }
            catch (System.Exception e) { Debug.LogException(e); }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSystemConfig(CameraTool tool)
        {
            DrawReferencesSection();
            DrawViewportSection();
            DrawHapticsSection(tool);
        }

        private void DrawAnimationControls(CameraTool tool)
        {
            DrawAnimationSection(tool);
        }

        private void DrawChapterManager(CameraTool tool)
        {
            DrawChaptersSection(tool);
            DrawEventsSection();
        }

        private void DrawEditorUtilities(CameraTool tool)
        {
            DrawQuickActions(tool);
            DrawDebugSection(tool);
        }

        // ══════════════════════════════════════════════════════════════
        // Header
        // ══════════════════════════════════════════════════════════════

        private new void DrawHeader()
        {
            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, 32);
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 0.5f));
            GUI.Label(rect, "  🎬  MÉTRŌMA Camera Tool", _headerStyle);
            EditorGUILayout.Space(2);
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 3);
            EditorGUI.DrawRect(rect, SeparatorColor);
            EditorGUILayout.Space(4);
        }

        private static void DrawThinSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
            EditorGUILayout.Space(2);
        }

        // ══════════════════════════════════════════════════════════════
        // Status Panel
        // ══════════════════════════════════════════════════════════════

        private void DrawStatusPanel(CameraTool tool)
        {
            bool hasRails = tool.EditorRailCount > 0;
            bool hasCamera = tool.EditorCamera != null;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Status", EditorStyles.miniBoldLabel);
            DrawThinSeparator();

            EditorGUILayout.BeginHorizontal();
            DrawStatusIndicator($"Rails: {tool.EditorRailCount}", hasRails);
            GUILayout.FlexibleSpace();
            DrawStatusIndicator("Camera", hasCamera);
            GUILayout.FlexibleSpace();

            bool hasLookAt = tool.EditorLookAtTarget != null;
            if (hasLookAt)
            {
                GUIStyle lookStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.5f, 0.3f) },
                    fontStyle = FontStyle.Bold
                };

                GUILayout.Label("🎯 LookAt", lookStyle);
                GUILayout.FlexibleSpace();
            }

            bool ready = hasRails && hasCamera;
            GUIStyle readyStyle = ready ? _statusOkStyle : _statusBadStyle;
            GUILayout.Label(ready ? "● READY" : "● NOT READY", readyStyle);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawStatusIndicator(string label, bool ok)
        {
            GUIStyle style = ok ? _statusOkStyle : _statusBadStyle;
            GUILayout.Label($"{(ok ? "✔" : "✘")}  {label}", style);
        }

        // ══════════════════════════════════════════════════════════════
        // References Section
        // ══════════════════════════════════════════════════════════════

        private void DrawReferencesSection()
        {
            _foldReferences = DrawSectionHeader("📎  References", _foldReferences);
            if (!_foldReferences)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_splineRails, new GUIContent("Spline Rails"), true);
            EditorGUILayout.PropertyField(_targetCamera, new GUIContent("Target Camera"));
            EditorGUILayout.PropertyField(_playableDirector, new GUIContent("Playable Director"));
            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(_lookAtTarget, new GUIContent("🎯 Default Target"));
            EditorGUILayout.PropertyField(_lookAtTargets, new GUIContent("🎯 Target List"), true);
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
        }

        // ══════════════════════════════════════════════════════════════
        // Animation Section
        // ══════════════════════════════════════════════════════════════

        private void DrawAnimationSection(CameraTool tool)
        {
            _foldAnimation = DrawSectionHeader("🎞  Animation Settings", _foldAnimation);
            if (!_foldAnimation)
                return;

            EditorGUI.indentLevel++;

            // ── Progress slider ──
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_splineProgress, new GUIContent("Spline Progress"));

            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                tool.EditorEvaluateAt(_splineProgress.floatValue);
                SceneView.RepaintAll();
            }

            // ── Visual progress bar ──
            Rect barRect = EditorGUILayout.GetControlRect(false, 20);
            barRect = EditorGUI.IndentedRect(barRect);
            float progress = _splineProgress.floatValue;

            EditorGUI.DrawRect(barRect, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            
            Rect fillRect = barRect;
            fillRect.width *= progress;
            Color barColor = Color.Lerp(new Color(0.2f, 0.6f, 1f), new Color(0.1f, 0.9f, 0.4f), progress);
            
            EditorGUI.DrawRect(fillRect, barColor);
            GUI.Label(barRect, $"  {progress * 100f:F1}%", _progressLabelStyle);

            // ── LookAt weight slider ──
            EditorGUILayout.Space(2);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_lookAtWeight, new GUIContent("🎯 LookAt Weight"));
            if (EditorGUI.EndChangeCheck() && !Application.isPlaying)
            {
                serializedObject.ApplyModifiedProperties();
                tool.EditorEvaluateAt(_splineProgress.floatValue);
                SceneView.RepaintAll();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawViewportSection()
        {
            _showHud = DrawSectionHeader("👁  Viewport HUD", _showHud);
            if (!_showHud)
                return;

            EditorGUI.indentLevel++;
            _showGrid = EditorGUILayout.Toggle("Rule of Thirds", _showGrid);
            _showLetterbox = EditorGUILayout.Toggle("Cinematic Letterbox", _showLetterbox);
            if (_showLetterbox)
            {
                _letterboxHeight = EditorGUILayout.Slider("Letterbox Size", _letterboxHeight, 0.05f, 0.25f);
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawHapticsSection(CameraTool tool)
        {
            _showHaptics = DrawSectionHeader("🎮  Gamepad Haptics", _showHaptics);
            if (!_showHaptics)
                return;

            EditorGUI.indentLevel++;

            var cam = tool.TargetCamera;
            if (!cam)
            {
                EditorGUILayout.HelpBox("Assign a Target Camera to manage haptics.", MessageType.Warning);
            }
            else
            {
                var h = cam.GetComponent<CameraModifierHandler>();
                if (!h)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox("CameraModifierHandler missing.", MessageType.Info);
                    
                    if (GUILayout.Button("Add", GUILayout.Width(40)))
                        cam.gameObject.AddComponent<CameraModifierHandler>();
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    h.enableGamepadHaptics = EditorGUILayout.Toggle("Enable Vibration", h.enableGamepadHaptics);
                    h.hapticMultiplier = EditorGUILayout.Slider("Vibration Power", h.hapticMultiplier, 0f, 2f);
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        EditorUtility.SetDirty(h);
                    }
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawChaptersSection(CameraTool tool)
        {
            _foldChapters = DrawSectionHeader("📚  Chapters Manager", _foldChapters);
            if (!_foldChapters)
                return;

            EditorGUI.indentLevel++;

            if (_chapters.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No chapters defined. Add one to organize your Timelines.", MessageType.Info);
            }

            for (int i = 0; i < _chapters.arraySize; i++)
            {
                SerializedProperty chapterProp = _chapters.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = chapterProp.FindPropertyRelative("name");
                SerializedProperty directorProp = chapterProp.FindPropertyRelative("director");

                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                
                // Display Index
                GUILayout.Label($"#{i}", EditorStyles.miniBoldLabel, GUILayout.Width(25));
                
                // Fields
                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.Width(100));
                EditorGUILayout.PropertyField(directorProp, GUIContent.none);

                // Play Button
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.4f);
                if (GUILayout.Button("▶ Play", GUILayout.Width(55)))
                {
                    if (Application.isPlaying)
                    {
                        tool.PlayChapter(i);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("CameraTool", "Play mode required to test chapter transitions.", "OK");
                    }
                }
                GUI.backgroundColor = Color.white;

                // Delete Button
                if (GUILayout.Button("✕", GUILayout.Width(22)))
                {
                    _chapters.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    break;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add Chapter"))
            {
                _chapters.arraySize++;
                var newChapter = _chapters.GetArrayElementAtIndex(_chapters.arraySize - 1);
                newChapter.FindPropertyRelative("name").stringValue = $"Chapter {_chapters.arraySize}";
                newChapter.FindPropertyRelative("director").objectReferenceValue = null;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawEventsSection()
        {
            _foldEvents = DrawSectionHeader("🔔  Chapter Lifecycle", _foldEvents);
            if (!_foldEvents)
                return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_onChapterStart, new GUIContent("On Chapter Start"));
            EditorGUILayout.PropertyField(_onChapterEnd, new GUIContent("On Chapter End"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(4);
        }

        // ══════════════════════════════════════════════════════════════
        // Segment Manager
        // ══════════════════════════════════════════════════════════════

        private void DrawSegmentManager(CameraTool tool)
        {
            _foldSegments = DrawSectionHeader("🔗  Segment Manager", _foldSegments);
            if (!_foldSegments)
                return;

            EditorGUI.indentLevel++;

            // ── Info + Sync ──
            EditorGUILayout.BeginHorizontal();
            int pointCount = tool.EditorPointCount;
            int segmentCount = _segments.arraySize;
            EditorGUILayout.LabelField($"Rails: {tool.EditorRailCount}  |  Nodes: {pointCount}  |  Segments: {segmentCount}",
                EditorStyles.miniLabel);

            if (GUILayout.Button("⟳ Sync", GUILayout.Width(70), GUILayout.Height(20)))
            {
                Undo.RecordObject(tool, "Sync Camera Segments");
                tool.EditorSyncSegments();
                EditorUtility.SetDirty(tool);
                serializedObject.SetIsDifferentCacheDirty();
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            if (segmentCount == 0)
            {
                EditorGUILayout.HelpBox("Click \"Sync\" to populate segments from the spline rails.", MessageType.Info);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
                return;
            }

            EditorGUILayout.Space(4);
            DrawDurationTimeline(_segments);
            EditorGUILayout.Space(4);

            // ── Per-segment cards ──
            for (int i = 0; i < _segments.arraySize; i++)
                DrawSegmentCard(i, tool);

            // ── Totals ──
            float totalDuration = 0f, totalWait = 0f;
            for (int i = 0; i < _segments.arraySize; i++)
            {
                var seg = _segments.GetArrayElementAtIndex(i);
                totalDuration += seg.FindPropertyRelative("duration").floatValue;
                totalWait += seg.FindPropertyRelative("waitAtEnd").floatValue;
            }

            DrawThinSeparator();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"⏱ Total: {totalDuration + totalWait:F1}s", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"(Move: {totalDuration:F1}s  +  Wait: {totalWait:F1}s)", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ── Generate Timeline ──
            GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
            if (GUILayout.Button("🎬  Generate Timeline Clips", GUILayout.Height(30)))
                GenerateTimelineClips(tool);
                
            GUI.backgroundColor = Color.white;

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        private void DrawSegmentCard(int index, CameraTool tool)
        {
            SerializedProperty segProp = _segments.GetArrayElementAtIndex(index);
            SerializedProperty labelProp = segProp.FindPropertyRelative("label");
            SerializedProperty durationProp = segProp.FindPropertyRelative("duration");
            SerializedProperty easingProp = segProp.FindPropertyRelative("easing");
            SerializedProperty waitProp = segProp.FindPropertyRelative("waitAtEnd");

            int segmentCount = _segments.arraySize;
            float startProgress = (float)index / segmentCount;
            float endProgress = (float)(index + 1) / segmentCount;

            EditorGUILayout.BeginVertical("helpbox");

            // ── Header ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(labelProp.stringValue, EditorStyles.miniBoldLabel, GUILayout.Width(120));
            EditorGUILayout.LabelField($"{durationProp.floatValue:F1}s", EditorStyles.miniLabel, GUILayout.Width(40));

            if (waitProp.floatValue > 0f)
            {
                GUIStyle ws = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1f, 0.6f, 0.1f) } };
                EditorGUILayout.LabelField($"+ {waitProp.floatValue:F1}s", ws, GUILayout.Width(50));
            }
            
            EditorGUILayout.EndHorizontal();

            // ── Fields ──
            EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (sec)"));
            EditorGUILayout.PropertyField(easingProp, new GUIContent("Easing"));
            EditorGUILayout.PropertyField(waitProp, new GUIContent("Pause at End"));

            // ── Easing Presets ──
            EditorGUILayout.BeginHorizontal();
            GUIStyle ps = EditorStyles.miniButton;
            if (GUILayout.Button("Linear", ps))
                easingProp.animationCurveValue = AnimationCurve.Linear(0, 0, 1, 1);

            if (GUILayout.Button("Ease", ps))
                easingProp.animationCurveValue = AnimationCurve.EaseInOut(0, 0, 1, 1);
                
            if (GUILayout.Button("Ease In", ps))
                easingProp.animationCurveValue = new AnimationCurve(
                    new Keyframe(0, 0, 0, 0), 
                    new Keyframe(1, 1, 2, 0));
            
            if (GUILayout.Button("Ease Out", ps))
                easingProp.animationCurveValue = new AnimationCurve(
                    new Keyframe(0, 0, 0, 2),
                    new Keyframe(1, 1, 0, 0));
            
            if (GUILayout.Button("Slow", ps))
                easingProp.animationCurveValue = new AnimationCurve(
                    new Keyframe(0, 0, 0, 0),
                    new Keyframe(0.5f, 0.5f, 0.3f, 0.3f),
                    new Keyframe(1, 1, 0, 0));
            
            EditorGUILayout.EndHorizontal();

            // ── Preview buttons ──
            EditorGUILayout.BeginHorizontal();
            GUIStyle pvs = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 18 };
            if (GUILayout.Button($"📍 Start ({startProgress * 100f:F0}%)", pvs))
            {
                if (tool.EditorRailCount > 0)
                {
                    tool.EditorEvaluateAt(startProgress);
                    SceneView.RepaintAll();
                }
            }
            if (GUILayout.Button($"📍 End ({endProgress * 100f:F0}%)", pvs))
            {
                if (tool.EditorRailCount > 0)
                {
                    tool.EditorEvaluateAt(endProgress);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private static void DrawDurationTimeline(SerializedProperty segments)
        {
            float totalTime = 0f;
            for (int i = 0; i < segments.arraySize; i++)
            {
                var seg = segments.GetArrayElementAtIndex(i);
                totalTime += seg.FindPropertyRelative("duration").floatValue;
                totalTime += seg.FindPropertyRelative("waitAtEnd").floatValue;
            }
            if (totalTime <= 0f)
                return;

            Rect barRect = EditorGUILayout.GetControlRect(false, 16);
            barRect = EditorGUI.IndentedRect(barRect);
            EditorGUI.DrawRect(barRect, new Color(0.08f, 0.08f, 0.08f, 0.8f));

            float xOffset = 0f;
            for (int i = 0; i < segments.arraySize; i++)
            {
                var seg = segments.GetArrayElementAtIndex(i);
                float dur = seg.FindPropertyRelative("duration").floatValue;
                float wait = seg.FindPropertyRelative("waitAtEnd").floatValue;

                float moveW = (dur / totalTime) * barRect.width;
                Rect moveRect = new Rect(barRect.x + xOffset, barRect.y, moveW, barRect.height);
                Color c = Color.HSVToRGB((float)i / segments.arraySize * 0.6f, 0.5f, 0.85f);
                c.a = 0.7f;
                EditorGUI.DrawRect(moveRect, c);

                if (moveW > 25f)
                {
                    GUIStyle ms = new GUIStyle(EditorStyles.miniLabel) {
                        alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white }, fontSize = 9 };
                    GUI.Label(moveRect, $"{i}", ms);
                }
                xOffset += moveW;

                if (wait > 0f)
                {
                    float waitW = (wait / totalTime) * barRect.width;
                    EditorGUI.DrawRect(new Rect(barRect.x + xOffset, barRect.y, waitW, barRect.height), WaitBarColor);
                    xOffset += waitW;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Timeline Generation
        // ══════════════════════════════════════════════════════════════

        private void GenerateTimelineClips(CameraTool tool)
        {
            PlayableDirector director = tool.EditorDirector;
            if (!director)
            {
                EditorUtility.DisplayDialog("CameraTool", "Assign a PlayableDirector first.", "OK");
                return;
            }

            TimelineAsset timeline = director.playableAsset as TimelineAsset;
            if (!timeline)
            {
                EditorUtility.DisplayDialog("CameraTool", "No TimelineAsset on the PlayableDirector.", "OK");
                return;
            }

            List<CameraSplineSegment> segs = tool.EditorSegments;
            if (segs.Count == 0)
            {
                EditorUtility.DisplayDialog("CameraTool", "No segments. Click Sync first.", "OK");
                return;
            }

            Undo.RecordObject(timeline, "Generate Camera Timeline Clips");

            CameraToolTrack cameraTrack = null;
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {   
                if (track is CameraToolTrack ct)
                {
                    cameraTrack = ct;
                    break;
                }
            }

            if (!cameraTrack)
            {
                cameraTrack = timeline.CreateTrack<CameraToolTrack>(null, "🎬 Camera Spline");
                director.SetGenericBinding(cameraTrack, tool);
            }

            // Clear
            var existing = new List<TimelineClip>(cameraTrack.GetClips());
            foreach (var c in existing)
            {
                cameraTrack.DeleteClip(c);
            }

            // Create
            int segmentCount = segs.Count;
            double currentTime = 0;

            for (int i = 0; i < segmentCount; i++)
            {
                CameraSplineSegment seg = segs[i];
                float startP = (float)i / segmentCount;
                float endP = (float)(i + 1) / segmentCount;

                TimelineClip clip = cameraTrack.CreateDefaultClip();
                clip.displayName = seg.label;
                clip.start = currentTime;
                clip.duration = seg.duration;

                CameraToolClip clipAsset = clip.asset as CameraToolClip;
                if (clipAsset)
                {
                    Undo.RecordObject(clipAsset, "Configure Camera Clip");
                    clipAsset.Template.startProgress = startP;
                    clipAsset.Template.endProgress = endP;
                    clipAsset.Template.easingCurve = new AnimationCurve(seg.easing.keys);
                    EditorUtility.SetDirty(clipAsset);
                }

                currentTime += seg.duration + seg.waitAtEnd;
            }

            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(cameraTrack);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CameraTool] Generated {segmentCount} clips. Total: {currentTime:F1}s");
        }

        // ══════════════════════════════════════════════════════════════
        // Quick Actions + Play Preview
        // ══════════════════════════════════════════════════════════════

        private void DrawQuickActions(CameraTool tool)
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
            DrawThinSeparator();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("⏮  Start", GUILayout.Height(28)))
            {
                _splineProgress.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                    tool.EditorEvaluateAt(0f);
                
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("⏭  End", GUILayout.Height(28)))
            {
                _splineProgress.floatValue = 1f;
                serializedObject.ApplyModifiedProperties();
                if (!Application.isPlaying)
                    tool.EditorEvaluateAt(1f);
                
                SceneView.RepaintAll();
            }

            if (_isCameraLocked)
                GUI.backgroundColor = new Color(1f, 0.6f, 0f);
            
            string focusText = _isCameraLocked ? "🔒  Locked to Camera" : "📍  Focus & Lock";
            if (GUILayout.Button(focusText, GUILayout.Height(28)))
            {
                _isCameraLocked = !_isCameraLocked;
                if (_isCameraLocked && tool.EditorCamera)
                {
                    SceneView sv = SceneView.lastActiveSceneView;
                    if (sv)
                        sv.AlignViewToObject(tool.EditorCamera.transform);
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // ── Play Preview ──
            EditorGUILayout.Space(4);

            if (!_isPreviewing)
            {
                GUI.backgroundColor = new Color(0.2f, 0.85f, 0.3f);
                if (GUILayout.Button("▶  Preview Animation", GUILayout.Height(28)))
                    StartPreview(tool);
                
                GUI.backgroundColor = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                if (GUILayout.Button("⏹  Stop Preview", GUILayout.Height(28)))
                    StopPreview();
                
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        // ── Preview System ───────────────────────────────────────────

        private void StartPreview(CameraTool tool)
        {
            int segCount = _segments.arraySize;
            if (segCount == 0)
                return;

            _previewSegStarts = new float[segCount];
            _previewSegDurations = new float[segCount];
            _previewSegCurves = new AnimationCurve[segCount];

            float cumulative = 0f;
            for (int i = 0; i < segCount; i++)
            {
                var seg = _segments.GetArrayElementAtIndex(i);
                float dur = seg.FindPropertyRelative("duration").floatValue;
                float wait = seg.FindPropertyRelative("waitAtEnd").floatValue;
                AnimationCurve curve = seg.FindPropertyRelative("easing").animationCurveValue;

                _previewSegStarts[i] = cumulative;
                _previewSegDurations[i] = dur;
                _previewSegCurves[i] = curve ?? AnimationCurve.Linear(0, 0, 1, 1);

                cumulative += dur + wait;
            }

            _previewTotalDuration = cumulative;
            _previewStartTime = EditorApplication.timeSinceStartup;
            _isPreviewing = true;

            EditorApplication.update += PreviewUpdate;
        }

        private void StopPreview()
        {
            if (!_isPreviewing)
                return;
            
            _isPreviewing = false;
            EditorApplication.update -= PreviewUpdate;
        }

        private void PreviewUpdate()
        {
            if (!_isPreviewing || target == null)
            {
                StopPreview();
                return;
            }

            CameraTool tool = (CameraTool)target;
            float elapsed = (float)(EditorApplication.timeSinceStartup - _previewStartTime);

            if (elapsed >= _previewTotalDuration)
            {
                tool.EditorEvaluateAt(1f);
                StopPreview();
                return;
            }

            // Find current segment and calculate progress
            int segCount = _previewSegStarts.Length;
            float progress = 1f;

            for (int i = 0; i < segCount; i++)
            {
                if (elapsed < _previewSegStarts[i] + _previewSegDurations[i])
                {
                    float t = Mathf.Clamp01((elapsed - _previewSegStarts[i]) / _previewSegDurations[i]);
                    float easedT = _previewSegCurves[i].Evaluate(t);
                    progress = Mathf.Lerp((float)i / segCount, (float)(i + 1) / segCount, easedT);
                    break;
                }
                
                // If in wait time between segments
                if (i < segCount - 1 && elapsed < _previewSegStarts[i + 1])
                {
                    progress = (float)(i + 1) / segCount;
                    break;
                }
            }

            tool.EditorEvaluateAt(Mathf.Clamp01(progress));
            SceneView.RepaintAll();
            Repaint();
        }

        // ══════════════════════════════════════════════════════════════
        // Debug Section
        // ══════════════════════════════════════════════════════════════

        private void DrawDebugSection(CameraTool tool)
        {
            _foldDebug = DrawSectionHeader("🛠  Debug Info", _foldDebug);
            if (!_foldDebug)
                return;

            EditorGUI.indentLevel++;
            GUI.enabled = false;

            if (tool.EditorRailCount > 0)
            {
                SplineSample sample = tool.EditorSampleAt(_splineProgress.floatValue);
                EditorGUILayout.Vector3Field("Position", sample.position);
                EditorGUILayout.Vector3Field("Forward", sample.forward);
                EditorGUILayout.Vector3Field("Up", sample.up);
            }
            else
            {
                EditorGUILayout.HelpBox("Assign Spline Rails to see debug data.", MessageType.Info);
            }

            GUI.enabled = true;
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        // ══════════════════════════════════════════════════════════════
        // Helpers
        // ══════════════════════════════════════════════════════════════

        private static bool DrawSectionHeader(string title, bool foldout)
        {
            EditorGUILayout.Space(2);
            var rect = EditorGUILayout.GetControlRect(false, 22);
            
            EditorGUI.DrawRect(rect, BoxBg);
            rect.x += 4;
            rect.width -= 4;
            return EditorGUI.Foldout(rect, foldout, title, true, EditorStyles.foldoutHeader);
        }

        private static void InitStyles()
        {
            if (_headerStyle != null)
                return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16, alignment = TextAnchor.MiddleLeft,
                normal = { textColor = HeaderColor }, fixedHeight = 32
            };
            
            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            
            _statusOkStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = OkColor }
            };
            
            _statusBadStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 11, fontStyle = FontStyle.Bold, normal = { textColor = BadColor }
            };
            
            _progressLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white }
            };
        }

        // ══════════════════════════════════════════════════════════════
        // Scene View Sync Logic
        // ══════════════════════════════════════════════════════════════

        private void SyncSceneView(SceneView sv)
        {
            if (!_isCameraLocked || Application.isPlaying)
                return;

            CameraTool tool = (CameraTool)target;
            Camera cam = tool.EditorCamera;
            if (cam == null)
                return;

            Transform t = cam.transform;

            sv.pivot = t.position;
            sv.rotation = t.rotation;
            sv.size = 0f;
            sv.orthographic = cam.orthographic;
            
            SerializedObject so = new SerializedObject(sv);
            so.Update();
            var fovProp = so.FindProperty("m_FieldOfView");
            if (fovProp != null)
            {
                fovProp.floatValue = cam.fieldOfView;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            sv.Repaint();
        }

        private void OnSceneGUI()
        {
            if (!_isCameraLocked || !_showHud)
                return;

            DrawCinematicHUD();
        }

        private void DrawCinematicHUD()
        {
            Handles.BeginGUI();
            Rect viewRect = SceneView.lastActiveSceneView.position;
            float w = viewRect.width;
            float h = viewRect.height;

            // 1. Cinematic Letterbox
            if (_showLetterbox)
            {
                float barHeight = h * _letterboxHeight;
                EditorGUI.DrawRect(new Rect(0, 0, w, barHeight), Color.black);
                EditorGUI.DrawRect(new Rect(0, h - barHeight, w, barHeight), Color.black);
            }

            // 2. Rule of Thirds
            if (_showGrid)
            {
                Color gridColor = new Color(1f, 1f, 1f, 0.2f);
                
                // Vertical lines
                EditorGUI.DrawRect(new Rect(w / 3f, 0, 1, h), gridColor);
                EditorGUI.DrawRect(new Rect(2f * w / 3f, 0, 1, h), gridColor);
                
                // Horizontal lines
                EditorGUI.DrawRect(new Rect(0, h / 3f, w, 1), gridColor);
                EditorGUI.DrawRect(new Rect(0, 2f * h / 3f, w, 1), gridColor);
            }

            // 3. 16:9 Safe Area Guide
            Color safeColor = new Color(0f, 1f, 0.3f, 0.15f);
            float targetAspect = 16f / 9f;
            float currentAspect = w / h;
            if (currentAspect > targetAspect)
            {
                float safeW = h * targetAspect;
                float side = (w - safeW) / 2f;
                EditorGUI.DrawRect(new Rect(side, 0, 1, h), safeColor);
                EditorGUI.DrawRect(new Rect(w - side, 0, 1, h), safeColor);
            }

            Handles.EndGUI();
        }
    }
}
