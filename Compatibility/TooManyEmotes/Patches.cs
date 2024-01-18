using HarmonyLib;
using LCVR.Patches;
using TooManyEmotes;
using TooManyEmotes.Input;
using TooManyEmotes.Patches;

namespace LCVR.Compatibility
{
    /// <summary>
    /// Simulating the behaviour for TooManyEmotes as if "DisableEmotesForSelf" was set to True
    /// </summary>
    [LCVRPatch(dependency: "TooManyEmotes")]
    [HarmonyPatch]
    internal static class TooManyEmotesPatches
    {
        [HarmonyPatch(typeof(EmoteMenuManager), "GetInput")]
        [HarmonyPrefix]
        private static bool GetInput()
        {
            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "InitializePlayerCloneRenderObject")]
        [HarmonyPrefix]
        private static bool InitializePlayerCloneRenderObject()
        {
            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "InitializeUI")]
        [HarmonyPrefix]
        private static bool InitializeUI()
        {
            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "OnScrollMouse")]
        [HarmonyPrefix]
        private static bool OnScrollMouse(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "PreventInteractInMenu")]
        [HarmonyPrefix]
        private static bool PreventInteractInMenu(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "PreventItemSecondaryUseInMenu")]
        [HarmonyPrefix]
        private static bool PreventItemSecondaryUseInMenu(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(EmoteMenuManager), "PreventItemTertiaryUseInMenu")]
        [HarmonyPrefix]
        private static bool PreventItemTertiaryUseInMenu(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(Keybinds), "OnPressOpenEmoteMenu")]
        [HarmonyPrefix]
        private static bool OnPressOpenEmoteMenu()
        {
            return false;
        }

        [HarmonyPatch(typeof(Keybinds), "OnUpdateRotatePlayerEmoteModifier")]
        [HarmonyPrefix]
        private static bool OnUpdateRotatePlayerEmoteModifier()
        {
            return false;
        }

        [HarmonyPatch(typeof(Keybinds), "PerformEmoteLocal")]
        [HarmonyPrefix]
        private static bool PerformEmoteLocal()
        {
            return false;
        }

        [HarmonyPatch(typeof(PlayerPatcher), "CheckIfLookingAtPlayerSyncableEmote")]
        [HarmonyPrefix]
        private static bool CheckIfLookingAtPlayerSyncableEmote()
        {
            return false;
        }

        [HarmonyPatch(typeof(PlayerPatcher), "OnLocalClientReady")]
        [HarmonyPrefix]
        private static bool OnLocalClientReady()
        {
            return false;
        }

        [HarmonyPatch(typeof(PlayerPatcher), "OnSyncEmoteWithPlayer")]
        [HarmonyPrefix]
        private static bool OnSyncEmoteWithPlayer(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(PlayerPatcher), "OnUpdateCustomEmote", [typeof(int), typeof(PlayerData), typeof(PlayerData)])]
        [HarmonyPrefix]
        private static bool OnUpdateCustomEmote(PlayerData playerData)
        {
            var block = playerData == null || playerData.playerController == PlayerPatcher.localPlayerController;

            return !block;
        }

        [HarmonyPatch(typeof(PlayerPatcher), "PerformCustomEmoteLocalPrefix")]
        [HarmonyPrefix]
        private static bool PerformCustomEmoteLocalPrefix(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(ThirdPersonEmoteController), "AdjustCameraDistance")]
        [HarmonyPrefix]
        private static bool AdjustCameraDistance(ref bool __result)
        {
            __result = true;

            return false;
        }

        [HarmonyPatch(typeof(ThirdPersonEmoteController), "InitLocalPlayerController")]
        [HarmonyPrefix]
        private static bool InitLocalPlayerController()
        {
            return false;
        }

        [HarmonyPatch(typeof(ThirdPersonEmoteController), "OnPlayerSpawn")]
        [HarmonyPrefix]
        private static bool OnPlayerSpawn()
        {
            return false;
        }

        [HarmonyPatch(typeof(ThirdPersonEmoteController), "UseFreeCamWhileEmoting")]
        [HarmonyPrefix]
        private static bool UseFreeCamWhileEmoting(ref bool __result)
        {
            __result = true;

            return false;
        }
    }
}
