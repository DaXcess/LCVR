using GameNetcodeStuff;
using LCVR.Assets;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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

        public Transform camera;

        private float cameraFloorOffset;
        private float rotationOffset;

        private Vector3 cameraPosAccounted;

        private bool isCrouching = false;
        private float crouchOffset;

        // Due to some black magic wizfuckery when rebuilding the RigBuilder it gets all kinds of fucked up even though the targets and hints are correct
        // We fix this by first resetting the contraint data, and then RESETTING?!??!? it.
        // For some fucking reason this "works" but very occasionally causes the shin to be rotated like 360 degress 
        private static TwoBoneIKConstraint leftLegConstraint;
        private static TwoBoneIKConstraint rightLegConstraint;

        private static TwoBoneIKConstraintData leftLegConstraintData;
        private static TwoBoneIKConstraintData rightLegConstraintData;

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

            // Store IK constraint data
            leftLegConstraint = gameObject.Find("ScavengerModel/metarig/Rig 1/LeftLeg").GetComponent<TwoBoneIKConstraint>();
            rightLegConstraint = gameObject.Find("ScavengerModel/metarig/Rig 1/RightLeg").GetComponent<TwoBoneIKConstraint>();

            leftLegConstraintData = leftLegConstraint.data;
            rightLegConstraintData = rightLegConstraint.data;

            StartCoroutine(RebuildRig());
        }

        private void Update()
        {
            // Apply crouch offset
            crouchOffset = Mathf.Lerp(crouchOffset, isCrouching ? -1 : 0, 0.2f);

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

            // Arms need to be moved forward when crouched
            if (isCrouching)
                xrOrigin.position += xrOrigin.forward * 0.55f;

            // Apply controller transforms
            leftHandTarget.position = leftHandVRTarget.position;
            leftHandTarget.rotation = leftHandVRTarget.rotation;

            rightHandTarget.position = rightHandVRTarget.position;
            rightHandTarget.rotation = rightHandVRTarget.rotation;
        }

        private IEnumerator RebuildRig()
        {
            RebuildingRig = true;

            var animator = GetComponentInChildren<Animator>();
            animator.runtimeAnimatorController = null;

            yield return null;

            leftLegConstraint.data = leftLegConstraintData;
            rightLegConstraint.data = rightLegConstraintData;

            var leftArmHint = gameObject.Find("ScavengerModel/metarig/Rig 1/LeftArm/LeftArm_hint");
            var rightArmHint = gameObject.Find("ScavengerModel/metarig/Rig 1/RightArm/RightArm_hint");

            var leftArmTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/LeftArm_target");
            var rightArmTarget = gameObject.Find("ScavengerModel/metarig/spine/spine.001/spine.002/spine.003/RightArm_target");
            var leftLegTarget = gameObject.Find("ScavengerModel/metarig/Rig 1/LeftLeg/LeftLeg_target");
            var rightLegTarget = gameObject.Find("ScavengerModel/metarig/Rig 1/RightLeg/RightLeg_target");

            leftArmHint.transform.localPosition = new Vector3(-0.7878151f, 1, -2.077282f);
            rightArmHint.transform.localPosition = new Vector3(2.57f, 1, -1.774f);

            leftArmTarget.transform.localPosition = new Vector3(-1.045884f, -0.05775639f, -0.04409964f);
            leftArmTarget.transform.localEulerAngles = new Vector3(-174.781f, 0, 77.251f);

            rightArmTarget.transform.localPosition = new Vector3(1.064115f, -0.06609607f, -0.0308033f);
            rightArmTarget.transform.localEulerAngles = new Vector3(-174.781f, 0, -78.548f);

            leftLegTarget.transform.localPosition = new Vector3(0.99f, 0.209f, 1.011f);
            leftLegTarget.transform.localEulerAngles = new Vector3(53.2761f, 180, 180);

            rightLegTarget.transform.localPosition = new Vector3(1.436f, 0.27f, 1.058f);
            rightLegTarget.transform.localEulerAngles = new Vector3(53.2761f, 180, 180);

            yield return null;

            GetComponentInChildren<RigBuilder>().Build();
            animator.runtimeAnimatorController = AssetManager.remoteVrMetarig;

            yield return null;

            // WHY DOES THIS WORK?!?? (IT IS NOT SUPPOSED TO!?!?!?!!)
            leftLegConstraint.Reset();
            rightLegConstraint.Reset();
            
            RebuildingRig = false;
        }

        public void UpdateTargetTransforms(DNet.Rig rig)
        {
            leftController.localPosition = rig.leftHandPosition;
            leftController.localEulerAngles = rig.leftHandEulers;

            rightController.localPosition = rig.rightHandPosition;
            rightController.localEulerAngles = rig.rightHandEulers;

            camera.transform.eulerAngles = rig.cameraEulers;
            cameraPosAccounted = rig.cameraPosAccounted;

            isCrouching = rig.isCrouching;
            rotationOffset = rig.rotationOffset;
        }

        public void UpdateCameraFloorOffset(float offset)
        {
            cameraFloorOffset = offset;
        }
    }
}
