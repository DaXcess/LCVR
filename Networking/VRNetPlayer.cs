using UnityEngine;

namespace LCVR.Networking
{
    public class VRNetPlayer : MonoBehaviour
    {
        public Transform leftHandTarget;
        public Transform rightHandTarget;

        public Transform leftItemHolder;
        public Transform rightItemHolder;

        public Transform camera;

        private void Awake()
        {
            leftHandTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/LeftArm_target").transform;
            rightHandTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/RightArm_target").transform;
            camera = gameObject.Find("ScavengerModel/metarig/CameraContainer/MainCamera").transform;

            // Set up item holders
            var rightHandParent = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/shoulder.R/arm.R_upper/arm.R_lower/hand.R").transform;
            var leftHandParent = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/shoulder.L/arm.L_upper/arm.L_lower/hand.L").transform;

            var rightHolder = new GameObject("Right Hand Item Holder");
            var leftHolder = new GameObject("Left Hand Item Holder");

            rightItemHolder = rightHolder.transform;
            rightItemHolder.SetParent(rightHandParent, false);
            rightItemHolder.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
            rightItemHolder.localEulerAngles = new Vector3(356.3837f, 357.6979f, 0.1453f);

            leftItemHolder = leftHolder.transform;
            leftItemHolder.SetParent(leftHandParent, false);
            leftItemHolder.localPosition = new Vector3(0.018f, 0.045f, -0.042f);
            leftItemHolder.localEulerAngles = new Vector3(360f - 356.3837f, 357.6979f, 0.1453f);
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
