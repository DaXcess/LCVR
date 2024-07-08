using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System;
using LCVR.Input;
using System.Collections;
using LCVR.Networking;
using GameNetcodeStuff;
using UnityEngine.XR.Interaction.Toolkit;
using LCVR.Patches;
using UnityEngine.Animations.Rigging;
using System.Linq;
using UnityEngine.Serialization;
using CrouchState = LCVR.Networking.DNet.Rig.CrouchState;

namespace LCVR.Player;

public class VRPlayer : MonoBehaviour
{
    private const float SCALE_FACTOR = 1.5f;
    private const int CAMERA_CLIP_MASK = 1 << 8 | 1 << 26;
    
    private const float SQR_MOVE_THRESHOLD = 1E-5f;
    private const float TURN_ANGLE_THRESHOLD = 120.0f;
    private const float TURN_WEIGHT_SHARP = 15.0f;

    private Coroutine stopSprintingCoroutine;
    private Coroutine resetHeightCoroutine;

    private float cameraFloorOffset;
    private float crouchOffset;
    private float realHeight = 2.3f;

    private bool isSprinting;

    private bool wasInSpecialAnimation;
    private bool wasInEnemyAnimation;
    private Vector3 specialAnimationPositionOffset = Vector3.zero;

    private Camera mainCamera;
    private CharacterController characterController;
    
    private GameObject leftController;
    private GameObject rightController;

    private XRRayInteractor leftControllerRayInteractor;
    private XRRayInteractor rightControllerRayInteractor;

    private Transform xrOrigin;

    private Vector3 lastFrameHMDPosition = new(0, 0, 0);
    private Vector3 lastFrameHMDRotation = new(0, 0, 0);

    private Vector3 totalMovementSinceLastMove = Vector3.zero;

    private VRController mainController;

    private VRInteractor leftHandInteractor;
    private VRInteractor rightHandInteractor;

    public Transform leftItemHolder;
    public Transform rightItemHolder;

    #region Public Accessors
    public PlayerControllerB PlayerController { get; private set; }
    public Bones Bones { get; private set; }
    public TurningProvider TurningProvider { get; private set; }
    public RigTracker RigTracker { get; private set; }
    public RigTracker RigTrackerLocal { get; private set; }

    public VRController PrimaryController => mainController;
    public VRInteractor LeftHandInteractor => leftHandInteractor;
    public VRInteractor RightHandInteractor => rightHandInteractor;
    public VRFingerCurler LeftFingerCurler { get; private set; }
    public VRFingerCurler RightFingerCurler { get; private set; }
    
    public Transform LeftHandVRTarget { get; private set; }
    public Transform RightHandVRTarget { get; private set; }
    
    public bool IsRoomCrouching { get; private set; }

    #endregion

    private void Awake()
    {
        Logger.LogDebug("Going to intialize XR Rig");
        
        PlayerController = GetComponent<PlayerControllerB>();
        characterController = GetComponent<CharacterController>();
        Bones = new Bones(transform);

        // Prevent walking through walls
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Create XR stuff
        xrOrigin = new GameObject("XR Origin").transform;
        mainCamera = VRSession.Instance.MainCamera;

        // Fool the animator (this removes console error spam)
        new GameObject("MainCamera").transform.parent = transform.Find("ScavengerModel/metarig/CameraContainer");

        // Unparent camera container
        mainCamera.transform.parent = xrOrigin;
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        xrOrigin.localScale = Vector3.one * SCALE_FACTOR;

        // Create controller objects
        rightController = new GameObject("Right Controller");
        leftController = new GameObject("Left Controller");

        // And mount to camera container
        rightController.transform.parent = xrOrigin;
        leftController.transform.parent = xrOrigin;

        // Left hand tracking
        var rightHandPoseDriver = rightController.AddComponent<TrackedPoseDriver>();
        rightHandPoseDriver.positionAction = Actions.Instance.RightHandPosition;
        rightHandPoseDriver.rotationAction = Actions.Instance.RightHandRotation;
        rightHandPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.RightHandTrackingState);

        // Right hand tracking
        var leftHandPoseDriver = leftController.AddComponent<TrackedPoseDriver>();
        leftHandPoseDriver.positionAction = Actions.Instance.LeftHandPosition;
        leftHandPoseDriver.rotationAction = Actions.Instance.LeftHandRotation;
        leftHandPoseDriver.trackingStateInput = new InputActionProperty(Actions.Instance.LeftHandTrackingState);

        // Set up IK Rig VR targets
        var headVRTarget = new GameObject("Head VR Target");
        RightHandVRTarget = new GameObject("Right Hand VR Target").transform;
        LeftHandVRTarget = new GameObject("Left Hand VR Target").transform;

        headVRTarget.transform.parent = mainCamera.transform;
        RightHandVRTarget.parent = rightController.transform;
        LeftHandVRTarget.parent = leftController.transform;

        // Head defintely does need to have offsets (in this case an offset of 0, 0, 0)
        headVRTarget.transform.localPosition = Vector3.zero;

        RightHandVRTarget.localPosition = new Vector3(0.0279f, 0.0353f, -0.0044f);
        RightHandVRTarget.localRotation = Quaternion.Euler(0, 90, 168);

        LeftHandVRTarget.localPosition = new Vector3(-0.0279f, 0.0353f, 0.0044f);
        LeftHandVRTarget.localRotation = Quaternion.Euler(0, 270, 192);

        // Add controller interactors
        mainController = rightController.AddComponent<VRController>();
        leftHandInteractor = Bones.LocalLeftHand.gameObject.AddComponent<VRInteractor>();
        rightHandInteractor = Bones.LocalRightHand.gameObject.AddComponent<VRInteractor>();

        // Add ray interactors for VR keyboard
        leftControllerRayInteractor = new GameObject("Left Ray Interactor").CreateInteractorController(Utils.Hand.Left, false, false);
        rightControllerRayInteractor = new GameObject("Right Ray Interactor").CreateInteractorController(Utils.Hand.Right, false, false);

        leftControllerRayInteractor.transform.SetParent(leftController.transform, false);
        rightControllerRayInteractor.transform.SetParent(rightController.transform, false);

        leftControllerRayInteractor.transform.localPosition = new Vector3(0.01f, 0, 0);
        leftControllerRayInteractor.transform.localRotation = Quaternion.Euler(80, 0, 0);

        rightControllerRayInteractor.transform.localPosition = new Vector3(-0.01f, 0, 0);
        rightControllerRayInteractor.transform.localRotation = Quaternion.Euler(80, 0, 0);

        // Add turning provider
        TurningProvider = Plugin.Config.TurnProvider.Value switch
        {
            Config.TurnProviderOption.Snap => new SnapTurningProvider(),
            Config.TurnProviderOption.Smooth => new SmoothTurningProvider(),
            Config.TurnProviderOption.Disabled => new NullTurningProvider(),
            _ => throw new ArgumentException("Unknown turn provider configuration option provided")
        };

        // Input actions
        Actions.Instance["Reset Height"].performed += ResetHeight_performed;
        Actions.Instance["Sprint"].performed += Sprint_performed;
        
        ResetHeight();

        // Set up item holders
        var leftHolder = new GameObject("Left Hand Item Holder");
        var rightHolder = new GameObject("Right Hand Item Holder");

        leftItemHolder = leftHolder.transform;
        leftItemHolder.SetParent(Bones.LocalLeftHand, false);
        leftItemHolder.localPosition = new Vector3(0.018f, 0.045f, -0.042f);
        leftItemHolder.localEulerAngles = new Vector3(360f - 356.3837f, 357.6979f, 0.1453f);

        rightItemHolder = rightHolder.transform;
        rightItemHolder.SetParent(Bones.LocalRightHand, false);
        rightItemHolder.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
        rightItemHolder.localEulerAngles = new Vector3(356.3837f, 357.6979f, 0.1453f);

        // Set up finger curlers
        LeftFingerCurler = new VRFingerCurler(Bones.LocalLeftHand, true);
        RightFingerCurler = new VRFingerCurler(Bones.LocalRightHand, false);

        BuildVRRig();

        Logger.LogDebug("Initialized XR Rig");
    }

    private void BuildVRRig()
    {
        // Reset player character briefly to allow the RigBuilder to behave properly
        Bones.ResetToPrefabPositions();

        // ARMS ONLY RIG

        // Set up rigging
        var model = transform.Find("ScavengerModel/metarig/ScavengerModelArmsOnly").gameObject;
        model.transform.localPosition = Vector3.zero;
        
        // Why are these even nonzero in the first place?
        Bones.LocalMetarig.localPosition = Vector3.zero;
        Bones.LocalArmsRig.localPosition = Vector3.zero;

        RigTrackerLocal ??= model.AddComponent<RigTracker>();

        // Setting up the head
        RigTrackerLocal.head = mainCamera.transform;

        // Setting up the left arm

        Bones.LocalLeftArmRig.localPosition = Vector3.zero;
        Bones.LocalLeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(Bones.LocalLeftArmRig.GetComponent<ChainIKConstraint>());
        var localLeftArmConstraint = Bones.LocalLeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        localLeftArmConstraint.data.root = Bones.LocalLeftUpperArm;
        localLeftArmConstraint.data.mid = Bones.LocalLeftLowerArm;
        localLeftArmConstraint.data.tip = Bones.LocalLeftHand;
        localLeftArmConstraint.data.target = Bones.LocalLeftArmRigTarget;
        localLeftArmConstraint.data.hint = Bones.LocalLeftArmRigHint;
        localLeftArmConstraint.data.hintWeight = 1;
        localLeftArmConstraint.data.targetRotationWeight = 1;
        localLeftArmConstraint.data.targetPositionWeight = 1;

        RigTrackerLocal.leftHand = new RigTracker.Tracker()
        {
            dstTransform = Bones.LocalLeftArmRigTarget,
            srcTransform = LeftHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        // Setting up the right arm

        Bones.LocalRightArmRig.localPosition = Vector3.zero;
        Bones.LocalRightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(Bones.LocalRightArmRig.GetComponent<ChainIKConstraint>());
        var localRightArmConstraint = Bones.LocalRightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        localRightArmConstraint.data.root = Bones.LocalRightUpperArm;
        localRightArmConstraint.data.mid = Bones.LocalRightLowerArm;
        localRightArmConstraint.data.tip = Bones.LocalRightHand;
        localRightArmConstraint.data.target = Bones.LocalRightArmRigTarget;
        localRightArmConstraint.data.hint = Bones.LocalRightArmRigHint;
        localRightArmConstraint.data.hintWeight = 1;
        localRightArmConstraint.data.targetRotationWeight = 1;
        localRightArmConstraint.data.targetPositionWeight = 1;

        RigTrackerLocal.rightHand = new RigTracker.Tracker()
        {
            dstTransform = Bones.LocalRightArmRigTarget,
            srcTransform = RightHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        // This one is pretty hit or miss, sometimes y needs to be -0.2f, other times it needs to be -2.25f
        RigTrackerLocal.headBodyPositionOffset = new Vector3(0, -0.2f, 0);

        // Disable badges
        Bones.Spine.Find("LevelSticker").gameObject.SetActive(false);
        Bones.Spine.Find("BetaBadge").gameObject.SetActive(false);

        // FULL BODY RIG

        // Set up rigging
        var fullModel = transform.Find("ScavengerModel").gameObject;
        fullModel.transform.localPosition = Vector3.zero;

        Bones.Metarig.localPosition = Vector3.zero;

        RigTracker ??= fullModel.AddComponent<RigTracker>();

        // Setting up the left arm

        Bones.LeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(Bones.LeftArmRig.GetComponent<ChainIKConstraint>());
        var leftArmConstraint = Bones.LeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        leftArmConstraint.data.root = Bones.LeftUpperArm;
        leftArmConstraint.data.mid = Bones.LeftLowerArm;
        leftArmConstraint.data.tip = Bones.LeftHand;
        leftArmConstraint.data.target = Bones.LeftArmRigTarget;
        leftArmConstraint.data.hint = Bones.LeftArmRigHint;
        leftArmConstraint.data.hintWeight = 1;
        leftArmConstraint.data.targetRotationWeight = 1;
        leftArmConstraint.data.targetPositionWeight = 1;

        RigTracker.leftHand = new RigTracker.Tracker()
        {
            dstTransform = Bones.LeftArmRigTarget,
            srcTransform = LeftHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        // Setting up the right arm

        Bones.RightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(Bones.RightArmRig.GetComponent<ChainIKConstraint>());
        var rightArmConstraint = Bones.RightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        rightArmConstraint.data.root = Bones.RightUpperArm;
        rightArmConstraint.data.mid = Bones.RightLowerArm;
        rightArmConstraint.data.tip = Bones.RightHand;
        rightArmConstraint.data.target = Bones.RightArmRigTarget;
        rightArmConstraint.data.hint = Bones.RightArmRigHint;
        rightArmConstraint.data.hintWeight = 1;
        rightArmConstraint.data.targetRotationWeight = 1;
        rightArmConstraint.data.targetPositionWeight = 1;

        RigTracker.rightHand = new RigTracker.Tracker()
        {
            dstTransform = Bones.RightArmRigTarget,
            srcTransform = RightHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        GetComponentInChildren<RigBuilder>().Build();
    }

    private void Sprint_performed(InputAction.CallbackContext obj)
    {
        if (!obj.performed)
            return;

        isSprinting = !isSprinting;
    }

    private void ResetHeight_performed(InputAction.CallbackContext obj)
    {
        if (obj.performed)
            ResetHeight();
    }
    
    private void Update()
    {
        // Make sure the XR Origin has the same parent as the player
        if (xrOrigin.parent != transform.parent)
        {
            xrOrigin.parent = transform.parent;
            TurningProvider.SetOffset(xrOrigin.transform.localEulerAngles.y);
        }
        
        var movement = mainCamera.transform.localPosition - lastFrameHMDPosition;
        movement.y = 0;

        // Make sure player is facing towards the interacted object and that they're not sprinting
        if (!wasInSpecialAnimation && PlayerController.inSpecialInteractAnimation &&
            PlayerController.currentTriggerInAnimationWith is not null &&
            PlayerController.currentTriggerInAnimationWith.playerPositionNode)
        {
            TurningProvider.SetOffset(PlayerController.currentTriggerInAnimationWith.playerPositionNode.eulerAngles.y -
                                      mainCamera.transform.localEulerAngles.y);
            isSprinting = false;
        }

        if (!wasInEnemyAnimation && PlayerController.inAnimationWithEnemy)
        {
            var direction = PlayerController.inAnimationWithEnemy.transform.position - transform.position;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            TurningProvider.SetOffset(rotation.eulerAngles.y - mainCamera.transform.localEulerAngles.y);
        }

        var rotationOffset = PlayerController.jetpackControls switch
        {
            true => Quaternion.Euler(PlayerController.jetpackTurnCompass.eulerAngles.x,
                TurningProvider.GetRotationOffset(), PlayerController.jetpackTurnCompass.eulerAngles.z),
            false => Quaternion.Euler(0, TurningProvider.GetRotationOffset(), 0)
        };

        var movementAccounted = rotationOffset * movement;
        var cameraPosAccounted = rotationOffset * new Vector3(mainCamera.transform.localPosition.x, 0, mainCamera.transform.localPosition.z);

        if (!wasInSpecialAnimation && PlayerController.inSpecialInteractAnimation)
            specialAnimationPositionOffset = new Vector3(-cameraPosAccounted.x * SCALE_FACTOR, 0, -cameraPosAccounted.z * SCALE_FACTOR);

        wasInSpecialAnimation = PlayerController.inSpecialInteractAnimation;
        wasInEnemyAnimation = PlayerController.inAnimationWithEnemy is not null;

        if (PlayerController.inSpecialInteractAnimation)
            totalMovementSinceLastMove = Vector3.zero;
        else
            totalMovementSinceLastMove += movementAccounted;

        var controllerMovement = Actions.Instance["Move"].ReadValue<Vector2>();
        var moved = controllerMovement.x > 0 || controllerMovement.y > 0;
        var hit = UnityEngine.Physics
            .OverlapBox(mainCamera.transform.position, Vector3.one * 0.1f, Quaternion.identity, CAMERA_CLIP_MASK)
            .Any(c => !c.isTrigger && c.transform != transform.Find("Misc/Cube"));

        // Move player if we're not in special interact animation
        if (!PlayerController.inSpecialInteractAnimation && (totalMovementSinceLastMove.sqrMagnitude > 0.25f || hit || moved))
        {
            var wasGrounded = characterController.isGrounded;

            characterController.Move(transform.parent.localRotation * new Vector3(
                totalMovementSinceLastMove.x * SCALE_FACTOR, 0f, totalMovementSinceLastMove.z * SCALE_FACTOR));
            totalMovementSinceLastMove = Vector3.zero;

            if (!characterController.isGrounded && wasGrounded)
                characterController.Move(new Vector3(0, -0.01f, 0));
        }

        // Update rotation offset after adding movement from frame (if not in build mode)
        if (!ShipBuildModeManager.Instance.InBuildMode && !PlayerController.inSpecialInteractAnimation)
            TurningProvider.Update();

        var lastOriginPos = xrOrigin.localPosition;

        // If we are in special animation allow 6 DOF but don't update player position
        if (!PlayerController.inSpecialInteractAnimation)
            xrOrigin.localPosition = new Vector3(
                transform.localPosition.x + (totalMovementSinceLastMove.x * SCALE_FACTOR) - (cameraPosAccounted.x * SCALE_FACTOR),
                transform.localPosition.y,
                transform.localPosition.z + (totalMovementSinceLastMove.z * SCALE_FACTOR) - (cameraPosAccounted.z * SCALE_FACTOR)
            );
        else
            xrOrigin.localPosition = transform.localPosition + specialAnimationPositionOffset;

        // Move player model
        var point = transform.InverseTransformPoint(mainCamera.transform.position);
        Bones.Model.localPosition = new Vector3(point.x, 0, point.z);

        // Check for roomscale crouching
        var realCrouch = mainCamera.transform.localPosition.y / realHeight;
        var roomCrouch = realCrouch < 0.5f;

        if (roomCrouch != IsRoomCrouching)
        {
            PlayerController.Crouch(roomCrouch);
            IsRoomCrouching = roomCrouch;
        }

        // Apply crouch offset (don't offset if roomscale)
        crouchOffset = Mathf.Lerp(crouchOffset, !IsRoomCrouching && PlayerController.isCrouching ? -1 : 0, 0.2f);
        
        // Apply car animation offset
        var carOffset = PlayerController.inVehicleAnimation ? -1f : 0f;

        // Apply height and rotation offsets
        xrOrigin.localPosition += new Vector3(0,
            cameraFloorOffset + crouchOffset - PlayerController.sinkingValue * 2.5f + carOffset, 0);
        xrOrigin.localRotation = rotationOffset;

        if ((xrOrigin.localPosition - lastOriginPos).sqrMagnitude > SQR_MOVE_THRESHOLD) // player moved
                                                                                 // Rotate body sharply but still smoothly
            TurnBodyToCamera(TURN_WEIGHT_SHARP);
        else if (!PlayerController.inSpecialInteractAnimation && GetBodyToCameraAngle() is var angle and > TURN_ANGLE_THRESHOLD)
            // Rotate body as smoothly as possible but prevent 360 deg head twists on quick rotations
            TurnBodyToCamera(TURN_WEIGHT_SHARP * Mathf.InverseLerp(TURN_ANGLE_THRESHOLD, 170f, angle));

        if (!PlayerController.inSpecialInteractAnimation)
            lastFrameHMDPosition = mainCamera.transform.localPosition;

        // Set sprint
        if (Plugin.Config.ToggleSprint.Value)
        {
            if (PlayerController.isExhausted)
                isSprinting = false;

            var move = PlayerController.isCrouching ? Vector2.zero : Actions.Instance["Move"].ReadValue<Vector2>();
            if (move.x == 0 && move.y == 0 && stopSprintingCoroutine == null && isSprinting)
                stopSprintingCoroutine = StartCoroutine(StopSprinting());
            else if ((move.x != 0 || move.y != 0) && stopSprintingCoroutine != null)
            {
                StopCoroutine(stopSprintingCoroutine);
                stopSprintingCoroutine = null;
            }


            PlayerControllerB_Sprint_Patch.sprint = !IsRoomCrouching && !PlayerController.isCrouching && isSprinting ? 1 : 0;
        }
        else
            PlayerControllerB_Sprint_Patch.sprint = !IsRoomCrouching && Actions.Instance["Sprint"].IsPressed() ? 1 : 0;
        
        if (!PlayerController.isPlayerDead)
            DNet.BroadcastRig(new DNet.Rig()
            {
                leftHandPosition = leftController.transform.localPosition,
                leftHandEulers = leftController.transform.localEulerAngles,
                leftHandFingers = LeftFingerCurler.GetCurls(),

                rightHandPosition = rightController.transform.localPosition,
                rightHandEulers = rightController.transform.localEulerAngles,
                rightHandFingers = RightFingerCurler.GetCurls(),

                cameraEulers = mainCamera.transform.eulerAngles,
                cameraPosAccounted = cameraPosAccounted,
                modelOffset = totalMovementSinceLastMove,

                crouchState = (PlayerController.isCrouching, IsRoomCrouching) switch
                {
                    (true, true) => CrouchState.Roomscale,
                    (true, false) => CrouchState.Button,
                    (false, _) => CrouchState.None
                },
                rotationOffset = rotationOffset.eulerAngles.y,
                cameraFloorOffset = cameraFloorOffset,
            });
        else
        {
            var targetTransform = PlayerController.isInElevator
                ? PlayerController.playersManager.elevatorTransform
                : PlayerController.playersManager.playersContainer;

            DNet.BroadcastSpectatorRig(new DNet.SpectatorRig()
            {
                headPosition = targetTransform.InverseTransformPoint(mainCamera.transform.position),
                headRotation = mainCamera.transform.eulerAngles,

                leftHandPosition = targetTransform.InverseTransformPoint(leftController.transform.position),
                leftHandRotation = leftController.transform.eulerAngles,

                rightHandPosition = targetTransform.InverseTransformPoint(rightController.transform.position),
                rightHandRotation = rightController.transform.eulerAngles,

                parentedToShip = PlayerController.isInElevator
            });
        }
    }

    private void LateUpdate()
    {
        var angles = mainCamera.transform.eulerAngles;
        var deltaAngles = new Vector3(
            Mathf.DeltaAngle(lastFrameHMDRotation.x, angles.x),
            Mathf.DeltaAngle(lastFrameHMDRotation.y, angles.y),
            Mathf.DeltaAngle(lastFrameHMDRotation.z, angles.z)
        );

        StartOfRound.Instance.playerLookMagnitudeThisFrame = deltaAngles.magnitude * Time.deltaTime * 0.1f;

        lastFrameHMDRotation = angles;

        // Update tracked finger curls after animator update
        LeftFingerCurler?.Update();

        if (!PlayerController.isHoldingObject)
        {
            RightFingerCurler?.Update();
        }

        var height = cameraFloorOffset + mainCamera.transform.localPosition.y;
        if (height is > 3f or < 0f)
            ResetHeight();
    }
    
    private void OnDestroy()
    {
        Actions.Instance["Sprint"].performed -= Sprint_performed;
        Actions.Instance["Reset Height"].performed -= ResetHeight_performed;
    }
    
    public void EnableInteractorVisuals(bool visible = true)
    {
        leftControllerRayInteractor.GetComponent<XRInteractorLineVisual>().enabled = visible;
        rightControllerRayInteractor.GetComponent<XRInteractorLineVisual>().enabled = visible;
    }

    public void ResetHeight()
    {
        if (resetHeightCoroutine is not null)
            return;
        
        resetHeightCoroutine = StartCoroutine(ResetHeightRoutine());
    }

    private IEnumerator ResetHeightRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        realHeight = mainCamera.transform.localPosition.y * SCALE_FACTOR;
        const float targetHeight = 2.3f;

        cameraFloorOffset = targetHeight - realHeight;

        resetHeightCoroutine = null;
    }

    private IEnumerator StopSprinting()
    {
        yield return new WaitForSeconds(Plugin.Config.MovementSprintToggleCooldown.Value);

        isSprinting = false;
        stopSprintingCoroutine = null;
    }

    private void TurnBodyToCamera(float turnWeight)
    {
        var newRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, mainCamera.transform.eulerAngles.y, transform.rotation.eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * turnWeight);
    }

    private float GetBodyToCameraAngle()
    {
        return Quaternion.Angle(Quaternion.Euler(0, transform.eulerAngles.y, 0), Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0));
    }
}

public class RigTracker : MonoBehaviour
{
    public class Tracker
    {
        public Transform srcTransform;
        public Transform dstTransform;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        public void Apply()
        {
            dstTransform.position = srcTransform.TransformPoint(positionOffset);
            dstTransform.rotation = srcTransform.rotation * Quaternion.Euler(rotationOffset);
        }
    }

    public Transform head;
    public Tracker leftHand;
    public Tracker rightHand;

    public Vector3 headBodyPositionOffset;

    private void LateUpdate()
    {
        if (head != null)
        {
            transform.position = head.position + headBodyPositionOffset;
        }

        leftHand.Apply();
        rightHand.Apply();
    }
}
