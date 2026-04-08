using UnityEngine;
using UnityEditor;
using Metroma.CameraTool;
using Dreamteck.Splines;
using System.Collections.Generic;

namespace Metroma.CameraTool.Editor
{
    /// <summary>
    /// Scene View gizmos for <see cref="CameraTool"/>:
    /// Per-segment colored spline path, node labels, progress indicator,
    /// camera frustum, and LookAt target line.
    /// </summary>
    public static class CameraToolGizmos
    {
        private static readonly Color ProgressSphereColor = new Color(0.1f, 0.9f, 0.4f, 0.9f);
        private static readonly Color DirectionColor = new Color(1f, 0.6f, 0.1f, 0.8f);
        private static readonly Color FrustumColor = new Color(0.2f, 0.6f, 1f, 0.8f);
        private static readonly Color LookAtLineColor = new Color(1f, 0.3f, 0.3f, 0.7f);
        private static readonly Color NodeLabelColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        private static readonly Color SeparatorColor = new Color(1f, 1f, 1f, 0.5f);

        private const int SAMPLES_PER_SEGMENT = 16;
        private const float SPHERE_RADIUS = 0.15f;
        private const float PATH_DOT_RADIUS = 0.06f;
        private const float NODE_SPHERE_RADIUS = 0.2f;
        private const float DIRECTION_ARROW_LENGTH = 1.5f;

        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void DrawGizmos(CameraTool tool, GizmoType gizmoType)
        {
            List<SplineComputer> rails = tool.EditorSplineRails;
            if (rails == null || rails.Count == 0) return;

            DrawSegmentedSplinePath(tool, rails);
            DrawProgressPoint(tool);
            DrawCameraFrustum(tool);
            DrawLookAtLine(tool);
            DrawProgressLabel(tool);
        }

        // ══════════════════════════════════════════════════════════════
        // Per-Segment Colored Path
        // ══════════════════════════════════════════════════════════════

        private static void DrawSegmentedSplinePath(CameraTool tool, List<SplineComputer> rails)
        {
            int totalSegments = tool.EditorTotalSegmentCount;
            if (totalSegments == 0) return;

            // Get selected chapter color if we are in editor (placeholder logic for now, using cyan as default)
            Color mainColor = Color.cyan;
            
            int globalSegIndex = 0;

            for (int r = 0; r < rails.Count; r++)
            {
                SplineComputer spline = rails[r];
                if (spline == null) continue;

                int nodeCount = spline.pointCount;
                int splineSegments = Mathf.Max(0, nodeCount - 1);

                for (int seg = 0; seg < splineSegments; seg++)
                {
                    // Use a slightly varying shade of the main color to distinguish nodes
                    float shade = 0.8f + (globalSegIndex % 2 == 0 ? 0.2f : 0f);
                    Gizmos.color = mainColor * shade;
                    Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.6f);

                    float segStart = (float)seg / splineSegments;
                    float segEnd = (float)(seg + 1) / splineSegments;

                    for (int s = 0; s <= SAMPLES_PER_SEGMENT; s++)
                    {
                        float t = Mathf.Lerp(segStart, segEnd, s / (float)SAMPLES_PER_SEGMENT);
                        SplineSample sample = spline.Evaluate(t);
                        Gizmos.DrawSphere(sample.position, PATH_DOT_RADIUS);
                    }

                    globalSegIndex++;
                }

                // ── Node labels & separators ──
                for (int n = 0; n < nodeCount; n++)
                {
                    float t = nodeCount > 1 ? (float)n / (nodeCount - 1) : 0f;
                    SplineSample nodeSample = spline.Evaluate(t);
                    Vector3 nodePos = nodeSample.position;

                    // Node sphere
                    Gizmos.color = SeparatorColor;
                    Gizmos.DrawWireSphere(nodePos, NODE_SPHERE_RADIUS);

                    // Label
                    string railPrefix = rails.Count > 1 ? $"R{r}:" : "";
                    GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 11,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = NodeLabelColor }
                    };

                    Vector3 labelPos = nodePos + Vector3.up * 0.6f;
                    Handles.Label(labelPos, $"{railPrefix}N{n}", labelStyle);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════
        // Progress & Camera
        // ══════════════════════════════════════════════════════════════

        private static void DrawProgressPoint(CameraTool tool)
        {
            SplineSample sample = tool.EditorSampleAt(tool.SplineProgress);

            Gizmos.color = ProgressSphereColor;
            Gizmos.DrawSphere(sample.position, SPHERE_RADIUS);

            Gizmos.color = DirectionColor;
            Vector3 end = sample.position + (Vector3)sample.forward * DIRECTION_ARROW_LENGTH;
            Gizmos.DrawLine(sample.position, end);
            Gizmos.DrawSphere(end, SPHERE_RADIUS * 0.5f);
        }

        private static void DrawCameraFrustum(CameraTool tool)
        {
            UnityEngine.Camera cam = tool.EditorCamera;
            if (cam == null)
                return;

            SplineSample sample = tool.EditorSampleAt(tool.SplineProgress);

            Gizmos.color = FrustumColor;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(sample.position, sample.rotation, Vector3.one);
            Gizmos.DrawFrustum(
                Vector3.zero,
                cam.fieldOfView,
                cam.farClipPlane * 0.05f,
                cam.nearClipPlane,
                cam.aspect
            );
            Gizmos.matrix = oldMatrix;
        }

        private static void DrawLookAtLine(CameraTool tool)
        {
            Transform lookAt = tool.EditorLookAtTarget;
            if (lookAt == null)
                return;

            SplineSample sample = tool.EditorSampleAt(tool.SplineProgress);

            Gizmos.color = LookAtLineColor;
            Gizmos.DrawLine(sample.position, lookAt.position);
            Gizmos.DrawWireSphere(lookAt.position, 0.3f);
        }

        private static void DrawProgressLabel(CameraTool tool)
        {
            SplineSample sample = tool.EditorSampleAt(tool.SplineProgress);
            Vector3 labelPos = sample.position + Vector3.up * 1.2f;

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ProgressSphereColor }
            };

            Handles.Label(labelPos, $"{tool.SplineProgress * 100f:F1}%", style);
        }
    }
}
