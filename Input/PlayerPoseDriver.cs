using UnityEngine;
using UnityEngine.InputSystem.XR;

namespace LethalCompanyVR.Input
{
    public class PlayerPoseDriver : TrackedPoseDriver
    {
        protected override void SetLocalTransform(Vector3 newPosition, Quaternion newRotation)
        {
            base.SetLocalTransform(newPosition, Quaternion.Euler(0, newRotation.eulerAngles.y, 0));
        }
    }
}
