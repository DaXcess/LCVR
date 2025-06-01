using System.Collections;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Player;
using UnityEngine;

namespace LCVR.Patches.Spectating;

/// <summary>
/// Generic patches for the free roam spectator functionality
/// </summary>
[LCVRPatch]
[HarmonyPatch]
internal static class SpectatorPlayerPatches
{
    private static readonly int GameOver = Animator.StringToHash("gameOver");
    private static readonly int Revive = Animator.StringToHash("revive");
    
    private static bool isSpectating;
    
    private static bool wasInElevator;
    private static bool wasInHangarShipRoom;
    private static bool wasInsideFactory;

    private static int lastSpectatedIndex = -1;

    private static bool allowSpectatorActions;

    /// <summary>
    /// Initialize values when joining a new game, since this class is static and values persist across games
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    [HarmonyPostfix]
    private static void OnGameJoined()
    {
        isSpectating = false;
    }
    
    /// <summary>
    /// Store some fields that need to be restored after death
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPrefix]
    private static void BeforePlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner)
            return;

        wasInElevator = __instance.isInElevator;
        wasInHangarShipRoom = __instance.isInHangarShipRoom;
        wasInsideFactory = __instance.isInsideFactory;
    }

    /// <summary>
    /// We died
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    private static void OnPlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || isSpectating || !__instance.AllowPlayerDeath())
            return;

        isSpectating = true;

        // Keep using the FPV camera after death
        __instance.ChangeAudioListenerToObject(__instance.gameplayCamera.gameObject);
        StartOfRound.Instance.SwitchCamera(__instance.gameplayCamera);

        // We should still be able to walk around
        __instance.isPlayerControlled = true;

        // We should still be able to see our hands
        __instance.thisPlayerModelArms.enabled = true;

        // Make sure effects are applied, according to the position of our own player
        __instance.spectatedPlayerScript = __instance;

        // Apply where player is from before death
        __instance.isInElevator = wasInElevator;
        __instance.isInHangarShipRoom = wasInHangarShipRoom;
        __instance.isInsideFactory = wasInsideFactory;

        // Apply audio reverb preset
        var audioPreset = (__instance.isInsideFactory, __instance.isInHangarShipRoom) switch
        {
            (true, _) => 2,
            (_, true) => 3,
            (false, false) => 1,
        };

        var presets = Object.FindObjectOfType<AudioReverbPresets>();
        if (presets)
            presets.audioPresets[audioPreset].ChangeAudioReverbForPlayer(__instance);

        // Make all large doors have no collision
        var switchables = Object.FindObjectsOfType<PowerSwitchable>();
        foreach (var switchable in switchables)
        {
            var leftDoor = switchable.transform.Find("BigDoorLeft")?.GetComponent<Collider>();
            var rightDoor = switchable.transform.Find("BigDoorRight")?.GetComponent<Collider>();

            if (leftDoor == null || rightDoor == null)
                continue;

            leftDoor.isTrigger = true;
            rightDoor.isTrigger = true;
        }

        // Make all normal doors have no collision
        var doors = Object.FindObjectsOfType<DoorLock>();
        foreach (var door in doors)
        {
            door.GetComponent<Collider>().isTrigger = true;
        }

        // Make sure the ship doors have no collision
        var shipDoorContainer = Object.FindObjectOfType<HangarShipDoor>();
        var shipDoorLeft = shipDoorContainer.transform.Find("HangarDoorLeft (1)");
        var shipDoorRight = shipDoorContainer.transform.Find("HangarDoorRight (1)");
        var shipDoorWall = shipDoorContainer.transform.Find("Cube");

        shipDoorLeft.GetComponent<BoxCollider>().isTrigger = true;
        shipDoorRight.GetComponent<BoxCollider>().isTrigger = true;
        shipDoorWall.GetComponent<BoxCollider>().isTrigger = true;

        // Of course, Nutcracker with special AI behavior
        var nutcrackers = Object.FindObjectsOfType<NutcrackerEnemyAI>();
        foreach (var nutcracker in nutcrackers)
            if (nutcracker.lastPlayerSeenMoving == (int)__instance.playerClientId)
                nutcracker.lastPlayerSeenMoving = -1;

        // Clear spectator text
        HUDManager.Instance.spectatingPlayerText.text = "";

        // Clear fear effect
        StartOfRound.Instance.fearLevel = 0;

        // Disable interactors
        VRSession.Instance.LocalPlayer.LeftHandInteractor.enabled = false;
        VRSession.Instance.LocalPlayer.RightHandInteractor.enabled = false;

        VRSession.Instance.VolumeManager.Death();
        VRSession.Instance.StartCoroutine(delayedShowDeathScreen());
    }

    private static IEnumerator delayedShowDeathScreen()
    {
        allowSpectatorActions = false;
        
        VRSession.Instance.HUD.ToggleDeathScreen(true);

        HUDManager.Instance.gameOverAnimator.ResetTrigger(GameOver);
        HUDManager.Instance.gameOverAnimator.ResetTrigger(Revive);

        yield return new WaitForSeconds(2.5f);

        HUDManager.Instance.gameOverAnimator.SetTrigger(GameOver);

        yield return new WaitForSeconds(4.3f);

        allowSpectatorActions = true;
        
        // Fixes an issue where if you pick up an item while dying it stays in your inventory
        StartOfRound.Instance.localPlayerController.DropAllHeldItems(false);
    }

    /// <summary>
    /// Quick fix for when you are not the host, and some fields are set after `KillPlayer` has already executed
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc))]
    [HarmonyPostfix]
    private static void KillPlayerClientRpc(PlayerControllerB __instance, int playerId)
    {
        if (playerId != (int)StartOfRound.Instance.localPlayerController.playerClientId)
            return;

        __instance.isPlayerControlled = true;
        __instance.thisPlayerModelArms.enabled = true;
    }

    /// <summary>
    /// If we were dead, perform necessary actions to recover properly
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
    [HarmonyPrefix]
    private static void OnPlayerRevived()
    {
        isSpectating = false;
        
        var player = StartOfRound.Instance.localPlayerController;

        if (!player.isPlayerDead)
            return;

        VRSession.Instance.VolumeManager.Saturation = 0;
        VRSession.Instance.VolumeManager.VignetteIntensity = 0;

        player.thisPlayerModelArms.enabled = false;
        player.isPlayerControlled = false;
        player.takingFallDamage = false;
        player.isCameraDisabled = true;

        // Re-enable interactors
        VRSession.Instance.LocalPlayer.LeftHandInteractor.enabled = true;
        VRSession.Instance.LocalPlayer.RightHandInteractor.enabled = true;

        // Make sure the ship doors have collision again
        var shipDoorContainer = Object.FindObjectOfType<HangarShipDoor>();
        var shipDoorLeft = shipDoorContainer.transform.Find("HangarDoorLeft (1)");
        var shipDoorRight = shipDoorContainer.transform.Find("HangarDoorRight (1)");
        var shipDoorWall = shipDoorContainer.transform.Find("Cube");

        shipDoorLeft.GetComponent<BoxCollider>().isTrigger = false;
        shipDoorRight.GetComponent<BoxCollider>().isTrigger = false;
        shipDoorWall.GetComponent<BoxCollider>().isTrigger = false;
        
        // Make sure the car has collision again
        player.GetComponent<CharacterController>().excludeLayers = 0;
        
        VRSession.Instance.HUD.ToggleSpectatorLight(false);
    }

    /// <summary>
    /// Make sure we have infinite sprint and not being hindered by injuries if we're dead
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    // [HarmonyPostfix]
    private static void OnPlayerUpdate(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || !__instance.isPlayerControlled || !__instance.isPlayerDead)
            return;

        __instance.sprintMeter = 1;
        __instance.isExhausted = false;
        __instance.criticallyInjured = false;
        __instance.takingFallDamage = false;
    }

    /// <summary>
    /// Handle spectating other players
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ActivateItem_performed))]
    [HarmonyPostfix]
    private static void SpectateNextPlayer(PlayerControllerB __instance)
    {
        if (__instance != StartOfRound.Instance.localPlayerController || !__instance.isPlayerDead)
            return;

        // Don't allow switching until after 2.5s
        if (!allowSpectatorActions)
            return;

        // Find player to spectate
        for (var i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++)
        {
            var nextIndex = (lastSpectatedIndex + 1 + i) % StartOfRound.Instance.allPlayerScripts.Length;
            var script = StartOfRound.Instance.allPlayerScripts[nextIndex];

            if (script == __instance || script.isPlayerDead || !script.isPlayerControlled)
                continue;

            lastSpectatedIndex = nextIndex;

            __instance.TeleportPlayer(script.transform.position);
            __instance.isInElevator = script.isInElevator;
            __instance.isInHangarShipRoom = script.isInHangarShipRoom;
            __instance.isInsideFactory = script.isInsideFactory;

            var audioPreset = (script.isInsideFactory, script.isInHangarShipRoom) switch
            {
                (true, _) => 2,
                (_, true) => 3,
                (false, false) => 1,
            };

            var presets = Object.FindObjectOfType<AudioReverbPresets>();
            if (presets)
                presets.audioPresets[audioPreset].ChangeAudioReverbForPlayer(__instance);

            break;
        }
    }

    /// <summary>
    /// Make sure the spectating text is not visible as it's no longer possible to spectate an individual player
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetSpectatedPlayerEffects))]
    [HarmonyPostfix]
    private static void HideSpectatingText()
    {
        HUDManager.Instance.spectatingPlayerText.text = "";
    }

    /// <summary>
    /// Toggle death screen UI by pressing the secondary use button
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ItemSecondaryUse_performed))]
    [HarmonyPostfix]
    private static void OnToggleDeathScreen(PlayerControllerB __instance)
    {
        if (!allowSpectatorActions || __instance != StartOfRound.Instance.localPlayerController || !__instance.isPlayerDead)
            return;

        VRSession.Instance.HUD.ToggleDeathScreen();
    }

    /// <summary>
    /// Toggle spectator light by pressing the tertiary use button
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Discard_performed))]
    [HarmonyPostfix]
    private static void OnToggleSpectatorLight(PlayerControllerB __instance)
    {
        if (__instance != StartOfRound.Instance.localPlayerController || !__instance.isPlayerDead)
            return;

        VRSession.Instance.HUD.ToggleSpectatorLight();
    }

    /// <summary>
    /// Prevent the game from spectating a player, since we use our own logic for this
    /// </summary>
    /// <returns></returns>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpectateNextPlayer))]
    [HarmonyPrefix]
    private static bool PreventSpectateNextPlayer()
    {
        return false;
    }

    /// <summary>
    /// Prevent dead player from being affected by quicksand
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CheckConditionsForSinkingInQuicksand))]
    [HarmonyPrefix]
    private static bool PreventQuicksandOnDeadPlayer(PlayerControllerB __instance)
    {
        return !__instance.isPlayerDead;
    }

    /// <summary>
    /// Enable night vision lights when in factory and when dead
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetNightVisionEnabled))]
    [HarmonyPrefix]
    private static bool SetNightVisionEnabled(PlayerControllerB __instance)
    {
        if (__instance != StartOfRound.Instance.localPlayerController || !__instance.isPlayerDead)
            return true;

        __instance.nightVision.enabled = __instance.isInsideFactory;
        return false;
    }
}