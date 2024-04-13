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
using CrouchState = LCVR.Networking.DNet.Rig.CrouchState;

namespace LCVR.Player;

public class VRPlayer : MonoBehaviour
{
    private const float SCALE_FACTOR = 1.5f;
    private const int CAMERA_CLIP_MASK = 1 << 8 | 1 << 26;
    
    private const float SQR_MOVE_THRESHOLD = 1E-5f;
    private const float TURN_ANGLE_THRESHOLD = 120.0f;
    private const float TURN_WEIGHT_SHARP = 15.0f;

    private PlayerControllerB playerController;
    private CharacterController characterController;
    private Bones bones;

    private Coroutine stopSprintingCoroutine;
    private Coroutine resetHeightCoroutine;

    private float cameraFloorOffset;
    private float crouchOffset;
    private float realHeight = 2.3f;

    private bool isSprinting;
    private bool isRoomCrouching;

    private bool wasInSpecialAnimation;
    private bool wasInEnemyAnimation;
    private Vector3 specialAnimationPositionOffset = Vector3.zero;

    private Camera mainCamera;

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

    public VRFingerCurler LeftFingerCurler { get; private set; }
    public VRFingerCurler RightFingerCurler { get; private set; }

    private GameObject leftHandVRTarget;
    private GameObject rightHandVRTarget;

    public Transform leftItemHolder;
    public Transform rightItemHolder;

    #region Public Accessors
    public PlayerControllerB PlayerController => playerController;
    public Bones Bones => bones;
    public TurningProvider TurningProvider { get; private set; }

    public VRController PrimaryController => mainController;
    public VRInteractor LeftHandInteractor => leftHandInteractor;
    public VRInteractor RightHandInteractor => rightHandInteractor;

    public bool IsDead => playerController.isPlayerDead;
    public bool IsRoomCrouching => isRoomCrouching;
    #endregion

    private void Awake()
    {
        Logger.LogDebug("Going to intialize XR Rig");
        
        playerController = GetComponent<PlayerControllerB>();
        characterController = GetComponent<CharacterController>();
        bones = new Bones(transform);

        // Prevent walking through walls
        GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Create XR stuff
        xrOrigin = new GameObject("XR Origin").transform;
        mainCamera = VRSession.Instance.MainCamera;

        // Fool the animator (this removes console error spam)
        new GameObject("MainCamera").transform.parent = Find("ScavengerModel/metarig/CameraContainer").transform;

        // Unparent camera container
        mainCamera.transform.parent = xrOrigin;
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localRotation = Quaternion.Euler(0, 0, 0);
        xrOrigin.localScale = Vector3.one;

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
        rightHandVRTarget = new GameObject("Right Hand VR Target");
        leftHandVRTarget = new GameObject("Left Hand VR Target");

        headVRTarget.transform.parent = mainCamera.transform;
        rightHandVRTarget.transform.parent = rightController.transform;
        leftHandVRTarget.transform.parent = leftController.transform;

        // Head defintely does need to have offsets (in this case an offset of 0, 0, 0)
        headVRTarget.transform.localPosition = Vector3.zero;

        rightHandVRTarget.transform.localPosition = new Vector3(0.0279f, 0.0353f, -0.0044f);
        rightHandVRTarget.transform.localRotation = Quaternion.Euler(0, 90, 168);

        leftHandVRTarget.transform.localPosition = new Vector3(-0.0279f, 0.0353f, 0.0044f);
        leftHandVRTarget.transform.localRotation = Quaternion.Euler(0, 270, 192);

        // Add controller interactors
        mainController = rightController.AddComponent<VRController>();
        leftHandInteractor = bones.LocalLeftHand.gameObject.AddComponent<VRInteractor>();
        rightHandInteractor = bones.LocalRightHand.gameObject.AddComponent<VRInteractor>();

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
        Actions.Instance.OnReload += OnReloadActions;
        Actions.Instance["Controls/Reset Height"].performed += ResetHeight_performed;
        Actions.Instance["Controls/Sprint"].performed += Sprint_performed;
        
        ResetHeight();

        // Set up item holders
        var leftHolder = new GameObject("Left Hand Item Holder");
        var rightHolder = new GameObject("Right Hand Item Holder");

        leftItemHolder = leftHolder.transform;
        leftItemHolder.SetParent(bones.LocalLeftHand, false);
        leftItemHolder.localPosition = new Vector3(0.018f, 0.045f, -0.042f);
        leftItemHolder.localEulerAngles = new Vector3(360f - 356.3837f, 357.6979f, 0.1453f);

        rightItemHolder = rightHolder.transform;
        rightItemHolder.SetParent(bones.LocalRightHand, false);
        rightItemHolder.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
        rightItemHolder.localEulerAngles = new Vector3(356.3837f, 357.6979f, 0.1453f);

        // Set up finger curlers
        LeftFingerCurler = new VRFingerCurler(bones.LocalLeftHand, true);
        RightFingerCurler = new VRFingerCurler(bones.LocalRightHand, false);

        BuildVRRig();

        Logger.LogDebug("Initialized XR Rig");
    }

    private void BuildVRRig()
    {
        // Reset player character briefly to allow the RigBuilder to behave properly
        bones.ResetToPrefabPositions();

        // ARMS ONLY RIG

        // Set up rigging
        var model = Find("ScavengerModel/metarig/ScavengerModelArmsOnly", true).gameObject;

        // Why are these even nonzero in the first place?
        bones.LocalMetarig.localPosition = Vector3.zero;
        bones.LocalArmsRig.localPosition = Vector3.zero;

        var rigFollow = model.GetComponent<IKRigFollowVRRig>() ?? model.AddComponent<IKRigFollowVRRig>();

        // Setting up the head
        rigFollow.head = mainCamera.transform;

        // Setting up the left arm

        bones.LocalLeftArmRig.localPosition = Vector3.zero;
        bones.LocalLeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(bones.LocalLeftArmRig.GetComponent<ChainIKConstraint>());
        var localLeftArmConstraint = bones.LocalLeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        localLeftArmConstraint.data.root = bones.LocalLeftUpperArm;
        localLeftArmConstraint.data.mid = bones.LocalLeftLowerArm;
        localLeftArmConstraint.data.tip = bones.LocalLeftHand;
        localLeftArmConstraint.data.target = bones.LocalLeftArmRigTarget;
        localLeftArmConstraint.data.hint = bones.LocalLeftArmRigHint;
        localLeftArmConstraint.data.hintWeight = 1;
        localLeftArmConstraint.data.targetRotationWeight = 1;
        localLeftArmConstraint.data.targetPositionWeight = 1;

        rigFollow.leftHand = new IKRigFollowVRRig.VRMap()
        {
            ikTarget = bones.LocalLeftArmRigTarget,
            vrTarget = leftHandVRTarget.transform,
            trackingPositionOffset = Vector3.zero,
            trackingRotationOffset = Vector3.zero
        };

        // Setting up the right arm

        bones.LocalRightArmRig.localPosition = Vector3.zero;
        bones.LocalRightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(bones.LocalRightArmRig.GetComponent<ChainIKConstraint>());
        var localRightArmConstraint = bones.LocalRightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        localRightArmConstraint.data.root = bones.LocalRightUpperArm;
        localRightArmConstraint.data.mid = bones.LocalRightLowerArm;
        localRightArmConstraint.data.tip = bones.LocalRightHand;
        localRightArmConstraint.data.target = bones.LocalRightArmRigTarget;
        localRightArmConstraint.data.hint = bones.LocalRightArmRigHint;
        localRightArmConstraint.data.hintWeight = 1;
        localRightArmConstraint.data.targetRotationWeight = 1;
        localRightArmConstraint.data.targetPositionWeight = 1;

        rigFollow.rightHand = new IKRigFollowVRRig.VRMap()
        {
            ikTarget = bones.LocalRightArmRigTarget,
            vrTarget = rightHandVRTarget.transform,
            trackingPositionOffset = Vector3.zero,
            trackingRotationOffset = Vector3.zero
        };

        // This one is pretty hit or miss, sometimes y needs to be -0.2f, other times it needs to be -2.25f
        rigFollow.headBodyPositionOffset = new Vector3(0, -0.2f, 0);

        // Disable badges
        bones.Spine.Find("LevelSticker").gameObject.SetActive(false);
        bones.Spine.Find("BetaBadge").gameObject.SetActive(false);

        // FULL BODY RIG

        // Set up rigging
        var fullModel = Find("ScavengerModel", true).gameObject;

        bones.Metarig.localPosition = Vector3.zero;

        var fullRigFollow = fullModel.GetComponent<IKRigFollowVRRig>() ?? fullModel.AddComponent<IKRigFollowVRRig>();

        // Setting up the left arm

        bones.LeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(bones.LeftArmRig.GetComponent<ChainIKConstraint>());
        var leftArmConstraint = bones.LeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        leftArmConstraint.data.root = bones.LeftUpperArm;
        leftArmConstraint.data.mid = bones.LeftLowerArm;
        leftArmConstraint.data.tip = bones.LeftHand;
        leftArmConstraint.data.target = bones.LeftArmRigTarget;
        leftArmConstraint.data.hint = bones.LeftArmRigHint;
        leftArmConstraint.data.hintWeight = 1;
        leftArmConstraint.data.targetRotationWeight = 1;
        leftArmConstraint.data.targetPositionWeight = 1;

        fullRigFollow.leftHand = new IKRigFollowVRRig.VRMap()
        {
            ikTarget = bones.LeftArmRigTarget,
            vrTarget = leftHandVRTarget.transform,
            trackingPositionOffset = Vector3.zero,
            trackingRotationOffset = Vector3.zero
        };

        // Setting up the right arm

        bones.RightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Destroy(bones.RightArmRig.GetComponent<ChainIKConstraint>());
        var rightArmConstraint = bones.RightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();

        rightArmConstraint.data.root = bones.RightUpperArm;
        rightArmConstraint.data.mid = bones.RightLowerArm;
        rightArmConstraint.data.tip = bones.RightHand;
        rightArmConstraint.data.target = bones.RightArmRigTarget;
        rightArmConstraint.data.hint = bones.RightArmRigHint;
        rightArmConstraint.data.hintWeight = 1;
        rightArmConstraint.data.targetRotationWeight = 1;
        rightArmConstraint.data.targetPositionWeight = 1;

        fullRigFollow.rightHand = new IKRigFollowVRRig.VRMap()
        {
            ikTarget = bones.RightArmRigTarget,
            vrTarget = rightHandVRTarget.transform,
            trackingPositionOffset = Vector3.zero,
            trackingRotationOffset = Vector3.zero
        };

        fullRigFollow.headBodyPositionOffset = new Vector3(0, 0, 0);

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

    private void OnReloadActions(InputActionAsset oldActions, InputActionAsset newActions)
    {
        oldActions["Controls/Reset Height"].performed -= ResetHeight_performed;
        oldActions["Controls/Sprint"].performed -= Sprint_performed;
        
        newActions["Controls/Reset Height"].performed += ResetHeight_performed;
        newActions["Controls/Sprint"].performed += Sprint_performed;
    }
    
    private void Update()
    {
        var movement = mainCamera.transform.localPosition - lastFrameHMDPosition;
        movement.y = 0;

        // Make sure player is facing towards the interacted object and that they're not sprinting
        if (!wasInSpecialAnimation && playerController.inSpecialInteractAnimation && playerController.currentTriggerInAnimationWith is not null && playerController.currentTriggerInAnimationWith.playerPositionNode)
        {
            TurningProvider.SetOffset(playerController.currentTriggerInAnimationWith.playerPositionNode.eulerAngles.y - mainCamera.transform.localEulerAngles.y);
            isSprinting = false;
        }

        if (!wasInEnemyAnimation && playerController.inAnimationWithEnemy)
        {
            var direction = playerController.inAnimationWithEnemy.transform.position - transform.position;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);

            TurningProvider.SetOffset(rotation.eulerAngles.y - mainCamera.transform.localEulerAngles.y);
        }

        var rotationOffset = playerController.jetpackControls switch
        {
            true => Quaternion.Euler(playerController.jetpackTurnCompass.eulerAngles.x, TurningProvider.GetRotationOffset(), playerController.jetpackTurnCompass.eulerAngles.z),
            false => Quaternion.Euler(0, TurningProvider.GetRotationOffset(), 0)
        };

        var movementAccounted = rotationOffset * movement;
        var cameraPosAccounted = rotationOffset * new Vector3(mainCamera.transform.localPosition.x, 0, mainCamera.transform.localPosition.z);

        if (!wasInSpecialAnimation && playerController.inSpecialInteractAnimation)
            specialAnimationPositionOffset = new Vector3(-cameraPosAccounted.x * SCALE_FACTOR, 0, -cameraPosAccounted.z * SCALE_FACTOR);

        wasInSpecialAnimation = playerController.inSpecialInteractAnimation;
        wasInEnemyAnimation = playerController.inAnimationWithEnemy is not null;

        if (playerController.inSpecialInteractAnimation)
            totalMovementSinceLastMove = Vector3.zero;
        else
            totalMovementSinceLastMove += movementAccounted;

        var controllerMovement = Actions.Instance["Movement/Move"].ReadValue<Vector2>();
        var moved = controllerMovement.x > 0 || controllerMovement.y > 0;
        var hit = UnityEngine.Physics
            .OverlapBox(mainCamera.transform.position, Vector3.one * 0.1f, Quaternion.identity, CAMERA_CLIP_MASK)
            .Any(c => !c.isTrigger && c.transform != transform.Find("Misc/Cube"));

        // Move player if we're not in special interact animation
        if (!playerController.inSpecialInteractAnimation && (totalMovementSinceLastMove.sqrMagnitude > 0.25f || hit || moved))
        {
            var wasGrounded = characterController.isGrounded;
            
            characterController.Move(new Vector3(totalMovementSinceLastMove.x * SCALE_FACTOR, 0f, totalMovementSinceLastMove.z * SCALE_FACTOR));
            totalMovementSinceLastMove = Vector3.zero;

            if (!characterController.isGrounded && wasGrounded)
                characterController.Move(new Vector3(0, -0.01f, 0));
        }

        // Update rotation offset after adding movement from frame (if not in build mode)
        if (!ShipBuildModeManager.Instance.InBuildMode && !playerController.inSpecialInteractAnimation)
            TurningProvider.Update();

        var lastOriginPos = xrOrigin.position;

        // If we are in special animation allow 6 DOF but don't update player position
        if (!playerController.inSpecialInteractAnimation)
            xrOrigin.position = new Vector3(
                transform.position.x + (totalMovementSinceLastMove.x * SCALE_FACTOR) - (cameraPosAccounted.x * SCALE_FACTOR),
                transform.position.y,
                transform.position.z + (totalMovementSinceLastMove.z * SCALE_FACTOR) - (cameraPosAccounted.z * SCALE_FACTOR)
            );
        else
            xrOrigin.position = transform.position + specialAnimationPositionOffset;

        // Move player model
        var point = transform.InverseTransformPoint(mainCamera.transform.position);
        bones.Model.localPosition = new Vector3(point.x, 0, point.z);

        // Check for roomscale crouching
        float realCrouch = mainCamera.transform.localPosition.y / realHeight;
        bool roomCrouch = realCrouch < 0.5f;

        if (roomCrouch != isRoomCrouching)
        {
            playerController.Crouch(roomCrouch);
            isRoomCrouching = roomCrouch;
        }

        // Apply crouch offset (don't offset if roomscale)
        crouchOffset = Mathf.Lerp(crouchOffset, !isRoomCrouching && playerController.isCrouching ? -1 : 0, 0.2f);

        // Apply floor offset and sinking value
        xrOrigin.position += new Vector3(0, cameraFloorOffset + crouchOffset - playerController.sinkingValue * 2.5f, 0);
        xrOrigin.rotation = rotationOffset;
        xrOrigin.localScale = Vector3.one * SCALE_FACTOR;

        //Logger.LogDebug($"{transform.position} {xrOrigin.position} {leftHandVRTarget.transform.position} {rightHandVRTarget.transform.position} {cameraFloorOffset} {cameraPosAccounted}");

        if ((xrOrigin.position - lastOriginPos).sqrMagnitude > SQR_MOVE_THRESHOLD) // player moved
                                                                                 // Rotate body sharply but still smoothly
            TurnBodyToCamera(TURN_WEIGHT_SHARP);
        else if (!playerController.inSpecialInteractAnimation && GetBodyToCameraAngle() is var angle && angle > TURN_ANGLE_THRESHOLD)
            // Rotate body as smoothly as possible but prevent 360 deg head twists on quick rotations
            TurnBodyToCamera(TURN_WEIGHT_SHARP * Mathf.InverseLerp(TURN_ANGLE_THRESHOLD, 170f, angle));

        if (!playerController.inSpecialInteractAnimation)
            lastFrameHMDPosition = mainCamera.transform.localPosition;

        // Set sprint
        if (Plugin.Config.ToggleSprint.Value)
        {
            if (playerController.isExhausted)
                isSprinting = false;

            var move = playerController.isCrouching ? Vector2.zero : Actions.Instance["Movement/Move"].ReadValue<Vector2>();
            if (move.x == 0 && move.y == 0 && stopSprintingCoroutine == null && isSprinting)
                stopSprintingCoroutine = StartCoroutine(StopSprinting());
            else if ((move.x != 0 || move.y != 0) && stopSprintingCoroutine != null)
            {
                StopCoroutine(stopSprintingCoroutine);
                stopSprintingCoroutine = null;
            }


            PlayerControllerB_Sprint_Patch.sprint = !isRoomCrouching && !playerController.isCrouching && isSprinting ? 1 : 0;
        }
        else
            PlayerControllerB_Sprint_Patch.sprint = !isRoomCrouching && Actions.Instance["Controls/Sprint"].IsPressed() ? 1 : 0;
        
        if (!playerController.isPlayerDead)
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

                crouchState = (playerController.isCrouching, isRoomCrouching) switch
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
            var targetTransform = playerController.isInElevator
                ? playerController.playersManager.elevatorTransform
                : playerController.playersManager.playersContainer;

            DNet.BroadcastSpectatorRig(new DNet.SpectatorRig()
            {
                headPosition = targetTransform.InverseTransformPoint(mainCamera.transform.position),
                headRotation = mainCamera.transform.eulerAngles,

                leftHandPosition = targetTransform.InverseTransformPoint(leftController.transform.position),
                leftHandRotation = leftController.transform.eulerAngles,

                rightHandPosition = targetTransform.InverseTransformPoint(rightController.transform.position),
                rightHandRotation = rightController.transform.eulerAngles,

                parentedToShip = playerController.isInElevator
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

        if (!playerController.isHoldingObject)
        {
            RightFingerCurler?.Update();
        }

        var height = cameraFloorOffset + mainCamera.transform.localPosition.y;
        if (height is > 3f or < 0f)
            ResetHeight();
    }
    
    private void OnDestroy()
    {
        Actions.Instance.OnReload -= OnReloadActions;
        Actions.Instance["Controls/Sprint"].performed -= Sprint_performed;
        Actions.Instance["Controls/Reset Height"].performed -= ResetHeight_performed;
    }
    
    public void EnableInteractorVisuals(bool enabled = true)
    {
        leftControllerRayInteractor.GetComponent<XRInteractorLineVisual>().enabled = enabled;
        rightControllerRayInteractor.GetComponent<XRInteractorLineVisual>().enabled = enabled;
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

    private Transform Find(string name, bool resetLocalPosition = false)
    {
        var transform = this.transform.Find(name);
        if (transform == null) return null;

        if (resetLocalPosition)
            transform.localPosition = Vector3.zero;

        return transform;
    }
}

internal class IKRigFollowVRRig : MonoBehaviour
{
    [Serializable]
    public class VRMap
    {
        public Transform vrTarget;
        public Transform ikTarget;
        public Vector3 trackingPositionOffset;
        public Vector3 trackingRotationOffset;

        public void Map()
        {
            ikTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
            ikTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
        }
    }

    public Transform head;
    public VRMap leftHand;
    public VRMap rightHand;

    public Vector3 headBodyPositionOffset;

    private void LateUpdate()
    {
        if (head != null)
        {
            transform.position = head.position + headBodyPositionOffset;
        }

        leftHand.Map();
        rightHand.Map();
    }
}
