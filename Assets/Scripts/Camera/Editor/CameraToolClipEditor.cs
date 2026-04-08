using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using Metroma.CameraTool.Timeline;

namespace Metroma.CameraTool.Editor
{
    /// <summary>
    /// Enhanced clip editor for <see cref="CameraToolClip"/> in the Timeline window.
    /// Renders a rich overlay with gradient background, progress indicators,
    /// easing curve with filled area, and dynamic clip coloring.
    /// </summary>
    [CustomTimelineEditor(typeof(CameraToolClip))]
    public class CameraToolClipEditor : ClipEditor
    {
        // ── Colors ───────────────────────────────────────────────────
        private static readonly Color GradientStart = new Color(0.08f, 0.35f, 0.70f, 0.35f);
        private static readonly Color GradientEnd = new Color(0.05f, 0.65f, 0.45f, 0.25f);

        private static readonly Color CurveLineColor = new Color(0.3f, 0.95f, 0.5f, 0.85f);
        private static readonly Color CurveFillColor = new Color(0.2f, 0.85f, 0.4f, 0.12f);

        private static readonly Color TextColor = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color TextShadowColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color DimTextColor = new Color(0.8f, 0.8f, 0.8f, 0.6f);

        private static readonly Color ProgressBarBg = new Color(0f, 0f, 0f, 0.3f);
        private static readonly Color ProgressBarStart = new Color(0.2f, 0.55f, 1f, 0.8f);
        private static readonly Color ProgressBarEnd = new Color(0.1f, 0.9f, 0.45f, 0.8f);

        private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color HighlightAccent = new Color(0.2f, 0.6f, 1f);

        private const int CURVE_SAMPLES = 48;
        private const float PROGRESS_BAR_HEIGHT = 4f;
        private const float PADDING = 4f;

        // ── Clip Options ─────────────────────────────────────────────

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            CameraToolClip asset = clip.asset as CameraToolClip;
            ClipDrawOptions options = base.GetClipOptions(clip);

            if (asset != null)
            {
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty templateProp = so.FindProperty("template");
                float start = templateProp.FindPropertyRelative("startProgress").floatValue;
                float end = templateProp.FindPropertyRelative("endProgress").floatValue;

                // Dynamic highlight color based on progress range
                float midProgress = (start + end) * 0.5f;
                options.highlightColor = Color.Lerp(
                    new Color(0.15f, 0.45f, 0.9f),
                    new Color(0.1f, 0.8f, 0.5f),
                    midProgress
                );

                options.tooltip = $"🎬 Camera Spline\n{start * 100f:F0}% → {end * 100f:F0}%";
            }

            return options;
        }

        // ── Background Drawing ───────────────────────────────────────

        public override void DrawBackground(TimelineClip clip, ClipBackgroundRegion region)
        {
            CameraToolClip asset = clip.asset as CameraToolClip;
            if (asset == null)
                return;

            Rect rect = region.position;
            if (rect.width < 10f || rect.height < 10f)
                return;

            // ── Read data ──
            SerializedObject so = new SerializedObject(asset);
            SerializedProperty templateProp = so.FindProperty("template");
            float startProgress = templateProp.FindPropertyRelative("startProgress").floatValue;
            float endProgress = templateProp.FindPropertyRelative("endProgress").floatValue;
            AnimationCurve curve = templateProp.FindPropertyRelative("easingCurve").animationCurveValue;

            DrawGradientOverlay(rect);
            DrawProgressHeader(rect, startProgress, endProgress);
            DrawMiniProgressBar(rect, startProgress, endProgress);

            if (rect.height > 35f && rect.width > 60f)
            {
                DrawEasingCurveWithFill(rect, curve);
            }

            if (rect.width > 80f)
            {
                DrawDirectionIndicator(rect, startProgress, endProgress);
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Drawing Methods
        // ══════════════════════════════════════════════════════════════

        private static void DrawGradientOverlay(Rect rect)
        {
            int strips = Mathf.Max(1, (int)(rect.width / 4f));
            float stripWidth = rect.width / strips;

            for (int i = 0; i < strips; i++)
            {
                float t = i / (float)(strips - 1);
                Color color = Color.Lerp(GradientStart, GradientEnd, t);

                Rect strip = new Rect(
                    rect.x + i * stripWidth,
                    rect.y,
                    stripWidth + 1f,
                    rect.height
                );

                EditorGUI.DrawRect(strip, color);
            }
        }

        private static void DrawProgressHeader(Rect rect, float start, float end)
        {
            // ── Main label ──
            string mainLabel = $"{start * 100f:F0}%  →  {end * 100f:F0}%";

            GUIStyle shadowStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 11,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = TextShadowColor },
                padding = new RectOffset((int)PADDING + 2, 0, 3, 0)
            };

            GUIStyle mainStyle = new GUIStyle(shadowStyle)
            {
                normal = { textColor = TextColor }
            };

            // Shadow
            Rect shadowRect = rect;
            shadowRect.x += 1f;
            shadowRect.y += 1f;
            GUI.Label(shadowRect, mainLabel, shadowStyle);

            // Text
            GUI.Label(rect, mainLabel, mainStyle);

            if (rect.width > 120f)
            {
                GUIStyle iconStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    fontSize = 9,
                    alignment = TextAnchor.UpperRight,
                    normal = { textColor = DimTextColor },
                    padding = new RectOffset(0, 6, 4, 0)
                };

                GUI.Label(rect, "🎬 Spline", iconStyle);
            }
        }

        private static void DrawMiniProgressBar(Rect rect, float start, float end)
        {
            float barY = rect.y + 20f;
            if (barY + PROGRESS_BAR_HEIGHT > rect.yMax - 4f)
                return;

            Rect barBg = new Rect(
                rect.x + PADDING,
                barY,
                rect.width - PADDING * 2f,
                PROGRESS_BAR_HEIGHT
            );

            EditorGUI.DrawRect(barBg, ProgressBarBg);

            float fillX = barBg.x + barBg.width * start;
            float fillW = barBg.width * (end - start);

            Rect fillRect = new Rect(fillX, barBg.y, Mathf.Max(fillW, 2f), PROGRESS_BAR_HEIGHT);

            int fillStrips = Mathf.Max(1, (int)(fillRect.width / 3f));
            float fillStripW = fillRect.width / fillStrips;

            for (int i = 0; i < fillStrips; i++)
            {
                float t = i / (float)Mathf.Max(1, fillStrips - 1);
                Color c = Color.Lerp(ProgressBarStart, ProgressBarEnd, t);

                Rect s = new Rect(
                    fillRect.x + i * fillStripW,
                    fillRect.y,
                    fillStripW + 1f,
                    fillRect.height
                );

                EditorGUI.DrawRect(s, c);
            }

            // Start/End markers
            EditorGUI.DrawRect(new Rect(fillRect.x, barBg.y - 1f, 2f, PROGRESS_BAR_HEIGHT + 2f), TextColor);
            EditorGUI.DrawRect(new Rect(fillRect.xMax - 2f, barBg.y - 1f, 2f, PROGRESS_BAR_HEIGHT + 2f), TextColor);
        }

        private static void DrawEasingCurveWithFill(Rect clipRect, AnimationCurve curve)
        {
            if (curve == null || curve.length == 0)
                return;

            float curveAreaHeight = (clipRect.height - 30f) * 0.7f;
            if (curveAreaHeight < 10f)
                return;

            float curveY = clipRect.yMax - curveAreaHeight - PADDING;
            float curveX = clipRect.x + PADDING + 2f;
            float curveWidth = clipRect.width - PADDING * 2f - 4f;

            if (curveWidth < 20f)
                return;

            // ── Separator line ──
            Rect sepRect = new Rect(curveX, curveY - 2f, curveWidth, 1f);
            EditorGUI.DrawRect(sepRect, SeparatorColor);

            // ── Fill area ──
            for (int i = 0; i < CURVE_SAMPLES; i++)
            {
                float t0 = i / (float)CURVE_SAMPLES;
                float t1 = (i + 1) / (float)CURVE_SAMPLES;
                float val0 = Mathf.Clamp01(curve.Evaluate(t0));
                float val1 = Mathf.Clamp01(curve.Evaluate(t1));
                float avgVal = (val0 + val1) * 0.5f;

                float x = curveX + t0 * curveWidth;
                float w = curveWidth / CURVE_SAMPLES + 1f;
                float fillTop = curveY + (1f - avgVal) * curveAreaHeight;
                float fillBottom = curveY + curveAreaHeight;

                Rect fillRect = new Rect(x, fillTop, w, fillBottom - fillTop);
                EditorGUI.DrawRect(fillRect, CurveFillColor);
            }

            // ── Curve line ──
            Handles.color = CurveLineColor;
            Vector3 prevPoint = Vector3.zero;

            for (int i = 0; i <= CURVE_SAMPLES; i++)
            {
                float t = i / (float)CURVE_SAMPLES;
                float val = Mathf.Clamp01(curve.Evaluate(t));

                Vector3 point = new Vector3(
                    curveX + t * curveWidth,
                    curveY + (1f - val) * curveAreaHeight,
                    0f
                );

                if (i > 0)
                {
                    Handles.DrawLine(prevPoint, point);
                }

                prevPoint = point;
            }

            // ── Curve endpoints ──
            float startVal = Mathf.Clamp01(curve.Evaluate(0f));
            float endVal = Mathf.Clamp01(curve.Evaluate(1f));

            Vector2 startPos = new Vector2(curveX, curveY + (1f - startVal) * curveAreaHeight);
            Vector2 endPos = new Vector2(curveX + curveWidth, curveY + (1f - endVal) * curveAreaHeight);

            Rect startDot = new Rect(startPos.x - 2f, startPos.y - 2f, 5f, 5f);
            Rect endDot = new Rect(endPos.x - 2f, endPos.y - 2f, 5f, 5f);

            EditorGUI.DrawRect(startDot, CurveLineColor);
            EditorGUI.DrawRect(endDot, CurveLineColor);
        }

        private static void DrawDirectionIndicator(Rect rect, float start, float end)
        {
            bool isForward = end >= start;

            GUIStyle dirStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9,
                alignment = TextAnchor.LowerRight,
                normal = { textColor = DimTextColor },
                padding = new RectOffset(0, 6, 0, 3)
            };

            string dirText = isForward ? "▶ Forward" : "◀ Reverse";
            GUI.Label(rect, dirText, dirStyle);
        }
    }
}
