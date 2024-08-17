using HarmonyLib;
using LCVR.Assets;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LCVR.Physics.Interactions;

public class MuffleManager
{
    private readonly HashSet<ushort> muffledPlayers = [];
    private readonly Channel channel;

    public MuffleManager()
    {
        channel = VRSession.Instance.NetworkSystem.CreateChannel(ChannelType.Muffle);
        channel.OnPacketReceived += OnOtherPlayerMuffle;
    }

    public bool IsPlayerMuffled(ushort id)
    {
        return muffledPlayers.Contains(id);
    }

    public void SetLocalMuffled(bool muffled)
    {
        channel.SendPacket([muffled ? (byte)1 : (byte)0]);
    }

    private void OnOtherPlayerMuffle(ushort sender, BinaryReader reader)
    {
        var muffled = reader.ReadBoolean();
        
        if (!VRSession.Instance.NetworkSystem.TryGetPlayer(sender, out var player))
            return;

        if (!muffled)
        {
            muffledPlayers.Remove(sender);

            StartOfRound.Instance.UpdatePlayerVoiceEffects();
            return;
        }

        // Muffling may happen before the voice chat sources are set up, so check for null first
        if (player.PlayerController.currentVoiceChatAudioSource != null)
        {
            var occlude = player.PlayerController.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();
            occlude.overridingLowPass = true;
            occlude.lowPassOverride = 1000f;
        }

        muffledPlayers.Add(sender);
    }
}

public class Muffler : MonoBehaviour, VRInteractable
{
    private const float MAX_COUNTER = 20f;

    /// <summary>
    /// A list of items names which should prevent muffling with the right hand if held
    /// </summary>
    private static readonly HashSet<string> MUFFLED_ITEMS_IGNORE = [
        "Walkie-talkie",
        "TZP-Inhalant"
    ];

    private Coroutine stopMuffleCoroutine;

    private float counter;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    public bool Muffled { get; private set; }

    private void Update()
    {
        counter = Muffled
            ? Mathf.Min(counter + Time.deltaTime * 2, MAX_COUNTER)
            : Mathf.Max(counter - Time.deltaTime, 0);

        // Disable muffle if we picked up a two-handed item while muffled
        if (Muffled && VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer is { } heldObject &&
            heldObject.itemProperties.twoHanded)
            Muffled = false;

        VRSession.Instance.VolumeManager.Muffle(counter);
    }

    public void OnColliderEnter(VRInteractor interactor)
    {        
        var heldItem = VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer;
        if (heldItem && heldItem.itemProperties.twoHanded)
            return;
        
        if (interactor.IsRightHand && heldItem && MUFFLED_ITEMS_IGNORE.Contains(heldItem.itemProperties.itemName))
            return;

        if (stopMuffleCoroutine != null)
            StopCoroutine(stopMuffleCoroutine);

        if (Muffled)
            return;

        interactor.Vibrate(0.1f, 1f);

        Muffled = true;
        VRSession.Instance.MuffleManager.SetLocalMuffled(true);
    }

    public void OnColliderExit(VRInteractor interactor)
    {            
        var heldItem = VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer;

        if (interactor.IsRightHand)
        {
            if (heldItem && MUFFLED_ITEMS_IGNORE.Contains(heldItem.itemProperties.itemName))
                return;
        }

        if (stopMuffleCoroutine != null)
            StopCoroutine(stopMuffleCoroutine);
        
        stopMuffleCoroutine = StartCoroutine(delayedStopMuffle(interactor));
    }

    private IEnumerator delayedStopMuffle(VRInteractor interactor)
    {
        yield return new WaitForSeconds(0.5f);

        if (Muffled)
            interactor.Vibrate(0.2f, 0.1f);

        Muffled = false;
        VRSession.Instance.MuffleManager.SetLocalMuffled(false);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    public static Muffler Create()
    {
        if (Plugin.Config.DisableMuffleInteraction.Value)
            return null;

        var interactableObject = Instantiate(AssetManager.Interactable, VRSession.Instance.MainCamera.transform);
        interactableObject.transform.localPosition = new Vector3(0, -0.1f, 0.1f);
        interactableObject.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);

        return interactableObject.AddComponent<Muffler>();
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class MufflePatches
{
    /// <summary>
    /// Prevent the voice chat from making audible noise for enemies when muffled
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.DetectVoiceChatAmplitude))]
    [HarmonyPrefix]
    private static bool MuffleBlockSound()
    {
        if (VRSession.Instance?.LocalMuffler is not { } muffler)
            return true;

        return !muffler.Muffled;
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalMufflePatches
{
    /// <summary>
    /// Make sure the muffle effect stays active even when the player voice effects are updated
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.UpdatePlayerVoiceEffects))]
    [HarmonyPostfix]
    private static void OnUpdatePlayerVoiceEffects(StartOfRound __instance)
    {
        if (VRSession.Instance?.MuffleManager is not { } manager)
            return;

        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            return;

        for (var i = 0; i < __instance.allPlayerScripts.Length; i++)
        {
            var player = __instance.allPlayerScripts[i];

            if (player == __instance.localPlayerController || player.isPlayerDead)
                continue;

            if (!manager.IsPlayerMuffled((ushort)i))
                continue;

            var occlude = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();

            occlude.overridingLowPass = true;
            occlude.lowPassOverride = 1000f;
        }
    }
}
