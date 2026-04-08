using UnityEngine;
using UnityEditor;
using Metroma.CameraTool;
using Metroma.CameraTool.Timeline;
using Dreamteck.Splines;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using System.Collections.Generic;
using Metroma.CameraTool.Modifiers;
using UnityEditor.Timeline;

namespace Metroma.CameraTool.Editor
{
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
        private SerializedProperty _chapters;
        private int _selectedChapterIndex = 0;
        private SerializedProperty _onChapterStart;
        private SerializedProperty _onChapterEnd;

        // ── Editor State ─────────────────────────────────────────────
        private bool _foldReferences = false;
        private bool _foldAnimation = false;
        private bool _foldSegments = false;
        private bool _foldChapters = false;
        private bool _foldEvents = false;
        private bool _foldViewport = false;
        private bool _foldHaptics = false;
        private bool _foldDebug = false;
        private bool _isCameraLocked;

        private bool _showHud;
        private bool _showGrid = true;
        private bool _showLetterbox = true;
        private float _letterboxHeight = 0.12f;

        // ── Preview State ────────────────────────────────────────────
        private bool _isPreviewing;
        private double _previewStartTime;
        private float _previewTotalDuration;
        private float[] _previewSegStarts;
        private float[] _previewSegDurations;
        private AnimationCurve[] _previewSegCurves;

        // ── Style Cache ──────────────────────────────────────────────
        private static GUIStyle _headerStyle;
        private static GUIStyle _statusOkStyle;
        private static GUIStyle _statusBadStyle;
        private static GUIStyle _sectionTitleStyle;

        // ── Colors ───────────────────────────────────────────────────
        private static readonly Color CyanAccent = new Color(0.1f, 0.8f, 1f);
        private static readonly Color HeaderBg = new Color(0.08f, 0.08f, 0.08f, 1f);
        private static readonly Color OkColor = new Color(0.2f, 0.85f, 0.4f);
        private static readonly Color BadColor = new Color(1f, 0.3f, 0.4f);
        private static readonly Color WaitBarColor = new Color(1f, 0.6f, 0.1f, 0.4f);
        private static readonly Color LineColor = new Color(1f, 1f, 1f, 0.08f);

        private void OnEnable()
        {
            _splineRails = serializedObject.FindProperty("splineRails");
            _targetCamera = serializedObject.FindProperty("targetCamera");
            _playableDirector = serializedObject.FindProperty("playableDirector");
            _lookAtTarget = serializedObject.FindProperty("lookAtTarget");
            _lookAtTargets = serializedObject.FindProperty("lookAtTargets");
            _splineProgress = serializedObject.FindProperty("splineProgress");
            _lookAtWeight = serializedObject.FindProperty("lookAtWeight");
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

            // Calculate Progress for the Live Cursor
            float currentProgress = -1;
            if (_isPreviewing)
            {
                currentProgress = (float)((EditorApplication.timeSinceStartup - _previewStartTime) / _previewTotalDuration);
            }
            else if (Application.isPlaying && tool.EditorDirector != null && tool.EditorDirector.state == PlayState.Playing)
            {
                currentProgress = (float)(tool.EditorDirector.time / tool.EditorDirector.duration);
            }

            if (currentProgress >= 0 || _isPreviewing) Repaint();

            try
            {
                DrawSuiteHeader();
                DrawStatusPanel(tool);
                EditorGUILayout.Space(8);

                DrawRigSetup();
                EditorGUILayout.Space(4);
                
                DrawChapterWorkflow(tool);
                EditorGUILayout.Space(4);
                
                DrawSegmentsSection(tool, currentProgress);
                EditorGUILayout.Space(4);
                
                DrawLookAtAndFX(tool);
                EditorGUILayout.Space(4);
                
                DrawHUDSection();
                EditorGUILayout.Space(4);
                
                DrawUtilitiesSection(tool);
                EditorGUILayout.Space(12);
            }
            catch (ExitGUIException) { throw; }
            catch (System.Exception e) { Debug.LogException(e); }

            serializedObject.ApplyModifiedProperties();
        }

        // ── Section Drawing ──────────────────────────────────────────

        private void DrawRigSetup()
        {
            _foldReferences = DrawSectionHeader("📐  Rig Setup", _foldReferences);
            if (_foldReferences)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_splineRails, new GUIContent("Spline Rails"), true);
                EditorGUILayout.PropertyField(_targetCamera, new GUIContent("Main Camera"));
                EditorGUILayout.PropertyField(_playableDirector, new GUIContent("Master Director"));
                EditorGUI.indentLevel--;
            }
        }

        private void DrawChapterWorkflow(CameraTool tool)
        {
            _foldChapters = DrawSectionHeader("🎞  Chapters & Sequences", _foldChapters);
            if (_foldChapters)
            {
                DrawChaptersSection(tool);
                DrawEventsSection();
            }
        }

        private void DrawSegmentsSection(CameraTool tool, float currentProgress)
        {
            _foldSegments = DrawSectionHeader("⏱  Pacing & Segments", _foldSegments);
            if (_foldSegments)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_splineProgress, new GUIContent("Manual Scrub"));
                EditorGUI.indentLevel--;
                
                EditorGUILayout.Space(8);
                DrawSegmentManager(tool, currentProgress);
            }
        }

        private void DrawLookAtAndFX(CameraTool tool)
        {
            _foldAnimation = DrawSectionHeader("🎯  Targets & Effects", _foldAnimation);
            if (_foldAnimation)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_lookAtTarget, new GUIContent("Default LookAt"));
                EditorGUILayout.PropertyField(_lookAtTargets, new GUIContent("Target List"), true);
                EditorGUILayout.PropertyField(_lookAtWeight, new GUIContent("LookAt Weight"));
                
                DrawThinSeparator();
                DrawHapticsSection(tool);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawHUDSection()
        {
            _foldViewport = DrawSectionHeader("👁  Editor Viewport", _foldViewport);
            if (_foldViewport)
            {
                DrawViewportSection();
            }
        }

        private void DrawUtilitiesSection(CameraTool tool)
        {
            _foldDebug = DrawSectionHeader("🛠  Utilities", _foldDebug);
            if (_foldDebug)
            {
                DrawQuickActions(tool);
                DrawDebugSection(tool);
            }
        }

        // ── Core Component View ──────────────────────────────────────

        private void DrawStatusPanel(CameraTool tool)
        {
            bool hasRails = tool.EditorRailCount > 0;
            bool hasCamera = tool.EditorCamera != null;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12);
            DrawStatusIndicator($"Rails: {tool.EditorRailCount}", hasRails);
            GUILayout.FlexibleSpace();
            DrawStatusIndicator("Camera", hasCamera);
            GUILayout.FlexibleSpace();
            
            bool ready = hasRails && hasCamera;
            GUILayout.Label(ready ? "● READY" : "● NOT READY", ready ? _statusOkStyle : _statusBadStyle);
            EditorGUILayout.EndHorizontal();
            DrawThinSeparator();
        }

        private void DrawStatusIndicator(string label, bool ok)
        {
            GUILayout.Label($"{(ok ? "✔" : "✘")}  {label}", ok ? _statusOkStyle : _statusBadStyle);
        }

        private void DrawChaptersSection(CameraTool tool)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12);
            if (GUILayout.Button("⟳  SCAN PROJECT", GUILayout.Height(26))) AutoScanChapters(tool);
            if (GUILayout.Button("🗑  CLEAR", GUILayout.Width(80), GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Clear Chapters", "Delete all?", "Yes", "No"))
                {
                    _chapters.arraySize = 0;
                    _selectedChapterIndex = 0;
                }
            }
            EditorGUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);

            for (int i = 0; i < _chapters.arraySize; i++)
            {
                SerializedProperty chapterProp = _chapters.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = chapterProp.FindPropertyRelative("name");
                SerializedProperty timelineProp = chapterProp.FindPropertyRelative("timeline");
                SerializedProperty railIdxProp = chapterProp.FindPropertyRelative("startRailIndex");
                SerializedProperty colorProp = chapterProp.FindPropertyRelative("debugColor");
                SerializedProperty isExpanded = chapterProp.FindPropertyRelative("isExpanded");

                bool isSelected = (_selectedChapterIndex == i);

                // --- Chapter Item Container ---
                EditorGUILayout.BeginVertical();
                
                // Selection highlight & line
                var rect = EditorGUILayout.BeginHorizontal(GUILayout.Height(32));
                if (isSelected)
                {
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height), new Color(0.1f, 0.8f, 1f, 0.05f));
                    EditorGUI.DrawRect(new Rect(rect.x, rect.y, 2, rect.height), CyanAccent);
                }

                EditorGUILayout.Space(12);
                isExpanded.boolValue = EditorGUILayout.Foldout(isExpanded.boolValue, $"#{i}  {nameProp.stringValue}", true);

                GUILayout.FlexibleSpace();

                // Actions
                if (GUILayout.Button(isSelected ? "● FOCUSED" : "🎯 FOCUS", isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton, GUILayout.Width(85), GUILayout.Height(20)))
                {
                    _selectedChapterIndex = i;
                    if (timelineProp.objectReferenceValue is TimelineAsset asset && tool.EditorDirector != null)
                    {
                        Undo.RecordObject(tool.EditorDirector, "Focus Chapter");
                        tool.EditorDirector.playableAsset = asset;
                        Selection.activeObject = tool.gameObject;
                        EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
                        TimelineEditor.Refresh(RefreshReason.ContentsModified);
                    }
                }

                GUI.backgroundColor = OkColor;
                if (GUILayout.Button("▶", GUILayout.Width(25), GUILayout.Height(20)))
                {
                    if (Application.isPlaying) tool.PlayChapter(i);
                    else EditorUtility.DisplayDialog("CameraTool", "Enter Play Mode to test.", "OK");
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("✕", EditorStyles.miniLabel, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    _chapters.DeleteArrayElementAtIndex(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                // Expanded Drawer
                if (isExpanded.boolValue)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space(30);
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.PropertyField(nameProp);
                    EditorGUILayout.PropertyField(timelineProp);
                    EditorGUILayout.PropertyField(railIdxProp);
                    EditorGUILayout.PropertyField(colorProp, new GUIContent("Debug Color"));
                    EditorGUILayout.Space(8);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
                DrawThinSeparator();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12);
            if (GUILayout.Button("+ ADD MANUAL CHAPTER", GUILayout.Height(24)))
            {
                _chapters.arraySize++;
                var c = _chapters.GetArrayElementAtIndex(_chapters.arraySize - 1);
                c.FindPropertyRelative("name").stringValue = "New Sequence";
                c.FindPropertyRelative("isExpanded").boolValue = true;
            }
            EditorGUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
        }


        private void DrawSegmentManager(CameraTool tool, float currentProgress)
        {
            if (_chapters.arraySize == 0) return;

            _selectedChapterIndex = Mathf.Clamp(_selectedChapterIndex, 0, _chapters.arraySize - 1);
            SerializedProperty chapter = _chapters.GetArrayElementAtIndex(_selectedChapterIndex);
            SerializedProperty segmentsProp = chapter.FindPropertyRelative("segments");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(12);
            GUI.enabled = false;
            EditorGUILayout.LabelField($"ACTIVE: {chapter.FindPropertyRelative("name").stringValue.ToUpper()}", EditorStyles.miniLabel);
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("⟳ SYNC RAIL NODES", EditorStyles.miniButton, GUILayout.Width(130))) tool.EditorSyncSegments(_selectedChapterIndex);
            EditorGUILayout.Space(12);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawDurationTimeline(segmentsProp, currentProgress);
            
            // Display Total Time
            float totalDur = 0;
            float totalWait = 0;
            for (int i = 0; i < segmentsProp.arraySize; i++)
            {
                var s = segmentsProp.GetArrayElementAtIndex(i);
                totalDur += s.FindPropertyRelative("duration").floatValue;
                totalWait += s.FindPropertyRelative("waitAtEnd").floatValue;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var timeStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = CyanAccent } };
            var waitStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = WaitBarColor } };
            
            GUILayout.Label($"TOTAL DURATION: {totalDur + totalWait:F2}s", timeStyle);
            EditorGUILayout.Space(8);
            GUILayout.Label($"[ WAIT: {totalWait:F2}s ]", waitStyle);
            EditorGUILayout.Space(12);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);
            
            for (int i = 0; i < segmentsProp.arraySize; i++)
            {
                DrawSegmentCard(segmentsProp.GetArrayElementAtIndex(i), i, segmentsProp.arraySize);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(40);
            if (GUILayout.Button("🎬  GENERATE TIMELINE CLIPS", GUILayout.Height(30)))
                GenerateTimelineClips(tool, _selectedChapterIndex);
            EditorGUILayout.Space(40);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSegmentCard(SerializedProperty seg, int index, int totalCount)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.Height(32));
            EditorGUILayout.Space(12);
            
            // Colorful indicator matching the timeline
            var dotRect = EditorGUILayout.GetControlRect(false, 20, GUILayout.Width(10));
            EditorGUI.DrawRect(new Rect(dotRect.x, dotRect.y + 6, 6, 12), Color.HSVToRGB((float)index / totalCount, 0.6f, 0.9f));

            EditorGUILayout.LabelField(seg.FindPropertyRelative("label").stringValue, EditorStyles.miniBoldLabel, GUILayout.Width(110));

            // Curve Thumbnail
            var curveRect = EditorGUILayout.GetControlRect(false, 18, GUILayout.Width(35));
            DrawMiniCurve(curveRect, seg.FindPropertyRelative("easing").animationCurveValue);
            EditorGUILayout.Space(5);

            EditorGUIUtility.labelWidth = 35;
            EditorGUILayout.PropertyField(seg.FindPropertyRelative("duration"), new GUIContent("Dur"));
            EditorGUILayout.PropertyField(seg.FindPropertyRelative("waitAtEnd"), new GUIContent("Wait"));
            EditorGUIUtility.labelWidth = 0;
            
            EditorGUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            DrawThinSeparator();
        }

        private void DrawMiniCurve(Rect rect, AnimationCurve curve)
        {
            EditorGUI.DrawRect(rect, new Color(0, 0, 0, 0.3f));
            if (curve == null) return;
            
            Handles.BeginGUI();
            Handles.color = CyanAccent;
            int samples = 8;
            Vector3 lastPos = new Vector3(rect.x, rect.yMax - curve.Evaluate(0) * rect.height, 0);
            for (int i = 1; i <= samples; i++)
            {
                float t = (float)i / samples;
                Vector3 pos = new Vector3(rect.x + t * rect.width, rect.yMax - curve.Evaluate(t) * rect.height, 0);
                Handles.DrawLine(lastPos, pos);
                lastPos = pos;
            }
            Handles.EndGUI();
        }

        private void DrawDurationTimeline(SerializedProperty segments, float progress)
        {
            float total = 0;
            for (int i = 0; i < segments.arraySize; i++)
            {
                total += segments.GetArrayElementAtIndex(i).FindPropertyRelative("duration").floatValue;
                total += segments.GetArrayElementAtIndex(i).FindPropertyRelative("waitAtEnd").floatValue;
            }
            if (total <= 0) return;

            Rect rect = EditorGUILayout.GetControlRect(false, 18);
            EditorGUI.DrawRect(rect, new Color(1,1,1, 0.05f));
            float x = 0;
            for (int i = 0; i < segments.arraySize; i++)
            {
                var s = segments.GetArrayElementAtIndex(i);
                float dur = s.FindPropertyRelative("duration").floatValue;
                float wait = s.FindPropertyRelative("waitAtEnd").floatValue;
                
                float w = (dur / total) * rect.width;
                EditorGUI.DrawRect(new Rect(rect.x + x, rect.y, w, rect.height), Color.HSVToRGB((float)i / segments.arraySize, 0.6f, 0.8f));
                x += w;
                
                float ww = (wait / total) * rect.width;
                EditorGUI.DrawRect(new Rect(rect.x + x, rect.y, ww, rect.height), WaitBarColor);
                x += ww;
            }

            // Draw Live Cursor
            if (progress >= 0 && progress <= 1)
            {
                float cursorX = rect.x + (progress * rect.width);
                EditorGUI.DrawRect(new Rect(cursorX - 1, rect.y - 4, 3, rect.height + 8), Color.white);
                EditorGUI.DrawRect(new Rect(cursorX - 4, rect.y - 6, 9, 3), CyanAccent); // Cursor head
            }
        }

        private void DrawHapticsSection(CameraTool tool)
        {
            _foldHaptics = DrawSectionHeader("🎮  Gamepad Haptics", _foldHaptics);
            if (!_foldHaptics) return;

            if (tool.EditorCamera == null) return;
            var h = tool.EditorCamera.GetComponent<CameraModifierHandler>();
            if (h == null)
            {
                if (GUILayout.Button("Add Haptic Handler")) tool.EditorCamera.gameObject.AddComponent<CameraModifierHandler>();
                return;
            }
            h.enableGamepadHaptics = EditorGUILayout.Toggle("Vibration", h.enableGamepadHaptics);
            h.hapticMultiplier = EditorGUILayout.Slider("Power", h.hapticMultiplier, 0, 2);
        }

        private void DrawViewportSection()
        {
            EditorGUI.indentLevel++;
            _showHud = EditorGUILayout.Toggle("Show HUD", _showHud);
            _showGrid = EditorGUILayout.Toggle("Rule of Thirds", _showGrid);
            _showLetterbox = EditorGUILayout.Toggle("Letterbox", _showLetterbox);
            if (_showLetterbox) _letterboxHeight = EditorGUILayout.Slider("Size", _letterboxHeight, 0.05f, 0.3f);
            EditorGUI.indentLevel--;
        }

        private void DrawQuickActions(CameraTool tool)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("⏮ Start")) tool.EditorEvaluateAt(0);
            if (GUILayout.Button("⏭ End")) tool.EditorEvaluateAt(1);
            if (GUILayout.Button(_isCameraLocked ? "🔒 Locked" : "📍 Lock", _isCameraLocked ? EditorStyles.miniButtonMid : EditorStyles.miniButton))
            {
                _isCameraLocked = !_isCameraLocked;
                if (_isCameraLocked && tool.EditorCamera) SceneView.lastActiveSceneView?.AlignViewToObject(tool.EditorCamera.transform);
            }
            EditorGUILayout.EndHorizontal();

            if (!_isPreviewing)
            {
                GUI.backgroundColor = OkColor;
                if (GUILayout.Button("▶  Preview Animation", GUILayout.Height(28))) StartPreview(tool);
            }
            else
            {
                GUI.backgroundColor = BadColor;
                if (GUILayout.Button("⏹  Stop Preview", GUILayout.Height(28))) StopPreview();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawDebugSection(CameraTool tool)
        {
            if (tool.EditorRailCount > 0)
            {
                var sample = tool.EditorSampleAt(_splineProgress.floatValue);
                EditorGUILayout.Vector3Field("Pos", sample.position);
            }
        }

        private void DrawEventsSection()
        {
            _foldEvents = DrawSectionHeader("🔔  Lifecycle Events", _foldEvents);
            if (!_foldEvents) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_onChapterStart);
            EditorGUILayout.PropertyField(_onChapterEnd);
            EditorGUI.indentLevel--;
        }

        // ── Helpers ──────────────────────────────────────────────────

        private void DrawSuiteHeader()
        {
            var rect = EditorGUILayout.GetControlRect(false, 40);
            EditorGUI.DrawRect(rect, HeaderBg);
            
            // Thin Cyan Bottom Line
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 2, rect.width, 2), CyanAccent);

            var labelRect = new Rect(rect.x + 12, rect.y + 8, rect.width, 24);
            GUI.Label(labelRect, "MÉTRŌMA  |  CAMERA SUITE", _headerStyle);
            
            var versionStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.LowerRight, normal = { textColor = new Color(1,1,1,0.3f) } };
            GUI.Label(new Rect(rect.xMax - 70, rect.y + 18, 60, 15), "V2.0 PRO", versionStyle);
            
            EditorGUILayout.Space(8);
        }

        private static bool DrawSectionHeader(string title, bool foldout)
        {
            EditorGUILayout.Space(12);
            var rect = EditorGUILayout.GetControlRect(false, 24);
            
            // Thin subtle line above
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), LineColor);
            
            // Accent dash
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 6, 2, 12), CyanAccent);

            var style = new GUIStyle(EditorStyles.foldoutHeader) 
            { 
                fontSize = 12, 
                fontStyle = FontStyle.Bold,
            };
            
            return EditorGUI.Foldout(new Rect(rect.x + 10, rect.y + 4, rect.width - 10, 20), foldout, title.ToUpper(), true, style);
        }

        private static void DrawThinSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, LineColor);
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;
            
            _headerStyle = new GUIStyle(EditorStyles.label) 
            { 
                fontSize = 18, 
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            _statusOkStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = OkColor } };
            _statusBadStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 10, fontStyle = FontStyle.Bold, normal = { textColor = BadColor } };
        }

        // ── Logic ─────────────────────────────────────────────────────

        private void AutoScanChapters(CameraTool tool)
        {
            string[] guids = AssetDatabase.FindAssets("t:TimelineAsset");
            foreach (var guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (!asset.name.Contains("Camera")) continue;
                bool exists = false;
                for (int i = 0; i < tool.EditorChaptersCount(); i++) if (tool.EditorGetTimeline(i) == asset) exists = true;
                if (exists) continue;
                _chapters.arraySize++;
                var c = _chapters.GetArrayElementAtIndex(_chapters.arraySize - 1);
                c.FindPropertyRelative("name").stringValue = asset.name;
                c.FindPropertyRelative("timeline").objectReferenceValue = asset;
                c.FindPropertyRelative("debugColor").colorValue = Color.HSVToRGB(Random.value, 0.7f, 0.9f);
            }
        }

        private void GenerateTimelineClips(CameraTool tool, int index)
        {
            var timeline = tool.EditorGetTimeline(index);
            if (!timeline) return;
            var segs = tool.EditorSegments(index);
            Undo.RecordObject(timeline, "Gen Timeline");
            CameraToolTrack track = null;
            foreach (var t in timeline.GetOutputTracks()) if (t is CameraToolTrack ct) track = ct;
            if (!track) track = timeline.CreateTrack<CameraToolTrack>(null, "Camera");
            foreach (var clip in new List<TimelineClip>(track.GetClips())) track.DeleteClip(clip);
            double time = 0;
            for (int i = 0; i < segs.Count; i++)
            {
                var clip = track.CreateDefaultClip();
                clip.start = time; clip.duration = segs[i].duration;
                var asset = clip.asset as CameraToolClip;
                asset.Template.startProgress = (float)i / segs.Count;
                asset.Template.endProgress = (float)(i + 1) / segs.Count;
                asset.Template.easingCurve = new AnimationCurve(segs[i].easing.keys);
                time += segs[i].duration + segs[i].waitAtEnd;
            }
            AssetDatabase.SaveAssets();
        }

        private void StartPreview(CameraTool tool)
        {
            var chapter = _chapters.GetArrayElementAtIndex(_selectedChapterIndex);
            var segs = chapter.FindPropertyRelative("segments");
            if (segs.arraySize == 0) return;
            _previewSegStarts = new float[segs.arraySize];
            _previewSegDurations = new float[segs.arraySize];
            _previewSegCurves = new AnimationCurve[segs.arraySize];
            float time = 0;
            for (int i = 0; i < segs.arraySize; i++)
            {
                var s = segs.GetArrayElementAtIndex(i);
                _previewSegStarts[i] = time;
                _previewSegDurations[i] = s.FindPropertyRelative("duration").floatValue;
                _previewSegCurves[i] = s.FindPropertyRelative("easing").animationCurveValue;
                time += _previewSegDurations[i] + s.FindPropertyRelative("waitAtEnd").floatValue;
            }
            _previewTotalDuration = time;
            _previewStartTime = EditorApplication.timeSinceStartup;
            _isPreviewing = true;
            EditorApplication.update += PreviewUpdate;
        }

        private void StopPreview() { if (!_isPreviewing) return; _isPreviewing = false; EditorApplication.update -= PreviewUpdate; }

        private void PreviewUpdate()
        {
            if (!_isPreviewing || target == null) { StopPreview(); return; }
            float elapsed = (float)(EditorApplication.timeSinceStartup - _previewStartTime);
            if (elapsed >= _previewTotalDuration) { StopPreview(); return; }
            int count = _previewSegStarts.Length;
            float p = 0;
            for (int i = 0; i < count; i++)
            {
                if (elapsed < _previewSegStarts[i] + _previewSegDurations[i])
                {
                    float t = _previewSegCurves[i].Evaluate((elapsed - _previewSegStarts[i]) / _previewSegDurations[i]);
                    p = Mathf.Lerp((float)i / count, (float)(i + 1) / count, t);
                    break;
                }
            }
            ((CameraTool)target).EditorEvaluateAt(p);
            SceneView.RepaintAll();
        }

        private void SyncSceneView(SceneView sv)
        {
            if (!_isCameraLocked || Application.isPlaying) return;
            var cam = ((CameraTool)target).EditorCamera;
            if (cam == null) return;
            sv.pivot = cam.transform.position; sv.rotation = cam.transform.rotation; sv.size = 0;
            sv.Repaint();
        }
    }
}
