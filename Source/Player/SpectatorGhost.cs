using LCVR.Networking;
using TMPro;
using UnityEngine;

namespace LCVR.Player;

public class SpectatorGhost : MonoBehaviour
{
    public VRNetPlayer player; 
    
    [SerializeField] private Transform head;
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    
    [SerializeField] private Transform usernameTransform;
    [SerializeField] private TextMeshProUGUI usernameText;
    [SerializeField] private CanvasGroup usernameAlpha;

    private Transform lastSyncedPhysicsParent;
    
    public bool ParentedToShip { get; private set; }
    public bool InHangarShipRoom { get; private set; }
    public bool InInterior { get; private set; }

    private void Start()
    {
        SetVisible(false);
        
        if (player.PlayerController.playerSteamId is 76561198438308784 or 76561199575858981)
        {
            usernameText.color = new Color(0, 1, 1, 1);
            usernameText.fontStyle = FontStyles.Bold;
        }

        usernameText.text = $"<noparse>{player.PlayerController.playerUsername}</noparse>";
    }

    private void Update()
    {
        usernameAlpha.alpha -= Time.deltaTime;
    }

    private void LateUpdate()
    {
        // Rotate spectator username billboard
        if (StartOfRound.Instance.localPlayerController.localVisorTargetPoint is not null)
            usernameTransform.LookAt(StartOfRound.Instance.localPlayerController.localVisorTargetPoint);
    }

    public void SetVisible(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
            renderer.enabled = visible;

        if (!visible)
            usernameAlpha.alpha = 0;
    }

    public void ShowNameBillboard()
    {
        usernameAlpha.alpha = 1;
    }

    public void UpdateRig(SpectatorRig rig)
    {
        ParentedToShip = rig.ParentedToShip;
        InHangarShipRoom = rig.InHangarShipRoom;
        InInterior = rig.InInterior;

        var parent = player.PlayerController.physicsParent ?? (ParentedToShip
            ? StartOfRound.Instance.elevatorTransform
            : StartOfRound.Instance.playersContainer);
        
        if (parent != lastSyncedPhysicsParent)
        {
            lastSyncedPhysicsParent = parent;
            
            transform.SetParent(parent, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        head.localPosition = rig.HeadPosition;
        head.eulerAngles = rig.HeadRotation;

        leftHand.localPosition = rig.LeftHandPosition;
        leftHand.eulerAngles = rig.LeftHandRotation;

        rightHand.localPosition = rig.RightHandPosition;
        rightHand.eulerAngles = rig.RightHandRotation;
    }
}