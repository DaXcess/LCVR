using HarmonyLib;
using LCVR.Assets;
using LCVR.Networking;
using LCVR.Patches;
using LCVR.Player;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCVR.Physics.Interactions;

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

    private bool isMuffled;
    private Coroutine stopMuffleCoroutine;

    private float counter = 0;

    public InteractableFlags Flags => InteractableFlags.BothHands;
    public bool Muffled => isMuffled;

    void Update()
    {
        if (isMuffled)
            counter = Mathf.Min(counter + Time.deltaTime * 2, MAX_COUNTER);
        else
            counter = Mathf.Max(counter - Time.deltaTime, 0);

        VRSession.Instance.VolumeManager.Muffle(counter);
    }

    public void OnColliderEnter(VRInteractor interactor)
    {
        if (interactor.IsRightHand && MUFFLED_ITEMS_IGNORE.Contains(VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer?.itemProperties.itemName))
            return;

        if (stopMuffleCoroutine != null)
            StopCoroutine(stopMuffleCoroutine);

        if (isMuffled)
            return;

        isMuffled = true;
        DNet.SetMuffled(true);
    }

    public void OnColliderExit(VRInteractor interactor)
    {
        if (interactor.IsRightHand)
        {
            switch (VRSession.Instance.LocalPlayer.PlayerController.currentlyHeldObjectServer?.itemProperties.itemName)
            {
                case "Walkie-talkie":
                    return;
            };
        }

        if (stopMuffleCoroutine != null)
            StopCoroutine(stopMuffleCoroutine);

        stopMuffleCoroutine = StartCoroutine(delayedStopMuffle());
    }

    private IEnumerator delayedStopMuffle()
    {
        yield return new WaitForSeconds(0.5f);

        isMuffled = false;
        DNet.SetMuffled(false);
    }

    public bool OnButtonPress(VRInteractor _) { return false; }
    public void OnButtonRelease(VRInteractor _) { }

    public static Muffler Create()
    {
        if (Plugin.Config.DisableMuffleInteraction.Value)
            return null;

        var interactableObject = Instantiate(AssetManager.interactable, VRSession.Instance.MainCamera.transform);
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
    [HarmonyPatch(typeof(StartOfRound), "DetectVoiceChatAmplitude")]
    [HarmonyPrefix]
    private static bool MuffleBlockSound()
    {
        return !VRSession.Instance?.Muffler?.Muffled ?? true;
    }

    /// <summary>
    /// Make sure the muffle effect stays active even when the player voice effects are updated
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.UpdatePlayerVoiceEffects))]
    [HarmonyPostfix]
    private static void OnUpdatePlayerVoiceEffects(StartOfRound __instance)
    {
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null)
            return;

        for (var i = 0; i < __instance.allPlayerScripts.Length; i++)
        {
            var player = __instance.allPlayerScripts[i];

            if (player == __instance.localPlayerController || player.isPlayerDead)
                continue;

            if (DNet.IsPlayerMuffled(i))
            {
                var occlude = player.currentVoiceChatAudioSource.GetComponent<OccludeAudio>();

                occlude.overridingLowPass = true;
                occlude.lowPassOverride = 1000f;
            }
        }
    }
}
