using UnityEngine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine.Timeline;
using Metroma.Camera.Timeline;

namespace Metroma.Camera.Editor
{
    /// <summary>
    /// Custom marker editor for <see cref="CameraEventMarker"/> in the Timeline window.
    /// Displays a rich tooltip with the event name and shows an error if the name is empty.
    /// </summary>
    [CustomTimelineEditor(typeof(CameraEventMarker))]
    public class CameraEventMarkerEditor : MarkerEditor
    {
        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            CameraEventMarker eventMarker = marker as CameraEventMarker;

            string tooltip = eventMarker != null && !string.IsNullOrEmpty(eventMarker.EventName)
                ? $"⚡ Camera Event: {eventMarker.EventName}"
                : "⚠ Camera Event — No Name Set!";

            return new MarkerDrawOptions
            {
                tooltip = tooltip,
                errorText = (eventMarker != null && string.IsNullOrEmpty(eventMarker.EventName))
                    ? "Event name is empty!"
                    : null
            };
        }
    }
}
