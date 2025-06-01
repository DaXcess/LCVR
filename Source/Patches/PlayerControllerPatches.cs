using System;
using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Input;
using LCVR.Networking;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
public static class PlayerControllerB_Update_Patch
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        int startIndex = codes.FindIndex(x => x.operand == (object)Field(typeof(PlayerControllerB), nameof(PlayerControllerB.hasBegunSpectating))) + 1;
        int endIndex = codes.FindIndex(x => x.operand == (object)Method(typeof(PlayerControllerB), "SetNightVisionEnabled")) - 3;

        // Remove HUD rotating
        for (int i = startIndex; i <= endIndex; i++)
        {
            codes[i].opcode = OpCodes.Nop;
            codes[i].operand = null;
        }

        startIndex = codes.FindIndex(x => x.operand == (object)PropertyGetter(typeof(Camera), nameof(Camera.fieldOfView))) - 4;
        endIndex = codes.FindLastIndex(x => x.operand == (object)PropertySetter(typeof(Camera), nameof(Camera.fieldOfView)));

        // Remove FOV updating
        for (int i = startIndex; i <= endIndex; i++)
        {
            codes[i].opcode = OpCodes.Nop;
            codes[i].operand = null;
        }

        return codes.AsEnumerable();
    }
}

[LCVRPatch]
[HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LateUpdate))]
internal static class PlayerControllerB_LateUpdate_Patches
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Remove local visor updating (this will be done using hierarchy instead)
        var startIndex = codes.FindIndex(x => x.opcode == OpCodes.Ldfld && x.operand == (object)Field(typeof(PlayerControllerB), nameof(PlayerControllerB.localVisor))) - 1;
        var endIndex = startIndex + 21;

        for (int i = startIndex; i <= endIndex; i++)
        {
            codes[i].opcode = OpCodes.Nop;
            codes[i].operand = null;
        }

        return codes.AsEnumerable();
    }
}

[LCVRPatch]
[HarmonyPatch]
internal static class PlayerControllerPatches
{
    /// <summary>
    /// Override the game's sprint input with our VR player's sprint value
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> SprintPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldstr, "Sprint"))
            .Advance(-3)
            .SetAndAdvance(OpCodes.Call, ((Func<float>)GetSprintValue).Method)
            .RemoveInstructions(6)
            .InstructionEnumeration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetSprintValue() => VRSession.Instance?.LocalPlayer.sprintValue ?? 0;
    }

    /// <summary>
    /// Apply walking force based on camera rotation instead of player rotation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> WalkTowardsCameraDirection(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Stfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.walkForce))))
            .Advance(-18)
            .RemoveInstructions(14)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Call, ((Func<PlayerControllerB, Vector3>)GetWalkForce).Method))
            .InstructionEnumeration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 GetWalkForce(PlayerControllerB player)
        {
            var rotation = Quaternion.Euler(0, player.gameplayCamera.transform.eulerAngles.y, 0);

            return rotation * Vector3.right * player.moveInputVector.x +
                   rotation * Vector3.forward * player.moveInputVector.y;
        }
    }

    /// <summary>
    /// Prevent the local player visor from being moved when the player dies
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PatchKillPlayer(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.localVisor))))
            .Advance(-1)
            .RemoveInstructions(7)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Adds an arbitrary deadzone since the ScrollMouse gets performed if you only even touch the joystick a little bit
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ScrollMouse_performed))]
    [HarmonyPrefix]
    private static bool OnScroll(PlayerControllerB __instance, ref InputAction.CallbackContext context)
    {
        if (__instance.inTerminalMenu)
            return true;

        return !(Mathf.Abs(context.ReadValue<float>()) < 0.75f);
    }

    /// <summary>
    /// Prevent the crouch button from doing anything if we are currently roomscale crouching
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Crouch_performed))]
    [HarmonyPrefix]
    private static bool OnCrouchPerformed(PlayerControllerB __instance)
    {
        if (!__instance.IsLocalPlayer())
            return true;

        return !VRSession.Instance.LocalPlayer.IsRoomCrouching;
    }

    /// <summary>
    /// Make sure the local player has the correct animator applied
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    [HarmonyPostfix]
    private static void DontTouchArms(PlayerControllerB __instance)
    {
        if (__instance != GameNetworkManager.Instance.localPlayerController)
            return;

        __instance.localArmsMatchCamera = false;
    }

    /// <summary>
    /// Send haptic feedback on damage received
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DamagePlayer))]
    [HarmonyPostfix]
    public static void AfterDamagePlayer(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || __instance.isPlayerDead)
            return;

        VRSession.VibrateController(XRNode.LeftHand, 0.2f, 0.6f);
        VRSession.VibrateController(XRNode.RightHand, 0.2f, 0.6f);

        VRSession.Instance.VolumeManager.TakeDamage();
    }

    /// <summary>
    /// Override the camera up parameter by using the headset rotation instead of mouse movement
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.PlayerLookInput))]
    [HarmonyPostfix]
    private static void AfterPlayerLookInput(PlayerControllerB __instance)
    {
        // Handle camera up value
        var rot = Actions.Instance.HeadRotation.ReadValue<Quaternion>().eulerAngles.x;

        if (rot > 180)
            rot -= 360;

        __instance.cameraUp = rot;

        if (__instance.isGrabbingObjectAnimation)
            return;

        // Handle username billboard
        var ray = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
        if (!__instance.isFreeCamera && UnityEngine.Physics.SphereCast(ray, 0.5f, out var hit, 5, 8))
        {
            if (hit.collider.TryGetComponent<PlayerControllerB>(out var player))
            {
                player.ShowNameBillboard();
                return;
            }

            if (!__instance.isPlayerDead)
                return;

            if (!hit.collider.TryGetComponent<SpectatorGhost>(out var spectator))
                return;

            spectator.ShowNameBillboard();
        }
    }

    /// <summary>
    /// Prevent `LookClamped` from updating the camera in an undesired way
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.LookClamped))]
    [HarmonyPrefix]
    private static bool PreventLookClamped(PlayerControllerB __instance)
    {
        return !__instance.IsLocalPlayer();
    }

    /// <summary>
    /// Disable the player spawn animation in VR
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
    [HarmonyPrefix]
    private static bool OnPlayerSpawnAnimation()
    {
        return false;
    }

    /// <summary>
    /// Prevent vanilla "look at interactable" code from running, as we have our own implementation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetHoverTipAndCurrentInteractTrigger))]
    [HarmonyPrefix]
    private static bool SetHoverTipAndCurrentInteractTriggerPrefix()
    {
        return false;
    }

    /// <summary>
    /// Prevent LC's built in Interact handler as we're shipping our own
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Interact_performed))]
    [HarmonyPrefix]
    private static bool PreventBuiltinInteract()
    {
        return false;
    }

    /// <summary>
    /// Prevent vanilla "hold interactable" code from running, as we have our own implementation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ClickHoldInteraction))]
    [HarmonyPrefix]
    private static bool ClickHoldInteractionPrefix()
    {
        return false;
    }

    /// <summary>
    /// Vibrates the controllers when the player dies.
    /// The actual cool `on death` code is inside the spectator patches.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    private static void OnPlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsLocalPlayer() || __instance.isPlayerDead)
            return;

        VRSession.VibrateController(XRNode.LeftHand, 1f, 1f);
        VRSession.VibrateController(XRNode.RightHand, 1f, 1f);
    }

    /// <summary>
    /// Detect when the local player switches to a VR special item and apply scripts to that item
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SwitchToItemSlot))]
    [HarmonyPostfix]
    private static void SwitchedToItemSlot(PlayerControllerB __instance)
    {
        // Ignore if it's someone else, that is handled by the universal patch
        if (!__instance.IsLocalPlayer())
            return;

        // Find held item
        var item = __instance.currentlyHeldObjectServer;
        if (item == null)
            return;

        // Add or enable VR item script on item if there is one for this item
        if (Player.Items.items.TryGetValue(item.itemProperties.name, out var type))
        {
            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
    }

    /// <summary>
    /// Fix for water suffocation to be calculated from a clamped offset, so that play area hacks won't affect drowning
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetFaceUnderwaterFilters))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UnderwaterExploitFix(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                [new CodeMatch(OpCodes.Call, Method(typeof(Bounds), nameof(Bounds.Contains), [typeof(Vector3)]))])
            .Advance(-3)
            .RemoveInstructions(3)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Call,
                ((Func<PlayerControllerB, Vector3>)GetClampedCameraPosition).Method))
            .InstructionEnumeration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector3 GetClampedCameraPosition(PlayerControllerB player)
        {
            var actualHeight = player.transform.InverseTransformPoint(player.gameplayCamera.transform.position).y;

            return player.transform.position + Vector3.up * Mathf.Clamp(actualHeight, 0.5f, 2.35f);
        }
    }

    /// <summary>
    /// Allow interacting with items even when we're inside a special menu
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ActivateItem_performed))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> AllowActivateDuringMenu(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.inSpecialMenu))))
            .Advance(-1)
            .SetOpcodeAndAdvance(OpCodes.Nop)
            .RemoveInstructions(3)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent "CalculateSmoothLookingInput" from touching the camera rotation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateSmoothLookingInput))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CalculateSmoothLookPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    Method(typeof(Mathf), nameof(Mathf.LerpAngle), [typeof(float), typeof(float), typeof(float)])))
            .Advance(7)
            .RemoveInstructions(27)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent "CalculateNormalLookingInput" from touching the camera rotation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.CalculateNormalLookingInput))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> CalculateNormalLookPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.gameplayCamera))))
            .Advance(-1)
            .RemoveInstructions(17)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Prevent "Update" from touching the camera rotation
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> UpdateDontTouchCameraPatch(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldflda,
                    Field(typeof(PlayerControllerB), nameof(PlayerControllerB.syncFullCameraRotation))))
            .Advance(-12)
            .SetOpcodeAndAdvance(OpCodes.Nop)
            .RemoveInstructions(62)
            .MatchForward(false,
                new CodeMatch(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.cameraUp))))
            .Advance(7)
            .RemoveInstructions(17)
            .InstructionEnumeration();
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalPlayerControllerPatches
{
    /// <summary>
    /// Detect when a VR player switches to a VR special item and apply scripts to that item
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SwitchToItemSlot))]
    [HarmonyPostfix]
    private static void SwitchedToItemSlot(PlayerControllerB __instance)
    {
        // Ignore if it's us, we have the VR patch for that if we're in VR
        if (__instance.IsOwner)
            return;

        // Find held item
        var item = __instance.currentlyHeldObjectServer;
        if (item == null)
            return;

        // Find remote VR player, if they're not VR then we don't have to set up special VR items
        var remotePlayer = __instance.GetComponent<VRNetPlayer>();
        if (remotePlayer == null)
            return;

        // Add or enable VR item script on item if there is one for this item
        if (Player.Items.items.TryGetValue(item.itemProperties.name, out var type))
        {
            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
    }

    /// <summary>
    /// On death, show all other spectator ghosts
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyPostfix]
    private static void OnPlayerDeath(PlayerControllerB __instance)
    {
        if (__instance != StartOfRound.Instance.localPlayerController || !__instance.AllowPlayerDeath())
            return;

        foreach (var player in NetworkSystem.Instance.Players.Where(player =>
                     player.PlayerController.isPlayerDead))
            player.ShowSpectatorGhost();
    }

    /// <summary>
    /// Detect when another VR player has died
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc))]
    [HarmonyPostfix]
    private static void OnOtherPlayerDeath(PlayerControllerB __instance, int playerId)
    {
        if (!StartOfRound.Instance.localPlayerController.isPlayerDead)
            return;

        var player = __instance.playersManager.allPlayerObjects[playerId].GetComponent<PlayerControllerB>();
        if (player == StartOfRound.Instance.localPlayerController)
            return;

        if (!player.TryGetComponent<VRNetPlayer>(out var networkPlayer))
            return;

        networkPlayer.ShowSpectatorGhost();

        // Reset snap transforms on death
        networkPlayer.SnapLeftHandTo(null);
        networkPlayer.SnapRightHandTo(null);
    }

    /// <summary>
    /// Notify VR players that they have been revived
    /// </summary>
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ReviveDeadPlayers))]
    [HarmonyPostfix]
    private static void OnPlayerRevived()
    {
        foreach (var player in NetworkSystem.Instance.Players)
            player.HideSpectatorGhost();
    }
}