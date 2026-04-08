using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Timeline clip asset that drives <see cref="CameraTool"/> along a spline segment.
    /// Each clip defines a start/end progress and an easing curve.
    /// </summary>
    [Serializable]
    public class CameraToolClip : PlayableAsset, ITimelineClipAsset
    {
        [SerializeField] private CameraToolBehaviour template = new CameraToolBehaviour();

        /// <summary>Direct access for editor tools to configure clip data.</summary>
        public CameraToolBehaviour Template => template;

        /// <summary>
        /// Clip capabilities — supports blending for smooth transitions between clips.
        /// </summary>
        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<CameraToolBehaviour>.Create(graph, template);
        }
    }
}
