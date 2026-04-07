using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Metroma.Camera.Timeline
{

    [Serializable]
    [DisplayName("Camera Event")]
    [CustomStyle("CameraEventMarker")]
    public class CameraEventMarker : Marker, INotification
    {
        [Tooltip("Event name forwarded to CameraTool.TriggerTimelineEvent().")]
        [SerializeField] private string eventName;

        [Tooltip("If true, the event fires even when the Timeline is scrubbed in reverse.")]
        [SerializeField] private bool retroactive;

        [Tooltip("If true, the event fires only once per Timeline playback.")]
        [SerializeField] private bool emitOnce;

        /// <summary>Unique notification id for the Playable system.</summary>
        public PropertyName id => new PropertyName(eventName);

        /// <summary>The event identifier forwarded to <see cref="CameraTool.TriggerTimelineEvent"/>.</summary>
        public string EventName => eventName;

        /// <inheritdoc cref="retroactive"/>
        public bool Retroactive => retroactive;

        /// <inheritdoc cref="emitOnce"/>
        public bool EmitOnce => emitOnce;

        public override void OnInitialize(TrackAsset aPent)
        {
            base.OnInitialize(aPent);
        }
    }
}
