using System.Collections;
using LCVR.Assets;
using LCVR.Input;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LCVR.UI.Controls;

public class ControlOption : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI controlName;
    [SerializeField] private TextMeshProUGUI controlText;
    [SerializeField] private TextMeshProUGUI modifierText;
    [SerializeField] private Image controlSprite;
    [SerializeField] private Image modifierSprite;

    public InputAction action => control.currentInput.action;

    private PlayerInput playerInput;
    private ControlsManager manager;
    private RemappableControl control;

    private Coroutine rebindTimerCoroutine;
    private int bindingIndex;
    private int? modifierBindingIndex;

    private void Awake()
    {
        playerInput = IngamePlayerSettings.Instance.playerInput;
    }

    public void StartRebind(bool modifier)
    {
        if (modifier && modifierBindingIndex is { } modifierIndex)
            manager.StartRebind(this, modifierIndex, true);
        else
            manager.StartRebind(this, bindingIndex, false);
    }

    public void ResetBinding()
    {
        action.RemoveBindingOverride(bindingIndex);

        if (modifierBindingIndex is { } modifierIndex)
            action.RemoveBindingOverride(modifierIndex);

        ReloadBinding();
        manager.SaveBindings();
    }

    public void ClearBinding()
    {
        action.ApplyBindingOverride(bindingIndex, "");

        if (modifierBindingIndex is { } modifierIndex)
            action.ApplyBindingOverride(modifierIndex, "");

        ReloadBinding();
        manager.SaveBindings();
    }

    public void Setup(ControlsManager controlsManager, RemappableControl remappableControl)
    {
        manager = controlsManager;
        control = remappableControl;

        controlName.text = control.controlName;
    }

    public void ReloadBinding()
    {
        if (rebindTimerCoroutine != null)
            StopCoroutine(rebindTimerCoroutine);

        if (control.controlModifier is { } modifierIndex)
            ReloadModifierBinding(modifierIndex);

        bindingIndex = Mathf.Max(control.bindingIndex, 0) +
                       Mathf.Max(control.currentInput.action.GetBindingIndex(playerInput.currentControlScheme), 0);

        if (string.IsNullOrEmpty(playerInput.currentControlScheme))
        {
            controlSprite.enabled = false;
            controlText.text = ".";

            return;
        }

        var binding = control.currentInput.action.bindings[bindingIndex].effectivePath;

        if (string.IsNullOrEmpty(binding))
        {
            controlSprite.enabled = false;
            controlText.text = ".";

            return;
        }

        controlText.text = "";
        controlSprite.enabled = true;
        controlSprite.sprite = AssetManager.RemappableControls.icons[binding];
    }

    private void ReloadModifierBinding(int modifierIndex)
    {
        // Store in variable because Rider likes complaining when using the obviously non-null var thinking it might be null
        var index = Mathf.Max(modifierIndex, 0) +
                    Mathf.Max(
                        control.currentInput.action.GetBindingIndex(playerInput.currentControlScheme),
                        0);
        modifierBindingIndex = index;

        if (string.IsNullOrEmpty(playerInput.currentControlScheme))
        {
            modifierSprite.enabled = false;
            modifierText.text = ".";

            return;
        }

        var binding = control.currentInput.action.bindings[index].effectivePath;

        if (string.IsNullOrEmpty(binding))
        {
            modifierSprite.enabled = false;
            modifierText.text = ".";

            return;
        }

        modifierText.text = "";
        modifierSprite.enabled = true;
        modifierSprite.sprite = AssetManager.RemappableControls.icons[binding];
    }

    public void StartRebindTimer(bool modifier)
    {
        rebindTimerCoroutine = StartCoroutine(RebindTimer(modifier));
    }

    private IEnumerator RebindTimer(bool modifier)
    {
        var targetSprite = modifier ? modifierSprite : controlSprite;
        var targetText = modifier ? modifierText : controlText;

        targetSprite.enabled = false;

        for (var i = 5; i >= 0; i--)
        {
            targetText.text = $".{i}.";

            yield return new WaitForSeconds(0.5f);

            targetText.text = $". {i} .";

            yield return new WaitForSeconds(0.5f);
        }
    }
}