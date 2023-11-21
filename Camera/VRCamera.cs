using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    public class VRCamera
    {
        /// <summary>
        /// Inject a <see cref="TrackedPoseDriver"/> into a <see cref="Camera"/> game object, which will track the HMD device  
        /// </summary>
        /// <param name="camera">The camera to track to the HMD</param>
        public static void InitializeHMDCamera(Camera camera)
        {
            var driver = camera.gameObject.AddComponent<TrackedPoseDriver>();

            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;

            driver.positionAction = Actions.XR_HeadPosition;
            driver.rotationAction = Actions.XR_HeadRotation;
            driver.trackingStateInput = new InputActionProperty(Actions.XR_HeadTrackingState);
        }
    }
}