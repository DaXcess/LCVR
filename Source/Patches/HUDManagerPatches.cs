using System;
using HarmonyLib;
using LCVR.Player;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using LCVR.Assets;
using LCVR.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using static HarmonyLib.AccessTools;
using Object = UnityEngine.Object;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class HUDManagerPatches
{
    /// <summary>
    /// Disables the ping scan if you are in the pause menu
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.CanPlayerScan))]
    [HarmonyPrefix]
    private static bool CanPlayerScan(ref bool __result)
    {
        if (GameNetworkManager.Instance.localPlayerController.quickMenuManager.isMenuOpen)
        {
            __result = false;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Since the HUD in VR is decoupled from the main HUD element in the game, make sure we manually hide the elements
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.HideHUD))]
    [HarmonyPostfix]
    private static void HideHUD(bool hide)
    {
        VRSession.Instance.HUD.HideHUD(hide);
    }

    /// <summary>
    /// Fix for the leave early button not working, by making the "PingScan" binding function as the leave early button
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.Update))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> LeaveEarlyFixTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var startIndex = codes.FindIndex(instruction =>
            instruction.opcode == OpCodes.Callvirt &&
            (MethodInfo)instruction.operand == PropertyGetter(typeof(PlayerActions), "Movement")) - 2;

        var labels = codes[startIndex].labels;
        codes[startIndex++] = new(OpCodes.Callvirt,
            PropertyGetter(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.Instance)))
        {
            labels = labels
        };

        codes[startIndex++] = new(OpCodes.Ldfld,
            Field(typeof(IngamePlayerSettings), nameof(IngamePlayerSettings.playerInput)));
        codes[startIndex++] = new(OpCodes.Callvirt, PropertyGetter(typeof(PlayerInput), nameof(PlayerInput.actions)));
        codes[startIndex++] = new(OpCodes.Ldstr, "PingScan");
        codes[startIndex++] = new(OpCodes.Ldc_I4_0);
        codes[startIndex++] = new(OpCodes.Callvirt,
            Method(typeof(InputActionAsset), nameof(InputActionAsset.FindAction), [typeof(string), typeof(bool)]));
        codes[startIndex] = new(OpCodes.Callvirt, Method(typeof(InputAction), nameof(InputAction.IsPressed)));

        return codes.AsEnumerable();
    }

    /// <summary>
    /// Fix that disables all lights and scan nodes on scrap found
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.DisplayNewScrapFound))]
    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> ScrapFoundNoLightsAndScan(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false, new CodeMatch(OpCodes.Stloc_0))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, ((Action<GameObject>)DisableComponents).Method)
            )
            .InstructionEnumeration();

        static void DisableComponents(GameObject @object)
        {
            foreach (var light in @object.GetComponentsInChildren<Light>(true))
                light.enabled = false;

            foreach (var node in @object.GetComponentsInChildren<ScanNodeProperties>())
                Object.Destroy(node);
        }
    }

    /// <summary>
    /// Update the spectator UI to set new dead players' UI to be always on top when newly instantiated
    /// </summary>
    [HarmonyPatch(typeof(HUDManager), nameof(HUDManager.UpdateBoxesSpectateUI))]
    [HarmonyTranspiler]
    [HarmonyDebug]
    private static IEnumerable<CodeInstruction> SpectatorBoxAlwaysOnTop(IEnumerable<CodeInstruction> instructions)
    {
        return new CodeMatcher(instructions)
            .MatchForward(false,
                new CodeMatch(OpCodes.Call,
                    typeof(Object)
                        .GetMethods(all).SingleOrDefault(method => method.ContainsGenericParameters && method.GetParameters().Length == 3 &&
                                                                   method.GetParameters()[2].ParameterType == typeof(bool))!
                        .MakeGenericMethod(typeof(GameObject))))
            .Advance(2)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Call, ((Action<GameObject>)SetAlwaysOnTop).Method)
            )
            .InstructionEnumeration();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetAlwaysOnTop(GameObject obj)
        {
            foreach (var element in obj.GetComponentsInChildren<Image>(true))
            {
                if (element.materialForRendering == null)
                    continue;

                if (!VRHUD.materialMappings.TryGetValue(element.materialForRendering, out var materialCopy))
                {
                    materialCopy = new Material(element.materialForRendering);
                    VRHUD.materialMappings.Add(element.materialForRendering, materialCopy);
                }

                materialCopy.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                element.material = materialCopy;
            }

            foreach (var shit in obj.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                shit.m_fontMaterial = shit.CreateMaterialInstance(shit.m_sharedMaterial);
                shit.m_sharedMaterial = shit.m_fontMaterial;
                shit.m_sharedMaterial.shader = AssetManager.TMPAlwaysOnTop;
            }
        }
    }
}