using UnityEngine;
using UnityEditor;
using Metroma.CameraTool.Timeline;

namespace Metroma.CameraTool.Editor
{
    /// <summary>
    /// Custom Inspector displayed when a <see cref="CameraToolClip"/> is selected
    /// in the Timeline window. Shows styled progress controls, easing curve,
    /// and preview buttons.
    /// </summary>
    [CustomEditor(typeof(CameraToolClip))]
    public class CameraToolClipInspector : UnityEditor.Editor
    {
        // ── Serialized Properties ──
        private SerializedProperty _template;
        private SerializedProperty _startProgress;
        private SerializedProperty _endProgress;
        private SerializedProperty _easingCurve;
        private SerializedProperty _lookAtWeight;

        // ── Colors ──
        private static readonly Color AccentColor = new Color(0.2f, 0.6f, 1f);
        private static readonly Color HeaderBg = new Color(0.12f, 0.12f, 0.12f, 0.5f);
        private static readonly Color BarBg = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        private static readonly Color BarStartColor = new Color(0.2f, 0.55f, 1f, 0.9f);
        private static readonly Color BarEndColor = new Color(0.1f, 0.9f, 0.45f, 0.9f);
        private static readonly Color RangeColor = new Color(0.3f, 0.7f, 1f, 0.3f);

        // ── Styles ──
        private static GUIStyle _headerStyle;
        private static GUIStyle _percentStyle;

        private void OnEnable()
        {
            _template = serializedObject.FindProperty("template");
            _startProgress = _template.FindPropertyRelative("startProgress");
            _endProgress = _template.FindPropertyRelative("endProgress");
            _easingCurve = _template.FindPropertyRelative("easingCurve");
            _lookAtWeight = _template.FindPropertyRelative("lookAtWeight");
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawClipHeader();
            EditorGUILayout.Space(4);
            DrawProgressSection();
            EditorGUILayout.Space(4);
            DrawEasingSection();
            EditorGUILayout.Space(4);
            DrawLookAtSection();
            EditorGUILayout.Space(4);
            DrawPreviewButtons();

            serializedObject.ApplyModifiedProperties();
        }

        // ══════════════════════════════════════════════════════════════
        // Header
        // ══════════════════════════════════════════════════════════════

        private void DrawClipHeader()
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, 28);
            EditorGUI.DrawRect(headerRect, HeaderBg);

            // Accent bar
            Rect accentRect = new Rect(headerRect.x, headerRect.yMax - 2, headerRect.width, 2);
            EditorGUI.DrawRect(accentRect, AccentColor);

            GUI.Label(headerRect, "  🎬  Camera Spline Clip", _headerStyle);
        }

        // ══════════════════════════════════════════════════════════════
        // Progress Section
        // ══════════════════════════════════════════════════════════════

        private void DrawProgressSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Spline Progress Range", EditorStyles.miniBoldLabel);

            // ── Sliders ──
            EditorGUILayout.PropertyField(_startProgress, new GUIContent("Start Progress"));
            EditorGUILayout.PropertyField(_endProgress, new GUIContent("End Progress"));

            // ── Visual range bar ──
            EditorGUILayout.Space(4);
            Rect barRect = EditorGUILayout.GetControlRect(false, 22);

            // Background
            EditorGUI.DrawRect(barRect, BarBg);

            float start = _startProgress.floatValue;
            float end = _endProgress.floatValue;

            // Active range highlight
            float rangeLeft = barRect.x + barRect.width * Mathf.Min(start, end);
            float rangeRight = barRect.x + barRect.width * Mathf.Max(start, end);
            Rect rangeRect = new Rect(rangeLeft, barRect.y, rangeRight - rangeLeft, barRect.height);
            EditorGUI.DrawRect(rangeRect, RangeColor);

            // Start pip
            Rect startPip = new Rect(barRect.x + barRect.width * start - 1f, barRect.y, 3f, barRect.height);
            EditorGUI.DrawRect(startPip, BarStartColor);

            // End pip
            Rect endPip = new Rect(barRect.x + barRect.width * end - 1f, barRect.y, 3f, barRect.height);
            EditorGUI.DrawRect(endPip, BarEndColor);

            // Labels
            string barLabel = $"{start * 100f:F0}%                →                {end * 100f:F0}%";
            GUI.Label(barRect, barLabel, _percentStyle);

            // ── Direction indicator ──
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            bool isForward = end >= start;
            GUIStyle dirStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = isForward ? new Color(0.3f, 0.9f, 0.4f) : new Color(1f, 0.5f, 0.2f) }
            };

            string dir = isForward ? "▶ Forward" : "◀ Reverse";
            float range = Mathf.Abs(end - start) * 100f;
            EditorGUILayout.LabelField($"{dir}  ({range:F0}% of spline)", dirStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════
        // Easing Section
        // ══════════════════════════════════════════════════════════════

        private void DrawEasingSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Easing Curve", EditorStyles.miniBoldLabel);

            // Large curve field
            Rect curveRect = EditorGUILayout.GetControlRect(false, 80);
            _easingCurve.animationCurveValue = EditorGUI.CurveField(
                curveRect,
                _easingCurve.animationCurveValue,
                AccentColor,
                new Rect(0, 0, 1, 1)
            );

            // Preset buttons
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Linear", EditorStyles.miniButton))
            {
                _easingCurve.animationCurveValue = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            }

            if (GUILayout.Button("Ease In-Out", EditorStyles.miniButton))
            {
                _easingCurve.animationCurveValue = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }

            if (GUILayout.Button("Ease In", EditorStyles.miniButton))
            {
                _easingCurve.animationCurveValue = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 0f),
                    new Keyframe(1f, 1f, 2f, 0f)
                );
            }

            if (GUILayout.Button("Ease Out", EditorStyles.miniButton))
            {
                _easingCurve.animationCurveValue = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 2f),
                    new Keyframe(1f, 1f, 0f, 0f)
                );
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawLookAtSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("🎯 LookAt Weight", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_lookAtWeight, new GUIContent("Weight (0 = Spline, 1 = Target)"));
            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════════════════════════════
        // Preview Buttons
        // ══════════════════════════════════════════════════════════════

        private void DrawPreviewButtons()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("📍 Preview Start", GUILayout.Height(24)))
            {
                PreviewAtProgress(_startProgress.floatValue);
            }

            if (GUILayout.Button("📍 Preview End", GUILayout.Height(24)))
            {
                PreviewAtProgress(_endProgress.floatValue);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void PreviewAtProgress(float progress)
        {
            // Find the CameraTool in the scene
            CameraTool tool = Object.FindFirstObjectByType<CameraTool>();
            if (tool == null)
            {
                Debug.LogWarning("[CameraToolClip] No CameraTool found in scene for preview.");
                return;
            }

            tool.EditorEvaluateAt(progress);
            SceneView.RepaintAll();

            // Focus scene view on the evaluated position
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                Dreamteck.Splines.SplineSample sample = tool.EditorSampleAt(progress);
                sv.LookAt(sample.position, Quaternion.LookRotation(sample.forward, sample.up), 5f);
                sv.Repaint();
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Styles
        // ══════════════════════════════════════════════════════════════

        private static void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = AccentColor },
                fixedHeight = 28
            };

            _percentStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
        }
    }
}
