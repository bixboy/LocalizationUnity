using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Metroma.CameraTool.Timeline
{
    /// <summary>
    /// Timeline marker: switches the active rail in the CameraTool's spline chain.
    /// </summary>
    [Serializable]
    [DisplayName("Camera/🔀 Rail Switch")]
    [CustomStyle("CameraRailSwitchMarker")]
    public class CameraRailSwitchMarker : CameraMarkerBase
    {
        [Tooltip("Index of the rail to switch to in CameraTool.splineRails.")]
        [Min(0)]
        [SerializeField] private int railIndex;

        [Tooltip("If true, resets to chain mode instead of switching to a single rail.")]
        [SerializeField] private bool resetToChainMode;

        public int RailIndex => railIndex;
        public bool ResetToChainMode => resetToChainMode;

        public override void Execute(CameraTool tool)
        {
            if (resetToChainMode) tool.ResetToChainMode();
            else tool.SwitchToRail(railIndex);
        }
    }
}
