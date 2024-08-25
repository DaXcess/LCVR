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
using Unity.XR.CoreUtils;
using CrouchState = LCVR.Networking.Rig.CrouchState;
using Rig = LCVR.Networking.Rig;

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

    private Vector3 lastFrameHmdPosition = new(0, 0, 0);
    private Vector3 lastFrameHmdRotation = new(0, 0, 0);

    private Vector3 totalMovementSinceLastMove = Vector3.zero;

    private VRController mainController;

    private VRInteractor leftHandInteractor;
    private VRInteractor rightHandInteractor;

    public Transform leftItemHolder;
    public Transform rightItemHolder;

    private Transform mysteriousCube;

    private Channel prefsChannel;
    private Channel rigChannel;
    private Channel spectatorRigChannel;

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

    // IK Constraints
    public TwoBoneIKConstraint LocalLeftArmVRRig { get; private set; }
    public TwoBoneIKConstraint LocalRightArmVRRig { get; private set; }
    public TwoBoneIKConstraint LeftArmVRRig { get; private set; }
    public TwoBoneIKConstraint RightArmVRRig { get; private set; }
    
    public Transform LeftHandVRTarget { get; private set; }
    public Transform RightHandVRTarget { get; private set; }
    
    public bool IsRoomCrouching { get; private set; }

    #endregion

    private void Awake()
    {
        Logger.LogDebug("Going to initialize XR Rig");
        
        PlayerController = GetComponent<PlayerControllerB>();
        characterController = GetComponent<CharacterController>();
        Bones = new Bones(transform);

        mysteriousCube = transform.Find("Misc/Cube");

        // Prevent walking through walls
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Create XR stuff
        xrOrigin = new GameObject("XR Origin").transform;
        mainCamera = VRSession.Instance.MainCamera;

        // Fool the animator (this removes console error spam)
        new GameObject("MainCamera").transform.parent = transform.Find("ScavengerModel/metarig/CameraContainer");

        // Un-parent camera container
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

        // Head definitely does need to have offsets (in this case an offset of 0, 0, 0)
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

        var networkSystem = NetworkSystem.Instance;
        
        prefsChannel = networkSystem.CreateChannel(ChannelType.PlayerPrefs, PlayerController.playerClientId);
        rigChannel = networkSystem.CreateChannel(ChannelType.Rig, PlayerController.playerClientId);
        spectatorRigChannel = networkSystem.CreateChannel(ChannelType.SpectatorRig, PlayerController.playerClientId);
        
        StartCoroutine(UpdatePlayerPrefs());
    }

    private void BuildVRRig()
    {
        // Reset player character briefly to allow the RigBuilder to behave properly
        Bones.ResetToPrefabPositions();

        // ARMS ONLY RIG
        
        UpdateRigOffsets();

        RigTrackerLocal ??= Bones.LocalMetarig.gameObject.AddComponent<RigTracker>();

        // Setting up the head
        RigTrackerLocal.head = mainCamera.transform;
        
        // Setting up the left arm

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.LocalLeftArmRig.GetComponent<ChainIKConstraint>().weight = 0;
        
        // Create new hints and targets for the VR rig
        var localLeftArmRigTarget = new GameObject("VR Left Arm Rig Target")
        {
            transform =
            {
                parent = Bones.LocalLeftArmRig,
                localPosition = Bones.LocalLeftArmRigTarget.localPosition,
                localRotation = Bones.LocalLeftArmRigTarget.localRotation,
                localScale = Bones.LocalLeftArmRigTarget.localScale
            }
        }.transform;
        
        var localLeftArmRigHint = new GameObject("VR Left Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.LocalLeftArmRig,
                localPosition = Bones.LocalLeftArmRigHint.localPosition,
                localRotation = Bones.LocalLeftArmRigHint.localRotation,
                localScale = Bones.LocalLeftArmRigHint.localScale
            }
        }.transform;
        
        LocalLeftArmVRRig = Bones.LocalLeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        LocalLeftArmVRRig.data.root = Bones.LocalLeftUpperArm;
        LocalLeftArmVRRig.data.mid = Bones.LocalLeftLowerArm;
        LocalLeftArmVRRig.data.tip = Bones.LocalLeftHand;
        LocalLeftArmVRRig.data.target = localLeftArmRigTarget;
        LocalLeftArmVRRig.data.hint = localLeftArmRigHint;
        LocalLeftArmVRRig.data.hintWeight = 1;
        LocalLeftArmVRRig.data.targetRotationWeight = 1;
        LocalLeftArmVRRig.data.targetPositionWeight = 1;
        LocalLeftArmVRRig.weight = 1;

        RigTrackerLocal.leftHand = new RigTracker.Tracker
        {
            dstTransform = localLeftArmRigTarget,
            srcTransform = LeftHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };
        
        // Setting up the right arm

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.LocalRightArmRig.GetComponent<ChainIKConstraint>().weight = 0;
        
        // Create new hints and targets for the VR rig
        var localRightArmRigTarget = new GameObject("VR Right Arm Rig Target")
        {
            transform =
            {
                parent = Bones.LocalRightArmRig,
                localPosition = Bones.LocalRightArmRigTarget.localPosition,
                localRotation = Bones.LocalRightArmRigTarget.localRotation,
                localScale = Bones.LocalRightArmRigTarget.localScale
            }
        }.transform;
        
        var localRightArmRigHint = new GameObject("VR Right Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.LocalRightArmRig,
                localPosition = Bones.LocalRightArmRigHint.localPosition,
                localRotation = Bones.LocalRightArmRigHint.localRotation,
                localScale = Bones.LocalRightArmRigHint.localScale
            }
        }.transform;
        
        LocalRightArmVRRig = Bones.LocalRightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        LocalRightArmVRRig.data.root = Bones.LocalRightUpperArm;
        LocalRightArmVRRig.data.mid = Bones.LocalRightLowerArm;
        LocalRightArmVRRig.data.tip = Bones.LocalRightHand;
        LocalRightArmVRRig.data.target = localRightArmRigTarget;
        LocalRightArmVRRig.data.hint = localRightArmRigHint;
        LocalRightArmVRRig.data.hintWeight = 1;
        LocalRightArmVRRig.data.targetRotationWeight = 1;
        LocalRightArmVRRig.data.targetPositionWeight = 1;
        LocalRightArmVRRig.weight = 1;

        RigTrackerLocal.rightHand = new RigTracker.Tracker
        {
            dstTransform = localRightArmRigTarget,
            srcTransform = RightHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        // This one is pretty hit or miss, sometimes y needs to be -0.2f, other times it needs to be -2.25f
        RigTrackerLocal.headBodyPositionOffset = new Vector3(0, -.2f, 0);

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

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.LeftArmRig.GetComponent<ChainIKConstraint>().weight = 0;
        
        // Create new hints and targets for the VR rig
        var leftArmRigTarget = new GameObject("VR Left Arm Rig Target")
        {
            transform =
            {
                parent = Bones.LeftArmRigTarget.parent,
                localPosition = Bones.LeftArmRigTarget.localPosition,
                localRotation = Bones.LeftArmRigTarget.localRotation,
                localScale = Bones.LeftArmRigTarget.localScale
            }
        }.transform;
        
        var leftArmRigHint = new GameObject("VR Left Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.LeftArmRigTarget.parent,
                localPosition = Bones.LeftArmRigHint.localPosition,
                localRotation = Bones.LeftArmRigHint.localRotation,
                localScale = Bones.LeftArmRigHint.localScale
            }
        }.transform;
        
        LeftArmVRRig = Bones.LeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        LeftArmVRRig.data.root = Bones.LeftUpperArm;
        LeftArmVRRig.data.mid = Bones.LeftLowerArm;
        LeftArmVRRig.data.tip = Bones.LeftHand;
        LeftArmVRRig.data.target = leftArmRigTarget;
        LeftArmVRRig.data.hint = leftArmRigHint;
        LeftArmVRRig.data.hintWeight = 1;
        LeftArmVRRig.data.targetRotationWeight = 1;
        LeftArmVRRig.data.targetPositionWeight = 1;

        RigTracker.leftHand = new RigTracker.Tracker
        {
            dstTransform = leftArmRigTarget,
            srcTransform = LeftHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        // Setting up the right arm
        
        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.RightArmRig.GetComponent<ChainIKConstraint>().weight = 0;
        
        // Create new hints and targets for the VR rig
        var rightArmRigTarget = new GameObject("VR Right Arm Rig Target")
        {
            transform =
            {
                parent = Bones.RightArmRigTarget.parent,
                localPosition = Bones.RightArmRigTarget.localPosition,
                localRotation = Bones.RightArmRigTarget.localRotation,
                localScale = Bones.RightArmRigTarget.localScale
            }
        }.transform;
        
        var rightArmRigHint = new GameObject("VR Right Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.RightArmRigTarget.parent,
                localPosition = Bones.RightArmRigHint.localPosition,
                localRotation = Bones.RightArmRigHint.localRotation,
                localScale = Bones.RightArmRigHint.localScale
            }
        }.transform;
        
        RightArmVRRig = Bones.RightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        RightArmVRRig.data.root = Bones.RightUpperArm;
        RightArmVRRig.data.mid = Bones.RightLowerArm;
        RightArmVRRig.data.tip = Bones.RightHand;
        RightArmVRRig.data.target = rightArmRigTarget;
        RightArmVRRig.data.hint = rightArmRigHint;
        RightArmVRRig.data.hintWeight = 1;
        RightArmVRRig.data.targetRotationWeight = 1;
        RightArmVRRig.data.targetPositionWeight = 1;

        RigTracker.rightHand = new RigTracker.Tracker
        {
            dstTransform = rightArmRigTarget,
            srcTransform = RightHandVRTarget,
            positionOffset = Vector3.zero,
            rotationOffset = Vector3.zero
        };

        GetComponentInChildren<RigBuilder>().Build();
    }

    private void UpdateRigOffsets()
    {
        Bones.LocalLeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);
        Bones.LocalRightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);
        Bones.LeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);
        Bones.RightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);
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
        
        var movement = mainCamera.transform.localPosition - lastFrameHmdPosition;
        movement.y = 0;

        // Make sure player is facing towards the interacted object and that they're not sprinting
        if (!wasInSpecialAnimation && PlayerController.inSpecialInteractAnimation &&
            PlayerController.currentTriggerInAnimationWith is not null &&
            PlayerController.currentTriggerInAnimationWith.playerPositionNode)
        {
            var nodeRotation = Quaternion.Inverse(transform.parent.rotation) *
                               PlayerController.currentTriggerInAnimationWith.playerPositionNode.rotation;
            var offset = nodeRotation.eulerAngles.y -
                         mainCamera.transform.localEulerAngles.y;

            TurningProvider.SetOffset(offset);
            specialAnimationPositionOffset = Quaternion.Euler(0, offset, 0) *
                                             (new Vector3(mainCamera.transform.localPosition.x, 0,
                                                 mainCamera.transform.localPosition.z) * -SCALE_FACTOR)
                                             .Divide(transform.parent.localScale);
            
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
            .Any(c => !c.isTrigger && c.transform != mysteriousCube);

        // Move player if we're not in special interact animation
        if (!PlayerController.inSpecialInteractAnimation &&
            (totalMovementSinceLastMove.sqrMagnitude > 0.25f || hit || moved))
        {
            var wasGrounded = characterController.isGrounded;

            characterController.Move(transform.parent.localRotation * Vector3.Scale(
                new Vector3(totalMovementSinceLastMove.x, 0f, totalMovementSinceLastMove.z) * SCALE_FACTOR,
                transform.parent.localScale));
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

        if (roomCrouch != IsRoomCrouching && !PlayerController.inSpecialInteractAnimation)
        {
            PlayerController.Crouch(roomCrouch);
            IsRoomCrouching = roomCrouch;
        }

        // Apply crouch offset (don't offset if roomscale)
        crouchOffset = Mathf.Lerp(crouchOffset, !IsRoomCrouching && PlayerController.isCrouching ? -1 : 0, 0.2f);
        
        // Apply car animation offset
        var carOffset = PlayerController.inVehicleAnimation ? -0.5f : 0f;

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
            lastFrameHmdPosition = mainCamera.transform.localPosition;

        // Set sprint
        if (Plugin.Config.ToggleSprint.Value)
        {
            if (PlayerController.isExhausted)
                isSprinting = false;

            var move = PlayerController.isCrouching ? Vector2.zero : Actions.Instance["Move"].ReadValue<Vector2>();
            if (move is { x: 0, y: 0 } && stopSprintingCoroutine == null && isSprinting)
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
            rigChannel.SendPacket(Serialization.Serialize(new Rig()
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
                specialAnimationPositionOffset = specialAnimationPositionOffset,

                crouchState = (PlayerController.isCrouching, IsRoomCrouching) switch
                {
                    (true, true) => CrouchState.Roomscale,
                    (true, false) => CrouchState.Button,
                    (false, _) => CrouchState.None
                },
                rotationOffset = rotationOffset.eulerAngles.y,
                cameraFloorOffset = cameraFloorOffset,
            }));
        else
        {
            var parent = PlayerController.physicsParent ?? (PlayerController.isInElevator
                ? PlayerController.playersManager.elevatorTransform
                : PlayerController.playersManager.playersContainer);
            
            spectatorRigChannel.SendPacket(Serialization.Serialize(
                new SpectatorRig
                {
                    headPosition = parent.InverseTransformPoint(mainCamera.transform.position),
                    headRotation = mainCamera.transform.eulerAngles,

                    leftHandPosition = parent.InverseTransformPoint(leftController.transform.position),
                    leftHandRotation = leftController.transform.eulerAngles,

                    rightHandPosition = parent.InverseTransformPoint(rightController.transform.position),
                    rightHandRotation = rightController.transform.eulerAngles,

                    parentedToShip = PlayerController.isInElevator,
                }));
        }
    }

    private void LateUpdate()
    {
        UpdateRigOffsets();
        
        var angles = mainCamera.transform.eulerAngles;
        var deltaAngles = new Vector3(
            Mathf.DeltaAngle(lastFrameHmdRotation.x, angles.x),
            Mathf.DeltaAngle(lastFrameHmdRotation.y, angles.y),
            Mathf.DeltaAngle(lastFrameHmdRotation.z, angles.z)
        );

        StartOfRound.Instance.playerLookMagnitudeThisFrame = deltaAngles.magnitude * Time.deltaTime * 0.1f;

        lastFrameHmdRotation = angles;

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
        prefsChannel.Dispose();
        
        Actions.Instance["Sprint"].performed -= Sprint_performed;
        Actions.Instance["Reset Height"].performed -= ResetHeight_performed;
    }

    public void UpdateIKWeights()
    {
        // Constants
        PlayerController.cameraLookRig1.weight = 0.45f;
        PlayerController.cameraLookRig2.weight = 1;
        PlayerController.leftArmRigSecondary.weight = 0;
        PlayerController.rightArmRigSecondary.weight = 0;
        PlayerController.playerBodyAnimator?.SetLayerWeight(
            PlayerController.playerBodyAnimator.GetLayerIndex("UpperBodyEmotes"), 0);
        
        // Vanilla Rigs
        PlayerController.leftArmRig.weight = 0;
        PlayerController.rightArmRig.weight = 0;
        
        // VR Rigs
        LocalLeftArmVRRig.weight = 1;
        LocalRightArmVRRig.weight = 1;
        LeftArmVRRig.weight = 1;
        RightArmVRRig.weight = 1;
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
        const float targetHeight = 2.3f;
        
        yield return new WaitForSeconds(0.2f);

        realHeight = mainCamera.transform.localPosition.y * SCALE_FACTOR;
        cameraFloorOffset = targetHeight - realHeight;

        if (PlayerController.inSpecialInteractAnimation)
        {
            var nodeRotation = Quaternion.Inverse(transform.parent.rotation) *
                               PlayerController.currentTriggerInAnimationWith.playerPositionNode.rotation;
            var offset = nodeRotation.eulerAngles.y -
                         mainCamera.transform.localEulerAngles.y;

            TurningProvider.SetOffset(offset);
            specialAnimationPositionOffset = Quaternion.Euler(0, offset, 0) *
                                             (new Vector3(mainCamera.transform.localPosition.x, 0,
                                                 mainCamera.transform.localPosition.z) * -SCALE_FACTOR)
                                             .Divide(transform.parent.localScale);
        }

        resetHeightCoroutine = null;
    }

    private IEnumerator StopSprinting()
    {
        yield return new WaitForSeconds(Plugin.Config.MovementSprintToggleCooldown.Value);

        isSprinting = false;
        stopSprintingCoroutine = null;
    }

    private IEnumerator UpdatePlayerPrefs()
    {
        while (true)
        {
            // More concise layout if more prefs need to be synced
            prefsChannel.SendPacket([Plugin.Config.DisableCarSteeringWheelInteraction.Value ? (byte)1 : (byte)0]);

            yield return new WaitForSeconds(5f);
        }
    }

    private void TurnBodyToCamera(float turnWeight)
    {
        var newRotation = Quaternion.Euler(transform.eulerAngles.x, mainCamera.transform.eulerAngles.y, transform.eulerAngles.z);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * turnWeight);
    }

    private float GetBodyToCameraAngle()
    {
        return Quaternion.Angle(Quaternion.Euler(0, transform.eulerAngles.y, 0), Quaternion.Euler(0, mainCamera.transform.eulerAngles.y, 0));
    }
}

// Fuck you, execution order
[DefaultExecutionOrder(-200)]
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
        if (head is not null)
            transform.position = head.position + headBodyPositionOffset;

        leftHand.Apply();
        rightHand.Apply();
    }
}