using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameNetcodeStuff;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Player;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace LCVR.Networking;

/// <summary>
/// A behaviour that is attached to other VR players
/// </summary>
public class VRNetPlayer : MonoBehaviour
{
    private record struct HandTargetOverride(Transform Transform, Vector3 PositionOffset, Vector3 RotationOffset);
    
    private SpectatorGhost playerGhost;

    private Transform xrOrigin;
    private Transform leftController;
    private Transform rightController;
    private Transform leftHandVRTarget;
    private Transform rightHandVRTarget;
    private Transform leftArmRigTarget;
    private Transform rightArmRigTarget;

    private TwoBoneIKConstraint leftArmVRRig;
    private TwoBoneIKConstraint rightArmVRRig;

    private HandTargetOverride? leftHandTargetOverride;
    private HandTargetOverride? rightHandTargetOverride;

    private Transform camera;

    private float cameraFloorOffset;
    private Vector3 originRotation;

    private Vector3 cameraEulers;
    private Vector3 cameraPosAccounted;
    private Vector3 modelOffset;
    private Vector3 specialAnimationPositionOffset;
    
    private CrouchState crouchState = CrouchState.None;
    private float crouchOffset;

    private Channel prefsChannel;
    private Channel rigChannel;
    private Channel spectatorRigChannel;

    public PlayerControllerB PlayerController { get; private set; }
    public Bones Bones { get; private set; }

    public Transform LeftItemHolder { get; private set; }
    public Transform RightItemHolder { get; private set; }

    public FingerCurler LeftFingerCurler { get; private set; }
    public FingerCurler RightFingerCurler { get; private set; }

    public PlayerData AdditionalData { get; private set; }

    private readonly List<GameObject> cleanupPool = [];
    
    private void Awake()
    {
        PlayerController = GetComponent<PlayerControllerB>();
        Bones = new Bones(transform);
        AdditionalData = new PlayerData();

        // Because I want to transmit local controller positions and angles (since it's much cleaner)
        // I decided to somewhat recreate the XR Origin setup so that all the offsets are correct
        xrOrigin = new GameObject("XR Origin").transform;
        xrOrigin.localPosition = Vector3.zero;
        xrOrigin.localEulerAngles = Vector3.zero;
        xrOrigin.localScale = Vector3.one * VRPlayer.SCALE_FACTOR;

        // Create controller objects & VR targets
        leftController = new GameObject("Left Controller").transform;
        rightController = new GameObject("Right Controller").transform;
        leftHandVRTarget = new GameObject("Left Hand VR Target").transform;
        rightHandVRTarget = new GameObject("Right Hand VR Target").transform;
        leftArmRigTarget = new GameObject("Left Hand Rig Target").transform;
        rightArmRigTarget = new GameObject("Right Hand Rig Target").transform;

        leftController.SetParent(xrOrigin, false);
        rightController.SetParent(xrOrigin, false);

        leftHandVRTarget.SetParent(leftController, false);
        rightHandVRTarget.SetParent(rightController, false);
        
        leftArmRigTarget.SetParent(xrOrigin, false);
        rightArmRigTarget.SetParent(xrOrigin, false);

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
        
        cleanupPool.Add(xrOrigin.gameObject);
        cleanupPool.Add(LeftItemHolder.gameObject);
        cleanupPool.Add(RightItemHolder.gameObject);

        // Set up finger curlers
        LeftFingerCurler = new FingerCurler(Bones.LeftHand, true);
        RightFingerCurler = new FingerCurler(Bones.RightHand, false);

        BuildVRRig();

        // Create spectating player
        playerGhost = Instantiate(AssetManager.SpectatorGhost, VRSession.Instance.transform)
            .GetComponent<SpectatorGhost>();
        playerGhost.gameObject.name = $"Spectating Player: {PlayerController.playerUsername}";
        playerGhost.player = this;

        var networkSystem = NetworkSystem.Instance;
        
        prefsChannel = networkSystem.CreateChannel(ChannelType.PlayerPrefs, PlayerController.playerClientId);
        rigChannel = networkSystem.CreateChannel(ChannelType.Rig, PlayerController.playerClientId);
        spectatorRigChannel = networkSystem.CreateChannel(ChannelType.SpectatorRig, PlayerController.playerClientId);
        
        prefsChannel.OnPacketReceived += OnPrefsPacketReceived;
        rigChannel.OnPacketReceived += OnRigDataReceived;
        spectatorRigChannel.OnPacketReceived += OnSpectatorRigDataReceived;
        
        // Set up VR items
        foreach (var item in PlayerController.ItemSlots.Where(val => val != null))
        {
            // Add or enable VR item script on item if there is one for this item
            if (!Player.Items.items.TryGetValue(item.itemProperties.itemName, out var type))
                continue;

            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
        
        // Create a "prefix" script that runs before other game scripts
        gameObject.AddComponent<VRNetPlayerEarly>();
    }

    private void BuildVRRig()
    {
        // Reset player character briefly to allow the RigBuilder to behave properly
        Bones.ResetToPrefabPositions();

        // Setting up the left arm

        Bones.LeftArmRigHint.localPosition = new Vector3(-10f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.LeftArmRig.GetComponent<ChainIKConstraint>().weight = 0;

        // Create new hints and targets for the VR rig
        var leftArmRigHint = new GameObject("VR Left Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.LeftArmRigHint.parent,
                localPosition = Bones.LeftArmRigHint.localPosition,
                localRotation = Bones.LeftArmRigHint.localRotation,
                localScale = Bones.LeftArmRigHint.localScale
            }
        }.transform;
        
        cleanupPool.Add(leftArmRigHint.gameObject);
        
        leftArmVRRig = Bones.LeftArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();
        leftArmVRRig.data.root = Bones.LeftUpperArm;
        leftArmVRRig.data.mid = Bones.LeftLowerArm;
        leftArmVRRig.data.tip = Bones.LeftHand;
        leftArmVRRig.data.target = leftArmRigTarget;
        leftArmVRRig.data.hint = leftArmRigHint;
        leftArmVRRig.data.hintWeight = 1;
        leftArmVRRig.data.targetRotationWeight = 1;
        leftArmVRRig.data.targetPositionWeight = 1;
        leftArmVRRig.weight = 1;

        // Setting up the right arm

        Bones.RightArmRigHint.localPosition = new Vector3(12.5f, -2f, -1f);

        // Disable built-in constraints since they don't support hints (fucks up the elbows)
        Bones.RightArmRig.GetComponent<ChainIKConstraint>().weight = 0;
        
        // Create new hints and targets for the VR rig
        var rightArmRigHint = new GameObject("VR Right Arm Rig Hint")
        {
            transform =
            {
                parent = Bones.RightArmRigHint.parent,
                localPosition = Bones.RightArmRigHint.localPosition,
                localRotation = Bones.RightArmRigHint.localRotation,
                localScale = Bones.RightArmRigHint.localScale
            }
        }.transform;
        
        cleanupPool.Add(rightArmRigHint.gameObject);

        rightArmVRRig = Bones.RightArmRig.gameObject.AddComponent<TwoBoneIKConstraint>();
        rightArmVRRig.data.root = Bones.RightUpperArm;
        rightArmVRRig.data.mid = Bones.RightLowerArm;
        rightArmVRRig.data.tip = Bones.RightHand;
        rightArmVRRig.data.target = rightArmRigTarget;
        rightArmVRRig.data.hint = rightArmRigHint;
        rightArmVRRig.data.hintWeight = 1;
        rightArmVRRig.data.targetRotationWeight = 1;
        rightArmVRRig.data.targetPositionWeight = 1;
        rightArmVRRig.weight = 1;

        GetComponentInChildren<RigBuilder>().Build();
    }

    private void Update()
    {
        UpdateIKWeights();
        
        // Apply crouch offset
        crouchOffset = Mathf.Lerp(crouchOffset, crouchState switch
        {
            CrouchState.Button => -1,
            _ => 0,
        }, 0.2f);

        // Apply origin transforms
        if (xrOrigin.parent != transform.parent)
            xrOrigin.parent = transform.parent;
        
        // If we are in special animation allow 6 DOF but don't update player position
        if (!PlayerController.inSpecialInteractAnimation)
        {
            xrOrigin.localPosition = new Vector3(
                transform.localPosition.x + modelOffset.x * VRPlayer.SCALE_FACTOR -
                cameraPosAccounted.x * VRPlayer.SCALE_FACTOR * transform.localScale.x,
                transform.localPosition.y,
                transform.localPosition.z + modelOffset.z * VRPlayer.SCALE_FACTOR -
                cameraPosAccounted.z * VRPlayer.SCALE_FACTOR * transform.localScale.z
            );

            Bones.Model.localPosition = transform.InverseTransformPoint(transform.position + modelOffset) +
                                        Vector3.down * (2.5f * PlayerController.sinkingValue);
        }
        else
        {
            xrOrigin.localPosition = transform.localPosition + specialAnimationPositionOffset;
            Bones.Model.localPosition = Vector3.zero;
        }

        // Apply car animation offset
        var carOffset = PlayerController.inVehicleAnimation ? -0.5f : 0f;

        xrOrigin.localPosition += new Vector3(0,
                                      cameraFloorOffset + crouchOffset - PlayerController.sinkingValue * 2.5f +
                                      carOffset, 0) *
                                  transform.localScale.y;
        xrOrigin.localEulerAngles = originRotation;

        // Arms need to be moved forward when crouched
        if (crouchState != CrouchState.None)
            xrOrigin.localPosition += transform.forward * 0.2f;
        
        // Set camera (head) rotation
        camera.transform.eulerAngles = cameraEulers;
    }

    private void LateUpdate()
    {
        var positionOffset = new Vector3(0, crouchState switch
        {
            CrouchState.Roomscale => 0.1f,
            _ => 0,
        }, 0);

        // Apply controller transforms
        if (leftHandTargetOverride is {} leftOverride)
        {
            if (leftOverride.Transform == null)
            {
                Logger.LogWarning("Left hand override target transform despawned");
                leftHandTargetOverride = null;

                return;
            }

            // Break snap if distance is too great
            if (Vector3.Distance(leftOverride.Transform.position, leftHandVRTarget.position) > 2)
            {
                leftHandTargetOverride = null;

                return;
            }
            
            leftArmRigTarget.position = leftOverride.Transform.TransformPoint(leftOverride.PositionOffset);
            leftArmRigTarget.rotation =
                leftOverride.Transform.rotation * Quaternion.Euler(leftOverride.RotationOffset);
        }
        else
        {
            leftArmRigTarget.position = leftHandVRTarget.position + positionOffset;
            leftArmRigTarget.rotation = leftHandVRTarget.rotation;
        }

        if (rightHandTargetOverride is {} rightOverride)
        {
            if (rightOverride.Transform == null)
            {
                Logger.LogWarning("Right hand override target transform despawned");
                rightHandTargetOverride = null;

                return;
            }

            // Break snap if distance is too great
            if (Vector3.Distance(rightOverride.Transform.position, rightHandVRTarget.position) > 2)
            {
                rightHandTargetOverride = null;

                return;
            }
            
            rightArmRigTarget.position = rightOverride.Transform.TransformPoint(rightOverride.PositionOffset);
            rightArmRigTarget.rotation =
                rightOverride.Transform.rotation * Quaternion.Euler(rightOverride.RotationOffset);
        }
        else
        {
            rightArmRigTarget.position = rightHandVRTarget.position + positionOffset;
            rightArmRigTarget.rotation = rightHandVRTarget.rotation;
        }

        // Update tracked finger curls after animator update
        LeftFingerCurler?.Update();

        if (!PlayerController.isHoldingObject)
            RightFingerCurler?.Update();
    }
    
    private void UpdateIKWeights()
    {
        // Constants
        PlayerController.cameraLookRig1.weight = 0.45f;
        PlayerController.cameraLookRig2.weight = 1;
        PlayerController.leftArmRigSecondary.weight = 0;
        PlayerController.rightArmRigSecondary.weight = 0;

        PlayerController.leftArmRig.weight = 0;
        PlayerController.rightArmRig.weight = 0;
        leftArmVRRig.weight = 1;
        rightArmVRRig.weight = 1;
    }

    /// <summary>
    /// Show the spectator ghost
    /// </summary>
    public void ShowSpectatorGhost()
    {
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
        if (!playerGhost)
            return;
        
        playerGhost.SetVisible(false);
    }
    
    /// <summary>
    /// Override the target transform that the left hand of this player should go towards
    /// </summary>
    public void SnapLeftHandTo(Transform target, Vector3? positionOffset = null, Vector3? rotationOffset = null)
    {
        if (!target)
        {
            leftHandTargetOverride = null;
            return;
        }

        leftHandTargetOverride = new HandTargetOverride
        {
            Transform = target,
            PositionOffset = positionOffset ?? Vector3.zero,
            RotationOffset = rotationOffset ?? Vector3.zero
        };
    }
    
    /// <summary>
    /// Override the target transform that the right hand of this player should go towards
    /// </summary>
    public void SnapRightHandTo(Transform target, Vector3? positionOffset = null, Vector3? rotationOffset = null)
    {
        if (!target)
        {
            rightHandTargetOverride = null;
            return;
        }

        rightHandTargetOverride = new HandTargetOverride
        {
            Transform = target,
            PositionOffset = positionOffset ?? Vector3.zero,
            RotationOffset = rotationOffset ?? Vector3.zero
        };
    }

    /// <summary>
    /// Properly clean up the IK and spectator ghost if a VR player leaves the game
    /// </summary>
    private void OnDestroy()
    {
        Bones.ResetToPrefabPositions();

        // Make sure to destroy immediately, otherwise the rig rebuilding will still use the constraints and cause errors
        DestroyImmediate(Bones.LeftArmRig.GetComponent<TwoBoneIKConstraint>());
        DestroyImmediate(Bones.RightArmRig.GetComponent<TwoBoneIKConstraint>());
        
        Bones.LeftArmRig.GetComponent<ChainIKConstraint>().weight = 1;
        Bones.RightArmRig.GetComponent<ChainIKConstraint>().weight = 1;

        // Rebuild rig now that we changed up the constraints
        GetComponentInChildren<RigBuilder>().Build();
        
        Destroy(playerGhost);
        
        foreach (var el in cleanupPool)
            Destroy(el);
        
        cleanupPool.Clear();
        prefsChannel.Dispose();
        rigChannel.Dispose();
        spectatorRigChannel.Dispose();
    }

    private void OnPrefsPacketReceived(ushort _, BinaryReader reader)
    {
        var steeringDisabled = reader.ReadBoolean();

        AdditionalData.DisableSteeringWheel = steeringDisabled;
    }

    private void OnRigDataReceived(ushort _, BinaryReader reader)
    {
        var rig = Serialization.Deserialize<Rig>(reader);
        
        leftController.localPosition = rig.LeftHandPosition;
        leftController.localEulerAngles = rig.LeftHandEulers;
        LeftFingerCurler?.SetCurls(rig.LeftHandFingers);

        rightController.localPosition = rig.RightHandPosition;
        rightController.localEulerAngles = rig.RightHandEulers;
        RightFingerCurler?.SetCurls(rig.RightHandFingers);

        cameraEulers = rig.CameraEulers;
        cameraPosAccounted = rig.CameraPosAccounted;
        modelOffset = rig.ModelOffset;
        specialAnimationPositionOffset = rig.SpecialAnimationPositionOffset;

        crouchState = rig.CrouchState;
        originRotation = rig.RotationOffset;
        cameraFloorOffset = rig.CameraFloorOffset;
    }

    private void OnSpectatorRigDataReceived(ushort _, BinaryReader reader)
    {
        var rig = Serialization.Deserialize<SpectatorRig>(reader);

        playerGhost.UpdateRig(rig);
    }

    public class PlayerData
    {
        public bool DisableSteeringWheel { get; set; }
    }

    /// <summary>
    /// A script for remote VR players that runs before other scripts in the game
    /// </summary>
    [DefaultExecutionOrder(-100)]
    private class VRNetPlayerEarly : MonoBehaviour
    {
        private VRNetPlayer player;

        private void Awake()
        {
            player = GetComponent<VRNetPlayer>();
        }

        private void LateUpdate()
        {
            UpdateBones();
        }

        private void UpdateBones()
        {
            player.Bones.ServerItemHolder.localPosition = new Vector3(0.002f, 0.056f, -0.046f);
            player.Bones.ServerItemHolder.localRotation = Quaternion.identity;
        }
    }
}

[Serialize]
public struct Rig
{
    public Vector3 RightHandPosition;
    public Vector3 RightHandEulers;
    public Fingers RightHandFingers;

    public Vector3 LeftHandPosition;
    public Vector3 LeftHandEulers;
    public Fingers LeftHandFingers;

    public Vector3 CameraEulers;
    public Vector3 CameraPosAccounted;
    public Vector3 ModelOffset;
    public Vector3 SpecialAnimationPositionOffset;
        
    public CrouchState CrouchState;
    public Vector3 RotationOffset;
    public float CameraFloorOffset;
}

[Serialize]
public struct SpectatorRig
{
    public Vector3 HeadPosition;
    public Vector3 HeadRotation;

    public Vector3 LeftHandPosition;
    public Vector3 LeftHandRotation;

    public Vector3 RightHandPosition;
    public Vector3 RightHandRotation;

    public bool ParentedToShip;
}
    
[Serialize]
public struct Fingers
{
    public byte Thumb;
    public byte Index;
    public byte Middle;
    public byte Ring;
    public byte Pinky;
}

public enum CrouchState : byte
{
    None,
    Roomscale,
    Button
}