using HarmonyLib;
using LCVR.Assets;
using LCVR.Player;
using LCVR.UI;
using LCVR.UI.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class UIPatches
{
    /// <summary>
    /// Make sure the new input system is being used
    /// </summary>
    [HarmonyPatch(typeof(XRUIInputModule), nameof(XRUIInputModule.ProcessNavigation))]
    [HarmonyPrefix]
    private static void ForceNewInputSystem(XRUIInputModule __instance)
    {
        if (__instance.activeInputMode != XRUIInputModule.ActiveInputMode.InputSystemActions)
            __instance.activeInputMode = XRUIInputModule.ActiveInputMode.InputSystemActions;
    }
}

[LCVRPatch(LCVRPatchTarget.Universal)]
[HarmonyPatch]
internal static class UniversalUIPatches
{
    /// <summary>
    /// This function runs when the pre-init menu is shown
    /// </summary>
    [HarmonyPatch(typeof(PreInitSceneScript), nameof(PreInitSceneScript.Start))]
    [HarmonyPostfix]
    private static void OnPreInitMenuShown(PreInitSceneScript __instance)
    {
        __instance.gameObject.AddComponent<MainMenu>();
    }
    
    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        __instance.gameObject.AddComponent<MainMenu>();
    }

    /// <summary>
    /// Create the VR settings menu when the UI loads
    /// </summary>
    [HarmonyPatch(typeof(SettingsOption), nameof(SettingsOption.OnEnable))]
    [HarmonyPostfix]
    private static void InjectSettingsMenu(SettingsOption __instance)
    {
        if (Plugin.Config.DisableSettingsButton.Value || __instance.name is not "SetToDefault")
            return;
        
        var isInGame = SceneManager.GetActiveScene().name is not "MainMenu";
        if (isInGame && !VRSession.InVR)
            return;
        
        Object.Destroy(__instance);

        var buttonObject = Object.Instantiate(__instance.gameObject, __instance.transform.parent);
        var button = buttonObject.GetComponent<Button>();

        buttonObject.name = "VRSettings";
        buttonObject.transform.localPosition += Vector3.up * 36;
        buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = "> VR Settings";
        buttonObject.transform.SetSiblingIndex(13);

        var settingsPanel = Object.Instantiate(AssetManager.SettingsPanel,
            isInGame ? __instance.transform.parent.parent : __instance.transform.parent.parent.parent);
        var settingsManager = settingsPanel.GetComponent<SettingsManager>();

        if (isInGame)
        {
            settingsManager.DisableCategory("interaction");
            settingsManager.DisableCategory("car");
        }
        
        settingsPanel.transform.localPosition = Vector3.zero;
        settingsPanel.transform.localEulerAngles = Vector3.zero;
        settingsPanel.transform.localScale = Vector3.one;
        settingsPanel.transform.SetSiblingIndex(6);
        settingsPanel.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.m_PersistentCalls.Clear();
        button.onClick.AddListener(() =>
        {
            settingsManager.PlayButtonPressSfx();
            settingsPanel.SetActive(true);
        });
    }

    /// <summary>
    /// Make sure the VR settings menu is closed when the pause menu is closed
    /// </summary>
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.CloseQuickMenu))]
    [HarmonyPostfix]
    private static void CloseVRSettingsOnUnpause()
    {
        Object.FindObjectOfType<SettingsManager>(true)?.gameObject.SetActive(false);
    }
}
