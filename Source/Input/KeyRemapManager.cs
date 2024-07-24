using System.Collections.Generic;
using HarmonyLib;
using LCVR.Assets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LCVR.Input;

public class KeyRemapManager : MonoBehaviour
{
    /// <summary>
    /// Due to many VR controllers also reporting "touched" state, we have to disable them as they interfere with
    /// "pressed" bindings (some touched bindings are allowed, since they don't have a corresponding "pressed" binding)
    /// </summary>
    private readonly string[] DISALLOWED_BINDINGS =
    [
        "<WMRSpatialController>/touchpadTouched",
        "<OculusTouchController>/primaryTouched",
        "<OculusTouchController>/secondaryTouched",
        "<OculusTouchController>/triggerTouched",
        "<OculusTouchController>/thumbstickTouched",
        "<ViveController>/trackpadTouched",
        "<ValveIndexController>/systemTouched",
        "<ValveIndexController>/primaryTouched",
        "<ValveIndexController>/secondaryTouched",
        "<ValveIndexController>/gripForce",
        "<ValveIndexController>/triggerTouched",
        "<ValveIndexController>/thumbstickTouched",
        "<ValveIndexController>/trackpadTouched",
        "<ValveIndexController>/trackpadForce",
        "<QuestProTouchController>/primaryTouched",
        "<QuestProTouchController>/secondaryTouched",
        "<QuestProTouchController>/triggerTouched",
        "<QuestProTouchController>/thumbstickTouched",
        "<QuestProTouchController>/triggerCurl",
        "<QuestProTouchController>/triggerSlide",
        "<QuestProTouchController>/triggerProximity",
        "<QuestProTouchController>/thumbProximity",
        "*/isTracked",
    ];

    public static KeyRemapManager Instance { get; private set; }

    private KepRemapPanel panel;
    private TextMeshProUGUI sectionText;

    private PlayerInput playerInput;
    private InputActionRebindingExtensions.RebindingOperation currentOperation;
    private SettingsOption currentOption;
    private List<(RemappableControl, SettingsOption)> controlsList = [];

    private float lastRebindTime;

    private void Awake()
    {
        Instance = this;
        panel = GetComponent<KepRemapPanel>();
        playerInput = IngamePlayerSettings.Instance.playerInput;

        // Disable remapping of keyboard/gamepad as it can softlock the game in VR
        GetComponentsInChildren<SettingsOption>().DoIf(opt => opt.optionType == SettingsOptionType.ChangeBinding,
            opt => opt.GetComponent<Button>().enabled = false);

        var vertOffset = (panel.maxVertical + 2) * 2;
        var sectionTextObj = Instantiate(panel.sectionTextPrefab, panel.keyRemapContainer);
        sectionTextObj.GetComponent<RectTransform>().anchoredPosition =
            new Vector2(-40, -panel.verticalOffset * vertOffset);
        sectionText = sectionTextObj.GetComponentInChildren<TextMeshProUGUI>();
        sectionText.text = string.IsNullOrEmpty(playerInput.currentControlScheme)
            ? "PLEASE CONNECT BOTH CONTROLLERS"
            : $"VR CONTROLLERS ({playerInput.currentControlScheme})";
        panel.keySlots.Add(sectionTextObj);

        panel.currentVertical = 0;
        panel.currentHorizontal = 0;

        var position = new Vector2(panel.horizontalOffset * panel.currentHorizontal,
            -panel.verticalOffset * (panel.currentVertical + vertOffset));

        // Create remapping buttons
        foreach (var remappableKey in AssetManager.RemappableControls.controls)
        {
            var obj = Instantiate(panel.keyRemapSlotPrefab, panel.keyRemapContainer);
            panel.keySlots.Add(obj);

            obj.GetComponentInChildren<TextMeshProUGUI>().text = remappableKey.controlName;
            obj.GetComponent<RectTransform>().anchoredPosition = position;

            var option = obj.GetComponentInChildren<SettingsOption>();
            var image = new GameObject("ControlImage").AddComponent<Image>();
            image.transform.SetParent(option.currentlyUsedKeyText.transform.parent, false);
            image.preserveAspect = true;

            var irt = image.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(25, 25);
            irt.localEulerAngles = new Vector3(0, 0, -90);

            panel.currentVertical++;
            if (panel.currentVertical > panel.maxVertical)
            {
                panel.currentVertical = 0;
                panel.currentHorizontal++;
            }

            position = new Vector2(panel.horizontalOffset * panel.currentHorizontal,
                -panel.verticalOffset * (panel.currentVertical + vertOffset));

            controlsList.Add((remappableKey, option));
        }

        // Fix weird ass menu transforms
        var rt = panel.keyRemapContainer.parent.GetComponent<RectTransform>();
        rt.offsetMax = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 1050);

        panel.keyRemapContainer.localPosition = new Vector3(panel.keyRemapContainer.localPosition.x, -20, 0);

        // Listen to controller changes
        playerInput.onControlsChanged += OnControlsChanged;

        ReloadBindings();
    }

    private void OnDestroy()
    {
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    /// <summary>
    /// If the current control scheme changes (somehow), reload the bindings for the new scheme
    /// </summary>
    private void OnControlsChanged(PlayerInput input)
    {
        sectionText.text = $"VR CONTROLLERS ({input.currentControlScheme})";

        ReloadBindings();
    }

    /// <summary>
    /// Reload the bindings and icons for the current control scheme
    /// </summary>
    private void ReloadBindings()
    {
        foreach (var (key, option) in controlsList)
        {
            var bindingIndex = Mathf.Max(key.bindingIndex, 0) +
                               Mathf.Max(
                                   key.currentInput.action.GetBindingIndex(playerInput.currentControlScheme),
                                   0);

            option.rebindableActionBindingIndex = bindingIndex;
            option.rebindableAction = key.currentInput;
            option.currentlyUsedKeyText.text = "";

            var image = option.transform.Find("ControlImage").GetComponent<Image>();
            
            image.sprite = string.IsNullOrEmpty(playerInput.currentControlScheme)
                ? null
                : AssetManager.RemappableControls.icons[
                    option.rebindableAction.action.bindings[bindingIndex].effectivePath];
            image.color = image.sprite == null ? new Color(0, 0, 0, 0) : new Color(1, 1, 1, 1);
        }
    }

    /// <summary>
    /// Initiate the interactive rebinding of a control binding
    /// </summary>
    public void StartRebind(InputActionReference action, SettingsOption option, int rebindIndex)
    {
        // Prevent accidentally re-triggering the rebind process
        if (Time.realtimeSinceStartup - lastRebindTime < 0.5f)
            return;
        
        // Don't allow rebinding until a controller scheme is known
        if (string.IsNullOrEmpty(playerInput.currentControlScheme))
            return;

        if (currentOperation != null)
        {
            currentOperation.Dispose();
            if (currentOption != null)
            {
                currentOption.GetComponentInChildren<Image>().enabled = true;
                currentOption.waitingForInput.SetActive(false);
            }
        }

        var image = option.transform.Find("ControlImage").GetComponent<Image>();
        image.enabled = false;
        option.waitingForInput.SetActive(true);

        playerInput.DeactivateInput();
        currentOption = option;
        currentOperation = action.action.PerformInteractiveRebinding(rebindIndex).OnMatchWaitForAnother(0.1f)
            .WithControlsHavingToMatchPath("<XRController>").WithTimeout(5).OnComplete(
                _ => { CompleteRebind(option, rebindIndex); })
            .OnCancel(_ => { CompleteRebind(option, rebindIndex); });

        foreach (var exclude in DISALLOWED_BINDINGS)
        {
            currentOperation.WithControlsExcluding(exclude);
        }

        currentOperation.Start();
    }

    /// <summary>
    /// Remove all binding overrides and revert back to current scheme defaults
    /// </summary>
    public void ResetBindings()
    {
        if (currentOperation != null)
            currentOperation.Dispose();

        if (currentOption != null)
        {
            currentOption.transform.Find("ControlImage").GetComponent<Image>().enabled = true;
            currentOption.waitingForInput.SetActive(false);
        }

        playerInput.ActivateInput();
        playerInput.actions.RemoveAllBindingOverrides();
        Plugin.Config.ControllerBindingsOverride.Value = string.Empty;

        ReloadBindings();
    }

    /// <summary>
    /// Finalize the rebinding operation, and permanently update the binding configuration
    /// </summary>
    private void CompleteRebind(SettingsOption option, int rebindIndex)
    {
        var action = currentOperation.action;

        currentOperation.Dispose();
        playerInput.ActivateInput();

        Logger.LogDebug(action.bindings[rebindIndex].effectivePath);

        var image = option.transform.Find("ControlImage").GetComponent<Image>();
        image.enabled = true;
        image.sprite = AssetManager.RemappableControls.icons[action.bindings[rebindIndex].effectivePath];
        image.color = image.sprite == null ? new Color(0, 0, 0, 0) : new Color(1, 1, 1, 1);

        option.waitingForInput.SetActive(false);

        lastRebindTime = Time.realtimeSinceStartup;

        Plugin.Config.ControllerBindingsOverride.Value = playerInput.actions.SaveBindingOverridesAsJson();
    }
}