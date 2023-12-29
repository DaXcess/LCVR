using GameNetcodeStuff;
using HarmonyLib;
using LCVR.Assets;
using LCVR.Input;
using LCVR.Items;
using LCVR.Networking;
using LCVR.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace LCVR.Patches
{
    [LCVRPatch]
    [HarmonyPatch(typeof(PlayerControllerB), "Update")]
    public static class PlayerControllerB_Update_Patch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            // Remove HUD rotating
            for (int i = 111; i <= 123; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            // Remove FOV updating
            for (int i = 305; i <= 316; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }

            // Remove Player Rig Updating
            //for (int i = 1965; i <= 1990; i++)
            //{
            //    codes[i].opcode = OpCodes.Nop;
            //    codes[i].operand = null;
            //}

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

            // Remove Player Rig Updating
            //for (int i = 497; i <= 516; i++)
            //{
            //    codes[i].opcode = OpCodes.Nop;
            //    codes[i].operand = null;
            //}

            // Make it so player sends position updates more frequently (Multiplayer 6 DOF looks better with this)
            codes[138].operand = 0.025f;
            codes[141].operand = 0.025f;

            return codes.AsEnumerable();
        }
    }

    [LCVRPatch]
    [HarmonyPatch]
    internal static class PlayerControllerPatches
    {
        private static bool isDead = false;
        private static bool allowSpectatorPivot = false;
        private static bool lastPivotCallAllowed = false;

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

        [HarmonyPatch(typeof(PlayerControllerB), "Update")]
        [HarmonyPostfix]
        private static void UpdatePrefix(PlayerControllerB __instance)
        {
            if (__instance.IsInactivePlayer() || !__instance.IsOwner)
                return;

            __instance.localArmsMatchCamera = false;

            if (__instance.isPlayerControlled)
            {
                __instance.playerBodyAnimator.runtimeAnimatorController = AssetManager.localVrMetarig;
            }

            if (isDead && __instance.spectatedPlayerScript != null)
            {
                var isPlayerDead = __instance.spectatedPlayerScript.isPlayerDead;
                var spectatedPlayerDeadTimer = (float)typeof(PlayerControllerB).GetField("spectatedPlayerDeadTimer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(__instance);

                if (spectatedPlayerDeadTimer < 1.5f && isPlayerDead)
                {
                    allowSpectatorPivot = true;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB), "DamagePlayer")]
        [HarmonyPostfix]
        public static void AfterDamagePlayer()
        {
            VRPlayer.VibrateController(XRNode.LeftHand, 0.1f, 0.5f);
            VRPlayer.VibrateController(XRNode.RightHand, 0.1f, 0.5f);
        }

        [HarmonyPatch(typeof(PlayerControllerB), "PlayerLookInput")]
        [HarmonyPostfix]
        private static void AfterPlayerLookInput(PlayerControllerB __instance)
        {
            if (isDead)
                return;

            var rot = Actions.XR_HeadRotation.ReadValue<Quaternion>().eulerAngles.x;

            if (rot > 180)
            {
                rot -= 360;
            }

            typeof(PlayerControllerB).GetField("cameraUp", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(__instance, rot);
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
            if (__instance.isGrabbingObjectAnimation)
                return false;

            var ray = new Ray(__instance.gameplayCamera.transform.position, __instance.gameplayCamera.transform.forward);
            if (!__instance.isFreeCamera && Physics.SphereCast(ray, 0.5f, out var hit, 5, 8))
                hit.collider.gameObject.GetComponent<PlayerControllerB>()?.ShowNameBillboard();

            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "ClickHoldInteraction")]
        [HarmonyPrefix]
        private static bool ClickHoldInteractionPrefix()
        {
            return false;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "KillPlayer")]
        [HarmonyPostfix]
        private static void OnPlayerDeath(PlayerControllerB __instance)
        {
            if (!__instance.IsOwner)
                return;

            isDead = true;

            Logger.Log("VR Player died");

            __instance.GetComponent<VRPlayer>().OnDeath();
        }

        // Shush I'm counting this as a player controller patch
        [HarmonyPatch(typeof(StartOfRound), "ReviveDeadPlayers")]
        [HarmonyPostfix]
        private static void OnPlayerRevived(StartOfRound __instance)
        {
            if (!isDead)
                return;

            isDead = false;

            Logger.Log("VR Player revived");

            __instance.localPlayerController.GetComponent<VRPlayer>().OnRevive();
        }

        [HarmonyPatch(typeof(PlayerControllerB), "RaycastSpectateCameraAroundPivot")]
        [HarmonyPrefix]
        private static bool OnSpectatorCamPivot(PlayerControllerB __instance)
        {
            if (!lastPivotCallAllowed && allowSpectatorPivot)
                __instance.playersManager.spectateCamera.transform.SetParent(GameObject.Find("SpectateCameraPivot").transform, false);
            else if (lastPivotCallAllowed && !allowSpectatorPivot && __instance.spectatedPlayerScript != null)
                __instance.playersManager.spectateCamera.transform.SetParent(__instance.spectatedPlayerScript.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera"), false);

            lastPivotCallAllowed = allowSpectatorPivot;

            return allowSpectatorPivot;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SpectateNextPlayer")]
        [HarmonyPostfix]
        private static void OnSpectateNextPlayer(PlayerControllerB __instance)
        {
            Logger.LogDebug($"SpectateNextPlayer called (IsOwner: {__instance.IsOwner})");

            if (!__instance.IsOwner)
                return;

            var spectateCamera = StartOfRound.Instance.spectateCamera;
            var playerToSpectate = __instance.spectatedPlayerScript;
            var playerCamera = playerToSpectate.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera");

            spectateCamera.transform.SetParent(playerCamera, false);
            spectateCamera.transform.localEulerAngles = Vector3.zero;
            spectateCamera.transform.localPosition = Vector3.zero;
            spectateCamera.nearClipPlane = 0.43f;
        }

        [HarmonyPatch(typeof(PlayerControllerB), "SwitchToItemSlot")]
        [HarmonyPostfix]
        private static void SwitchedToItemSlot(PlayerControllerB __instance)
        {
            // Ignore if it's someone else, that is handled by the universal patch
            if (!__instance.IsOwner)
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

        private static bool IsInactivePlayer(this PlayerControllerB player)
        {
            if (player == StartOfRound.Instance.localPlayerController)
                return false;

            return !player.transform.Find("ScavengerModel/metarig/CameraContainer/MainCamera").GetComponent<Camera>().enabled;
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
                {
                    __instance.playerBodyAnimator.runtimeAnimatorController = AssetManager.remoteVrMetarig;
                }
                // Used to restore the original metarig if a VR player leaves and a non-vr players join in their place
                else
                {
                    __instance.playerBodyAnimator.runtimeAnimatorController = __instance.playersManager.otherClientsAnimatorController;
                }
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

            Logger.LogDebug(item.itemProperties.itemName);

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
}
