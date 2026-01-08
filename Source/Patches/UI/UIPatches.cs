using HarmonyLib;
using LCVR.Assets;
using LCVR.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LCVR.Patches.UI;

[LCVRPatch]
[HarmonyPatch]
internal static class UIPatches
{
    /// <summary>
    /// Show our custom VR init screen instead of the vanilla one
    /// </summary>
    [HarmonyPatch(typeof(PreInitSceneScript), nameof(PreInitSceneScript.Start))]
    [HarmonyPostfix]
    private static void OnPreInitMenuShown(PreInitSceneScript __instance)
    {
        Object.FindObjectOfType<EventSystem>().gameObject.SetActive(false);
        
        SceneManager.LoadSceneAsync("LCVR Init Scene", LoadSceneMode.Additive);
    }

    /// <summary>
    /// This function runs when the main menu is shown
    /// </summary>
    [HarmonyPatch(typeof(MenuManager), nameof(MenuManager.Start))]
    [HarmonyPostfix]
    private static void OnMainMenuShown(MenuManager __instance)
    {
        Object.Instantiate(__instance.isInitScene
            ? AssetManager.InitMenuEnvironment
            : AssetManager.MainMenuEnvironment);
    }

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
    /// Create the VR settings menu when the UI loads in flatscreen
    /// </summary>
    [HarmonyPatch(typeof(SettingsOption), nameof(SettingsOption.OnEnable))]
    [HarmonyPostfix]
    private static void InjectSettingsMenu(SettingsOption __instance)
    {
        if (VRSession.InVR || Plugin.Config.DisableSettingsButton.Value || __instance.name is not "SetToDefault")
            return;

        var isInGame = SceneManager.GetActiveScene().name is not "MainMenu";
        if (isInGame)
            return;
        
        Object.Destroy(__instance);

        var buttonObject = Object.Instantiate(__instance.gameObject, __instance.transform.parent);
        var button = buttonObject.GetComponent<Button>();

        buttonObject.name = "VRSettings";
        buttonObject.transform.localPosition += Vector3.up * 36;
        buttonObject.GetComponentInChildren<TextMeshProUGUI>().text = "> VR Settings";
        buttonObject.transform.SetSiblingIndex(13);

        var settingsPanel = Object.Instantiate(AssetManager.SettingsPanel, __instance.transform.parent.parent.parent);
        
        settingsPanel.transform.localPosition = Vector3.zero;
        settingsPanel.transform.localRotation = Quaternion.identity;
        settingsPanel.transform.localScale = Vector3.one;
        settingsPanel.SetActive(false);
        
        button.onClick.RemoveAllListeners();
        button.onClick.m_PersistentCalls.Clear();
        button.onClick.AddListener(() =>
        {
            Object.FindObjectOfType<MenuManager>()?.PlayConfirmSFX();
            
            settingsPanel.SetActive(true);
        });
    }
}
