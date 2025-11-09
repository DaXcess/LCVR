using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Configuration;
using LCVR.Assets;
using UnityEngine.InputSystem;
using static HarmonyLib.AccessTools;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
public class InputPatches
{
    private static InputActionAsset originalInputActions;
    
    /// <summary>
    /// Replace the lethal company inputs with VR inputs
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.Awake))]
    [HarmonyPostfix]
    internal static void OnCreateSettings(IngamePlayerSettings __instance)
    {
        var playerInput = __instance.playerInput;

        originalInputActions = playerInput.actions;

        // We have to set these two values to make sure the `actions` assignment doesn't make a copy which breaks rebinding
        playerInput.enabled = false;
        playerInput.m_Actions = null;

        playerInput.actions = AssetManager.VRActions;
        playerInput.defaultActionMap = "Movement";
        playerInput.neverAutoSwitchControlSchemes = false;
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

        // Re-enable player input because otherwise it's stuck at no devices in some cases
        playerInput.enabled = true;
    }

    internal static void RestoreOriginalBindings()
    {
        var playerInput = IngamePlayerSettings.Instance.playerInput;

        playerInput.actions = originalInputActions;
        playerInput.enabled = false;
        playerInput.enabled = true;
    }
    
    /// <summary>
    /// Disable vanilla rebinding as we're rolling our own logic
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.RebindKey))]
    [HarmonyPrefix]
    private static bool OnRebindKey(InputActionReference rebindableAction, SettingsOption optionUI, int rebindIndex)
    {
        return false;
    }

    /// <summary>
    /// Prevent resetting vanilla keybinds from VR (why would you even want to do that in the first place?)
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.ResetAllKeybinds))]
    [HarmonyPrefix]
    private static bool OnResetBindings()
    {
        return false;
    }

    /// <summary>
    /// Prevent discarding LC settings from overriding VR control binding overrides
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.DiscardChangedSettings))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DiscardSettingsDontTouchMyOverrides(
        IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
            [
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(IngamePlayerSettings.Settings), nameof(IngamePlayerSettings.Settings.keyBindings)))
            ])
            .Advance(-2)
            .RemoveInstructions(18)
            .InstructionEnumeration();
    }

    /// <summary>
    /// Replace the vanilla override loading with VR bindings override loading
    /// </summary>
    [HarmonyPatch(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.LoadSettingsFromPrefs))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LoadVROverrideBindings(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
            [
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(IngamePlayerSettings.Settings), nameof(IngamePlayerSettings.Settings.keyBindings)))
            ])
            .Advance(-2)
            .RemoveInstructions(3)
            .Insert(
                new CodeInstruction(OpCodes.Call,
                    typeof(Plugin).GetProperty(nameof(Plugin.Config), typeof(Config))!.GetMethod),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(Config), nameof(Config.ControllerBindingsOverride))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(ConfigEntry<string>), nameof(ConfigEntry<string>.Value)))
            )
            .MatchForward(false,
            [
                new CodeMatch(OpCodes.Ldfld,
                    Field(typeof(IngamePlayerSettings.Settings), nameof(IngamePlayerSettings.Settings.keyBindings)))
            ]).Advance(-2)
            .RemoveInstructions(3)
            .Insert(
                new CodeInstruction(OpCodes.Call,
                    typeof(Plugin).GetProperty(nameof(Plugin.Config), typeof(Config))!.GetMethod),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(Config), nameof(Config.ControllerBindingsOverride))),
                new CodeInstruction(OpCodes.Callvirt,
                    PropertyGetter(typeof(ConfigEntry<string>), nameof(ConfigEntry<string>.Value)))
            )
            .InstructionEnumeration();
    }

    /// <summary>
    /// Disable Lethal Company's legacy inputs
    /// </summary>
    [HarmonyPatch(typeof(PlayerActions), MethodType.Constructor)]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> DisableLegacyInputs(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Ldstr))
            .SetOperandAndAdvance(AssetManager.NullActions.ToJson())
            .InstructionEnumeration();
    }
}