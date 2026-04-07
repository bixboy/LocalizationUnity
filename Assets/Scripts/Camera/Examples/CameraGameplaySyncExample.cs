using UnityEngine;
using System;
using Metroma.Camera;
using Metroma.Camera.Modifiers;
using Metroma.Camera.Timeline;

namespace Metroma.Camera.Examples
{
    /// <summary>
    /// Example script for Gameplay Programmers to demonstrate how to sync
    /// game systems (UI, Audio, VFX) with the Camera Suite events.
    /// </summary>
    public class CameraGameplaySyncExample : MonoBehaviour
    {
        private void OnEnable()
        {
            // IMPORTANT: Access the Active instance via the Service Pattern
            if (CameraTool.Active == null) return;

            // 1. Subscribe to Camera State changes (FollowRail, Transitioning, StaticPose, ReturningToRail)
            CameraTool.Active.OnStateChanged += HandleStateChanged;

            // 2. Subscribe to Static Pose arrival (perfect for triggering Minigames or Interaction UI)
            CameraTool.Active.OnPoseEventReached += HandleMinigameStart;

            // 3. Subscribe to Chapter transitions (when a new Timeline starts its blend)
            CameraTool.Active.OnChapterEventStarted += HandleChapterSwitch;

            // 4. Subscribe to any Timeline Marker hit
            CameraTool.Active.OnMarkerEventHit += HandleMarker;

            // 5. Subscribe to Slow-Motion state changes
            CameraTimeHandler.OnSlowMoStateChanged += HandleSlowMo;
        }

        private void OnDisable()
        {
            // ALWAYS Unsubscribe to avoid memory leaks or null references when switching scenes
            if (CameraTool.Active != null)
            {
                CameraTool.Active.OnStateChanged -= HandleStateChanged;
                CameraTool.Active.OnPoseEventReached -= HandleMinigameStart;
                CameraTool.Active.OnChapterEventStarted -= HandleChapterSwitch;
                CameraTool.Active.OnMarkerEventHit -= HandleMarker;
            }

            CameraTimeHandler.OnSlowMoStateChanged -= HandleSlowMo;
        }

        private void HandleStateChanged(CameraState state)
        {
            Debug.Log($"[SyncExample] Camera State is now: <color=cyan>{state}</color>");
        }

        private void HandleMinigameStart(CameraPose pose)
        {
            Debug.Log("[SyncExample] <color=green>Camera reached static pose.</color> You can now enable Minigame Input.");
            // Example: GameInput.SetMode(InputMode.Minigame);
            // Example: UIController.Instance.FadeInMinigameHUD();
        }

        private void HandleChapterSwitch(CameraChapter chapter)
        {
            Debug.Log($"[SyncExample] Transitioning to Chapter: <b>{chapter.name}</b>. Syncing Audio Mixer...");
            // Example: AudioManager.SetSnapShot(chapter.name);
        }

        private void HandleMarker(CameraMarkerBase marker)
        {
            Debug.Log($"[SyncExample] Marker Hit: {marker.GetType().Name}");
        }

        private void HandleSlowMo(bool isActive, float currentScale)
        {
            if (isActive)
            {
                Debug.Log($"[SyncExample] Slow Motion <color=yellow>STARTED</color>. Current Scale: {currentScale}");
                // Example: PostProcessService.SetVignette(0.5f);
            }
            else
            {
                Debug.Log("[SyncExample] Slow Motion <color=white>ENDED</color>. Restoring normal time.");
                // Example: PostProcessService.SetVignette(0f);
            }
        }
    }
}
