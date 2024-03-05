using GameNetcodeStuff;
using LCVR.API;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Player;
using TMPro;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using CrouchState = LCVR.Networking.DNet.Rig.CrouchState;

namespace LCVR.Networking;

/// <summary>
/// A behaviour that is attached to other VR players
/// </summary>
public class VRNetPlayer : MonoBehaviour
{
    private ChainIKConstraintData originalLeftArmConstraintData;
    private ChainIKConstraintData originalRightArmConstraintData;

    private GameObject playerGhost;
    private Transform usernameBillboard;
    private CanvasGroup usernameAlpha;
    private TextMeshProUGUI usernameText;

    private Transform xrOrigin;
    private Transform leftController;
    private Transform rightController;
    private Transform leftHandVRTarget;
    private Transform rightHandVRTarget;

    private FingerCurler leftFingerCurler;
    private FingerCurler rightFingerCurler;

    private Transform camera;

    private float cameraFloorOffset;
    private float rotationOffset;

    private Vector3 cameraPosAccounted;
    private Vector3 modelOffset;

    private CrouchState crouchState = CrouchState.None;
    private float crouchOffset;

    public PlayerControllerB PlayerController { get; private set; }
    public Bones Bones { get; private set; }
    
    public Transform LeftItemHolder { get; private set; }
    public Transform RightItemHolder { get; private set; }

    private void Awake()
    {
        PlayerController = GetComponent<PlayerControllerB>();
        Bones = new Bones(transform);

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

        camera = transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera");

        // Set up item holders
        var leftHolder = new GameObject("Left Hand Item Holder");
        var rightHolder = new GameObject("Right Hand Item Holder");

        LeftItemHolder = leftHolder.transform;
        LeftItemHolder.SetParent(Bones.LeftHand, false);
        LeftItemHolder.localPosition = new Vector3(0.018f, 0.045f, -0.042f);
        LeftItemHolder.localEulerAngles = new Vector3(360f - 356.3837f, 357.6979f, 0.1453f);

        RightItemHolder = rightHolder.transform;
        RightItemHolder.SetParent(Bones.RightHand, false);
        RightItemHolder.localPosition = new Vector3(-0.002f, 0.036f, -0.042f);
        RightItemHolder.localEulerAngles = new Vector3(356.3837f, 357.6979f, 0.1453f);

        // Set up finger curlers
        leftFingerCurler = new FingerCurler(Bones.LeftHand, true);
        rightFingerCurler = new FingerCurler(Bones.RightHand, false);

        BuildVRRig();

        // Create spectating player
        playerGhost = Instantiate(AssetManager.spectatorGhost, VRSession.Instance.transform);
        playerGhost.name = $"Spectating Player: {PlayerController.playerUsername}";

        usernameBillboard = playerGhost.GetComponentInChildren<Canvas>().transform;
        usernameText = playerGhost.GetComponentInChildren<TextMeshProUGUI>();
        usernameAlpha = playerGhost.GetComponentInChildren<CanvasGroup>();

        playerGhost.GetComponentInChildren<SpectatorGhost>().player = this;
        
        // Disable rendering ghost until player dies
        foreach (var renderer in playerGhost.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = false;
        }
        
        // Set username text
        if (PlayerController.playerSteamId is 76561198438308784 or 76561199575858981)
        {
            usernameText.color = new Color(0, 1, 1, 1);
            usernameText.fontStyle = FontStyles.Bold;
        }
        
        usernameText.text = $"<noparse>{PlayerController.playerUsername}</noparse>";
    }

    private void BuildVRRig()
    {
        // Reset player character briefly to allow the RigBuilder to behave properly
        Bones.ResetToPrefabPositions();

        // Setting up the left arm

        Bones.LeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        var originalLeftArmConstraint = Bones.LeftArmRig.GetComponent<ChainIKConstraint>();
        originalLeftArmConstraintData = originalLeftArmConstraint.data;
        Destroy(originalLeftArmConstraint);

        var leftArmConstraint = Bones.LeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();
        leftArmConstraint.data.root = Bones.LeftUpperArm;
        leftArmConstraint.data.mid = Bones.LeftLowerArm;
        leftArmConstraint.data.tip = Bones.LeftHand;
        leftArmConstraint.data.target = Bones.LeftArmRigTarget;
        leftArmConstraint.data.hint = Bones.LeftArmRigHint;
        leftArmConstraint.data.hintWeight = 1;
        leftArmConstraint.data.targetRotationWeight = 1;
        leftArmConstraint.data.targetPositionWeight = 1;

        // Setting up the right arm

        Bones.RightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        var originalRightArmConstraint = Bones.RightArmRig.GetComponent<ChainIKConstraint>();
        originalRightArmConstraintData = originalRightArmConstraint.data;
        Destroy(originalRightArmConstraint);

        var rightArmConstraint = Bones.RightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();
        rightArmConstraint.data.root = Bones.RightUpperArm;
        rightArmConstraint.data.mid = Bones.RightLowerArm;
        rightArmConstraint.data.tip = Bones.RightHand;
        rightArmConstraint.data.target = Bones.RightArmRigTarget;
        rightArmConstraint.data.hint = Bones.RightArmRigHint;
        rightArmConstraint.data.hintWeight = 1;
        rightArmConstraint.data.targetRotationWeight = 1;
        rightArmConstraint.data.targetPositionWeight = 1;

        GetComponentInChildren<RigBuilder>().Build();
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
        if (!PlayerController.inSpecialInteractAnimation)
        {
            xrOrigin.position = new Vector3(
                transform.position.x + (modelOffset.x * 1.5f) - (cameraPosAccounted.x * 1.5f),
                transform.position.y,
                transform.position.z + (modelOffset.z * 1.5f) - (cameraPosAccounted.z * 1.5f)
            );

            Bones.Model.localPosition = transform.InverseTransformPoint(transform.position + modelOffset);
        }
        else
        {
            xrOrigin.position = transform.position;
            Bones.Model.localPosition = Vector3.zero;
        }

        xrOrigin.position += new Vector3(0, cameraFloorOffset + crouchOffset - PlayerController.sinkingValue * 2.5f, 0);
        xrOrigin.eulerAngles = new Vector3(0, rotationOffset, 0);
        xrOrigin.localScale = Vector3.one * 1.5f;

        // Arms need to be moved forward when crouched
        if (crouchState != CrouchState.None)
            xrOrigin.position += transform.forward * 0.55f;

        usernameAlpha.alpha -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        var positionOffset = new Vector3(0, crouchState switch
        {
            CrouchState.Roomscale => 0.1f,
            _ => 0,
        }, 0);

        // Apply controller transforms
        Bones.LeftArmRigTarget.position = leftHandVRTarget.position + positionOffset;
        Bones.LeftArmRigTarget.rotation = leftHandVRTarget.rotation;

        Bones.RightArmRigTarget.position = rightHandVRTarget.position + positionOffset;
        Bones.RightArmRigTarget.rotation = rightHandVRTarget.rotation;

        // Update tracked finger curls after animator update
        leftFingerCurler?.Update();

        if (!PlayerController.isHoldingObject)
        {
            rightFingerCurler?.Update();
        }
        
        // Rotate spectator username billboard
        if (StartOfRound.Instance.localPlayerController.localVisorTargetPoint is not null)
        {
            usernameBillboard.LookAt(StartOfRound.Instance.localPlayerController.localVisorTargetPoint);
        }
    }

    /// <summary>
    /// Show the spectator ghost
    /// </summary>
    public void ShowSpectatorGhost()
    {
        // Show player ghost when player dies
        foreach (var renderer in playerGhost.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = true;
        }
    }

    /// <summary>
    /// Hide the spectator ghost and username billboard
    /// </summary>
    public void HideSpectatorGhost()
    {
        foreach (var renderer in playerGhost.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.enabled = false;
        }
        
        usernameAlpha.alpha = 0f;
    }

    /// <summary>
    /// Show the username of the player (if they are dead)
    /// </summary>
    public void ShowSpectatorNameBillboard()
    {
        if (!PlayerController.isPlayerDead)
            return;

        usernameAlpha.alpha = 1f;
    }
    
    internal void UpdateTargetTransforms(DNet.Rig rig)
    {
        leftController.localPosition = rig.leftHandPosition;
        leftController.localEulerAngles = rig.leftHandEulers;
        leftFingerCurler?.SetCurls(rig.leftHandFingers);

        rightController.localPosition = rig.rightHandPosition;
        rightController.localEulerAngles = rig.rightHandEulers;
        rightFingerCurler?.SetCurls(rig.rightHandFingers);

        camera.transform.eulerAngles = rig.cameraEulers;
        cameraPosAccounted = rig.cameraPosAccounted;
        modelOffset = rig.modelOffset;

        crouchState = rig.crouchState;
        rotationOffset = rig.rotationOffset;
        cameraFloorOffset = rig.cameraFloorOffset;
    }

    /// <summary>
    /// Apply transforms for the spectator ghost
    /// </summary>
    internal void UpdateSpectatorTransforms(DNet.SpectatorRig rig)
    {
        var head = playerGhost.transform.Find("Head");
        var leftHand = playerGhost.transform.Find("Hand.L");
        var rightHand = playerGhost.transform.Find("Hand.R");
        
        head.position = rig.headPosition;
        head.eulerAngles = rig.headRotation;

        leftHand.position = rig.leftHandPosition;
        leftHand.eulerAngles = rig.leftHandRotation;

        rightHand.position = rig.rightHandPosition;
        rightHand.eulerAngles = rig.rightHandRotation;

        if (StartOfRound.Instance.localPlayerController.localVisorTargetPoint is not null)
        {
            usernameBillboard.LookAt(StartOfRound.Instance.localPlayerController.localVisorTargetPoint);
        }
    }

    /// <summary>
    /// Properly clean up the IK and spectator ghost if a VR player leaves the game
    /// </summary>
    void OnDestroy()
    {
        Destroy(playerGhost);
        
        Bones.ResetToPrefabPositions();

        Destroy(Bones.LeftArmRig.GetComponent<TwoBoneIKConstraint>());
        Destroy(Bones.RightArmRig.GetComponent<TwoBoneIKConstraint>());

        var leftArmConstraint = Bones.LeftArmRig.gameObject.AddComponent<ChainIKConstraint>();
        var rightArmConstraint = Bones.RightArmRig.gameObject.AddComponent<ChainIKConstraint>();

        leftArmConstraint.data = originalLeftArmConstraintData;
        rightArmConstraint.data = originalRightArmConstraintData;

        GetComponentInChildren<RigBuilder>().Build();
        
        APIManager.OnVRPlayerLeft(this);
    }
}
