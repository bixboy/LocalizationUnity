using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;

namespace Metroma.Camera.Timeline
{
    /// <summary>
    /// Timeline marker that triggers a transition to another cinematic chapter.
    /// Great for chaining different Timelines together smoothly.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/⏭ Next Chapter")]
    [CustomStyle("CameraEventMarker")]
    public class CameraNextChapterMarker : CameraMarkerBase
    {
        [Tooltip("Exact name of the chapter to play (from the CameraTool Chapters list).")]
        public string nextChapterName;

        [Tooltip("Duration of the blend from the current position to the start of the next chapter.")]
        public float blendDuration = 2.0f;

        public override void Execute(CameraTool tool)
        {
            if (tool != null && !string.IsNullOrEmpty(nextChapterName))
            {
                tool.PlayChapter(nextChapterName, blendDuration);
            }
        }
    }
}
