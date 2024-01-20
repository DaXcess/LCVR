using GameNetcodeStuff;
using LCVR.Input;
using UnityEngine;

using CrouchState = LCVR.Networking.DNet.Rig.CrouchState;

namespace LCVR.Networking
{
    public class VRNetPlayer : MonoBehaviour
    {
        private PlayerControllerB playerController;

        public Transform xrOrigin;
        public Transform leftController;
        public Transform rightController;
        public Transform leftHandVRTarget;
        public Transform rightHandVRTarget;

        public Transform leftHandTarget;
        public Transform rightHandTarget;

        public Transform leftItemHolder;
        public Transform rightItemHolder;

        public FingerCurler leftFingerCurler;
        public FingerCurler rightFingerCurler;

        public Transform camera;

        private float cameraFloorOffset;
        private float rotationOffset;

        private Vector3 cameraPosAccounted;

        private CrouchState crouchState = CrouchState.None;
        private float crouchOffset;

        public bool RebuildingRig { get; private set; } = false;

        private void Awake()
        {
            playerController = GetComponent<PlayerControllerB>();

            // Because I want to transmit local controller positions and angles (since it's much cleaner)
            // I decided to somewhat recreate the XR Origin setup so that all the offsets are correct
            xrOrigin = new GameObject("XR Origin").transform;
            xrOrigin.localPosition = Vector3.zero;
            xrOrigin.localEulerAngles = Vector3.zero;
            xrOrigin.localScale = Vector3.one;

            // Create controller objects & VR targets
            leftController = new GameObject("Left Controller").transform;
            rightController = new GameObject("Right Controller").transform;
            leftHandVRTarget = new GameObject("Left Hand VR Target").transform;
            rightHandVRTarget = new GameObject("Right Hand VR Target").transform;

            leftController.SetParent(xrOrigin, false);
            rightController.SetParent(xrOrigin, false);

            leftHandVRTarget.SetParent(leftController, false);
            rightHandVRTarget.SetParent(rightController, false);

            rightHandVRTarget.localPosition = new Vector3(0.0279f, 0.0353f, -0.0044f);
            rightHandVRTarget.localEulerAngles = new Vector3(0, 90, 168);

            leftHandVRTarget.localPosition = new Vector3(-0.0279f, 0.0353f, 0.0044f);
            leftHandVRTarget.localEulerAngles = new Vector3(0, 270, 192);

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

            // Set up finger curlers
            rightFingerCurler = new FingerCurler(rightHandParent, false);
            leftFingerCurler = new FingerCurler(leftHandParent, true);
        }

        private void Update()
        {
            // Apply crouch offset
            crouchOffset = Mathf.Lerp(crouchOffset, crouchState switch
            {
                CrouchState.Button => -1,
                _ => 0,
            }, 0.2f);

            // Apply origin transforms
            xrOrigin.position = transform.position;

            // If we are in special animation allow 6 DOF but don't update player position
            if (!playerController.inSpecialInteractAnimation)
                xrOrigin.position = new Vector3(transform.position.x - cameraPosAccounted.x * 1.5f, transform.position.y, transform.position.z - cameraPosAccounted.z * 1.5f);
            else
                xrOrigin.position = transform.position /*+ specialAnimationPositionOffset*/;

            xrOrigin.position += new Vector3(0, cameraFloorOffset + crouchOffset - playerController.sinkingValue * 2.5f, 0);
            xrOrigin.eulerAngles = new Vector3(0, rotationOffset, 0);
            xrOrigin.localScale = Vector3.one * 1.5f;

            //Logger.LogDebug($"{transform.position} {xrOrigin.position} {leftHandVRTarget.position} {rightHandVRTarget.position} {cameraFloorOffset} {cameraPosAccounted}");

            // Arms need to be moved forward when crouched
            if (crouchState != CrouchState.None)
                xrOrigin.position += transform.forward * 0.55f;
        }

        private void LateUpdate()
        {
            var positionOffset = new Vector3(0, crouchState switch
            {
                CrouchState.Roomscale => 0.1f,
                _ => 0,
            }, 0);

            // Apply controller transforms
            leftHandTarget.position = leftHandVRTarget.position + positionOffset;
            leftHandTarget.rotation = leftHandVRTarget.rotation;

            rightHandTarget.position = rightHandVRTarget.position + positionOffset;
            rightHandTarget.rotation = rightHandVRTarget.rotation;

            // Update tracked finger curls after animator update
            leftFingerCurler?.Update();

            if (!playerController.isHoldingObject)
            {
                rightFingerCurler?.Update();
            }
        }

        public void UpdateTargetTransforms(DNet.Rig rig)
        {
            leftController.localPosition = rig.leftHandPosition;
            leftController.localEulerAngles = rig.leftHandEulers;
            leftFingerCurler?.SetCurls(rig.leftHandFingers);

            rightController.localPosition = rig.rightHandPosition;
            rightController.localEulerAngles = rig.rightHandEulers;
            rightFingerCurler?.SetCurls(rig.rightHandFingers);

            camera.transform.eulerAngles = rig.cameraEulers;
            cameraPosAccounted = rig.cameraPosAccounted;

            crouchState = rig.crouchState;
            rotationOffset = rig.rotationOffset;
            cameraFloorOffset = rig.cameraFloorOffset;
        }
    }
}
