using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using Metroma.Camera.Timeline;

namespace Metroma.Camera.Editor
{
    /// <summary>
    /// Custom track editor for <see cref="CameraToolTrack"/>.
    /// Auto-names clips on creation for better readability.
    /// </summary>
    [CustomTimelineEditor(typeof(CameraToolTrack))]
    public class CameraToolTrackEditor : TrackEditor
    {
        public override void OnCreate(TrackAsset track, TrackAsset copiedFrom)
        {
            base.OnCreate(track, copiedFrom);
            track.name = "🎬 Camera Spline";
        }

        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            TrackDrawOptions options = base.GetTrackOptions(track, binding);
            options.errorText = binding == null ? "⚠ No CameraTool bound — drag one here" : null;
            return options;
        }
    }
}
