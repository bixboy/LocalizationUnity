using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Base class for all Camera Markers.
    /// Implements a Command Pattern to simplify adding new markers without editing CameraTool.
    /// </summary>
    [Serializable]
    public abstract class CameraMarkerBase : Marker, INotification
    {
        // Default ID for the notification system
        public virtual PropertyName id => new PropertyName(GetType().Name);
        
        public abstract void Execute(CameraTool tool);
    }
}
