using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR
{
    // TODO: Check if we can ditch this hacky approach
    public class CameraPoseDriver : TrackedPoseDriver
    {
        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetLocalTransform(newPosition, Quaternion.Euler(newRotation.eulerAngles.x, 0, newRotation.eulerAngles.z));
        }
    }
}
