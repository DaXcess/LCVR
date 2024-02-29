using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Networking;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch(typeof(PlayerControllerB), "Update")]
public static class PlayerControllerB_Update_Patch
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
[HarmonyPatch(typeof(PlayerControllerB), "Update")]
public static class PlayerControllerB_Sprint_Patch
{
    public static float sprint = 0;

    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Override sprint
        int index = codes.FindLastIndex(x => x.operand == (object)"Move") + 5;

        codes[index++] = new(OpCodes.Ldsfld, Field(typeof(PlayerControllerB_Sprint_Patch), nameof(sprint)));
        codes[index] = new(OpCodes.Stloc_0);

        index = codes.FindLastIndex(x => x.operand == (object)"Sprint");

        int startIndex = index - 1;
        int endIndex = index + 4;

        for (int i = startIndex; i <= endIndex; i++)
        {
            codes[i].opcode = OpCodes.Nop;
            codes[i].operand = null;
        }

        return codes.AsEnumerable();
    }
}

[LCVRPatch]
[HarmonyPatch(typeof(PlayerControllerB), "LateUpdate")]
internal static class PlayerControllerB_LateUpdate_Patches
{
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);

        // Make it so player sends position updates more frequently (Multiplayer 6 DOF looks better with this)

        int index = codes.FindLastIndex(x => x.operand == (object)Method(typeof(PlayerControllerB), "NearOtherPlayers"));

        codes[index + 2].operand = 0.025f;
        codes[index + 5].operand = 0.025f;

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
    private static readonly FieldInfo cameraUpField = Field(typeof(PlayerControllerB), "cameraUp");

    private static void SetCameraUp(this PlayerControllerB player, float value)
    {
        cameraUpField.SetValue(player, value);
    }

    private static float GetCameraUp(this PlayerControllerB player)
    {
        return (float)cameraUpField.GetValue(player);
    }

    [HarmonyPatch(typeof(PlayerControllerB), "OnEnable")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PatchOnEnable(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var firstIndex = codes.FindIndex(code => code.opcode == OpCodes.Ldstr);

        for (var i = 0; i < 14; i++)
            codes[firstIndex + i * 8].operand = $"Movement/{codes[firstIndex + i * 8].operand}";

        return codes.AsEnumerable();
    }

    [HarmonyPatch(typeof(PlayerControllerB), "OnDisable")]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PatchOnDisable(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var firstIndex = codes.FindIndex(code => code.opcode == OpCodes.Ldstr);

        for (var i = 0; i < 14; i++)
            codes[firstIndex + i * 8].operand = $"Movement/{codes[firstIndex + i * 8].operand}";

        return codes.AsEnumerable();
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayer))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> PatchKillPlayer(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldfld, Field(typeof(PlayerControllerB), nameof(PlayerControllerB.localVisor))))
            .Advance(-1)
            .RemoveInstructions(7)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Adds an arbitrary deadzone since the ScrollMouse gets performed if you only even touch the joystick a little bit
    /// </summary>
    /// <returns></returns>
    [HarmonyPatch(typeof(PlayerControllerB), "ScrollMouse_performed")]
    [HarmonyPrefix]
    private static bool OnScroll(PlayerControllerB __instance, ref InputAction.CallbackContext context)
    {
        if (__instance.inTerminalMenu)
            return true;

        if (Mathf.Abs(context.ReadValue<float>()) < 0.75f)
            return false;

        return true;
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Crouch_performed")]
    [HarmonyPrefix]
    private static bool OnCrouchPerformed(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || __instance.IsInactivePlayer())
            return true;

        return !VRSession.Instance.LocalPlayer.IsRoomCrouching;
    }

    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    [HarmonyPostfix]
    private static void UpdatePrefix(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || __instance.IsInactivePlayer())
            return;

        __instance.localArmsMatchCamera = false;

        if (__instance.GetComponent<VRPlayer>() == null)
            return;

        if (__instance.isPlayerControlled)
            __instance.playerBodyAnimator.runtimeAnimatorController = AssetManager.localVrMetarig;
    }

    /// <summary>
    /// Send haptic feedback on damage received
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
    [HarmonyPostfix]
    public static void AfterDamagePlayer(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || __instance.isPlayerDead)
            return;

        VRSession.VibrateController(XRNode.LeftHand, 0.1f, 0.5f);
        VRSession.VibrateController(XRNode.RightHand, 0.1f, 0.5f);

        VRSession.Instance.VolumeManager.TakeDamage();
    }

    [HarmonyPatch(typeof(PlayerControllerB), "PlayerLookInput")]
    [HarmonyPostfix]
    private static void AfterPlayerLookInput(PlayerControllerB __instance)
    {
        // Handle camera up value
        var rot = Actions.Instance.HeadRotation.ReadValue<Quaternion>().eulerAngles.x;

        if (rot > 180)
            rot -= 360;

        __instance.SetCameraUp(rot);

        // Handle username billboard
        if (__instance.isGrabbingObjectAnimation)
            return;

        var ray = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
        if (!__instance.isFreeCamera && UnityEngine.Physics.SphereCast(ray, 0.5f, out var hit, 5, 8))
            hit.collider.gameObject.GetComponent<PlayerControllerB>()?.ShowNameBillboard();
    }

    [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SpawnPlayerAnimation))]
    [HarmonyPrefix]
    private static bool OnPlayerSpawnAnimation()
    {
        return false;
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SetHoverTipAndCurrentInteractTrigger")]
    [HarmonyPrefix]
    private static bool SetHoverTipAndCurrentInteractTriggerPrefix(PlayerControllerB __instance)
    {
        return false;
    }

    [HarmonyPatch(typeof(PlayerControllerB), "ClickHoldInteraction")]
    [HarmonyPrefix]
    private static bool ClickHoldInteractionPrefix()
    {
        return false;
    }

    /// <summary>
    /// Vibrates the controllers when the player dies.
    /// The actual cool `on death` code is inside the spectator patches.
    /// </summary>
    [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
    [HarmonyPostfix]
    private static void OnPlayerDeath(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner || __instance.IsInactivePlayer())
            return;

        Logger.Log("VR Player died");

        VRSession.VibrateController(XRNode.LeftHand, 1f, 1f);
        VRSession.VibrateController(XRNode.RightHand, 1f, 1f);
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot")]
    [HarmonyPostfix]
    private static void SwitchedToItemSlot(PlayerControllerB __instance)
    {
        // Ignore if it's someone else, that is handled by the universal patch
        if (!__instance.IsOwner || __instance.IsInactivePlayer())
            return;

        // Find held item
        var item = __instance.currentlyHeldObjectServer;
        if (item == null)
            return;

        // Add or enable VR item script on item if there is one for this item
        if (Player.Items.items.TryGetValue(item.itemProperties.itemName, out var type))
        {
            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalPlayerControllerPatches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    [HarmonyPostfix]
    private static void UpdatePrefix(PlayerControllerB __instance)
    {
        if (!__instance.IsOwner)
        {
            var networkPlayer = __instance.GetComponent<VRNetPlayer>();
            if (networkPlayer != null)
                __instance.playerBodyAnimator.runtimeAnimatorController = AssetManager.remoteVrMetarig;
            // Used to restore the original metarig if a VR player leaves and a non-vr players join in their place
            else if (__instance.playerBodyAnimator.runtimeAnimatorController == AssetManager.remoteVrMetarig)
                __instance.playerBodyAnimator.runtimeAnimatorController = __instance.playersManager.otherClientsAnimatorController;
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot")]
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
        if (Player.Items.items.TryGetValue(item.itemProperties.itemName, out var type))
        {
            var component = (MonoBehaviour)item.GetComponent(type);
            if (component == null)
                item.gameObject.AddComponent(type);
            else
                component.enabled = true;
        }
    }
}
