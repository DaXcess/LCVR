using UnityEngine;

namespace LCVR.Networking
{
    public class VRNetPlayer : MonoBehaviour
    {
        private Transform leftHandTarget;
        private Transform rightHandTarget;
        private Transform camera;

        private void Awake()
        {
            leftHandTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/LeftArm_target").transform;
            rightHandTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/RightArm_target").transform;
            camera = gameObject.Find("ScavengerModel/metarig/CameraContainer/MainCamera").transform;
        }

        public void UpdateTargetTransforms(DNet.Rig rig)
        {
            leftHandTarget.transform.position = rig.leftHandPosition;
            leftHandTarget.transform.eulerAngles = rig.leftHandEulers;

            rightHandTarget.transform.position = rig.rightHandPosition;
            rightHandTarget.transform.eulerAngles = rig.rightHandEulers;

            camera.transform.eulerAngles = rig.cameraEulers;
        }
    }
}
