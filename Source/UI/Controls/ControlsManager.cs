using System.Collections.Generic;
using LCVR.Assets;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.UI.Controls;

public class ControlsManager : MonoBehaviour
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
    
    [SerializeField] private GameObject categoryTemplate;
    [SerializeField] private GameObject controlTemplate;
    [SerializeField] private GameObject controlWithModifierTemplate;

    [SerializeField] private Transform content;
    [SerializeField] private TextMeshProUGUI controllersText;

    private PlayerInput playerInput;
    private InputActionRebindingExtensions.RebindingOperation currentOperation;
    private ControlOption currentOption;
    private List<ControlOption> options = [];

    private float lastRebindTime;
    
    public bool IsRebinding => currentOperation != null || Time.realtimeSinceStartup - lastRebindTime < 0.5f;
    
    private void Awake()
    {
        playerInput = IngamePlayerSettings.Instance.playerInput;

        controllersText.text = string.IsNullOrEmpty(playerInput.currentControlScheme)
            ? "Please connect both your controllers"
            : $"Detected controllers: {playerInput.currentControlScheme}";
        
        // Create base game controls
        var baseCategory = CreateCategoryUI("Lethal Company");

        foreach (var control in AssetManager.RemappableControls.controls)
        {
            var option =
                Instantiate(control.controlModifier.HasValue ? controlWithModifierTemplate : controlTemplate,
                    baseCategory).GetComponent<ControlOption>();
            option.Setup(this, control);

            options.Add(option);
        }

        playerInput.onControlsChanged += OnControlsChanged;
        
        ReloadBindings();
    }

    private void OnDestroy()
    {
        playerInput.onControlsChanged -= OnControlsChanged;
    }

    /// <summary>
    /// If the current control scheme changes, reload the bindings for the new scheme
    /// </summary>
    private void OnControlsChanged(PlayerInput input)
    {
        controllersText.text = $"Detected controllers: {input.currentControlScheme}";
        
        ReloadBindings();
    }

    /// <summary>
    /// Reload the bindings for the current control scheme
    /// </summary>
    private void ReloadBindings()
    {
        foreach (var option in options)
            option.ReloadBinding();
    }

    /// <summary>
    /// Create a new control category with a specified name
    /// </summary>
    private Transform CreateCategoryUI(string label)
    {
        var go = Instantiate(categoryTemplate, content);

        go.GetComponentInChildren<TextMeshProUGUI>().text = label;
        
        return go.transform;
    }

    /// <summary>
    /// Initiate the interactive rebinding of a control binding
    /// </summary>
    public void StartRebind(ControlOption option, int bindingIndex, bool modifier)
    {
        // Prevent accidentally re-triggering the rebind process
        if (Time.realtimeSinceStartup - lastRebindTime < 0.5f)
            return;
        
        // Don't allow rebinding until a controller scheme is known]
        if (string.IsNullOrEmpty(playerInput.currentControlScheme))
            return;

        if (currentOperation != null)
        {
            currentOperation.Dispose();
            if (currentOption != null)
                currentOption.ReloadBinding();
        }
        
        option.StartRebindTimer(modifier);
        
        playerInput.DeactivateInput();
        currentOption = option;
        currentOperation = option.action.PerformInteractiveRebinding(bindingIndex).OnMatchWaitForAnother(0.1f)
            .WithControlsHavingToMatchPath("<XRController>").WithTimeout(5).OnComplete(_ => CompleteRebind(option))
            .OnCancel(_ => CompleteRebind(option));

        foreach (var exclude in DISALLOWED_BINDINGS)
            currentOperation.WithControlsExcluding(exclude);

        currentOperation.Start();
    }

    /// <summary>
    /// Save the current binding overrides to the configuration
    /// </summary>
    public void SaveBindings()
    {
        Plugin.Config.ControllerBindingsOverride.Value = playerInput.actions.SaveBindingOverridesAsJson();
    }

    /// <summary>
    /// Finalize the rebinding operation, and update the binding configuration
    /// </summary>
    private void CompleteRebind(ControlOption option)
    {
        currentOperation.Dispose();
        currentOperation = null;
        
        playerInput.ActivateInput();
        
        option.ReloadBinding();
        SaveBindings();
        
        lastRebindTime = Time.realtimeSinceStartup;
    }
}