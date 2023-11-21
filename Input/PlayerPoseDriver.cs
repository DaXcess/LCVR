using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    // TODO: Check if we can ditch this hacky approach
    public class PlayerPoseDriver : TrackedPoseDriver
    {
        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetLocalTransform(newPosition, Quaternion.Euler(0, newRotation.eulerAngles.y, 0));
        }
    }
}
