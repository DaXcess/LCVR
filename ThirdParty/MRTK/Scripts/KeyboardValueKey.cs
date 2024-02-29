// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI;

/// <summary>
/// Represents a key on the keyboard that has a string value for input.
/// </summary>
[RequireComponent(typeof(Button))]
public class KeyboardValueKey : MonoBehaviour
{
    private NonNativeKeyboard keyboard;

    /// <summary>
    /// The default string value for this key.
    /// </summary>
    //[Experimental]
    public string Value;

    /// <summary>
    /// The shifted string value for this key.
    /// </summary>
    public string ShiftValue;

    /// <summary>
    /// Reference to child text element.
    /// </summary>
    private TextMeshProUGUI m_Text;

    /// <summary>
    /// Reference to the GameObject's button component.
    /// </summary>
    private Button m_Button;

    /// <summary>
    /// Get the button component.
    /// </summary>
    private void Awake()
    {
        if (keyboard == null)
            keyboard = GetComponentInParent<NonNativeKeyboard>();

        m_Button = GetComponent<Button>();
    }

    /// <summary>
    /// Initialize key text, subscribe to the onClick event, and subscribe to keyboard shift event.
    /// </summary>
    private void Start()
    {
        m_Text = gameObject.GetComponentInChildren<TextMeshProUGUI>();
        m_Text.text = Value;

        m_Button.onClick.RemoveAllListeners();
        m_Button.onClick.AddListener(FireAppendValue);

        keyboard.OnKeyboardShifted += Shift;
    }

    /// <summary>
    /// Method injected into the button's onClick listener.
    /// </summary>
    private void FireAppendValue()
    {
        keyboard.AppendValue(this);
    }

    /// <summary>
    /// Called by the Keyboard when the shift key is pressed. Updates the text for this key using the Value and ShiftValue fields.
    /// </summary>
    /// <param name="isShifted">Indicates the state of shift, the key needs to be changed to.</param>
    public void Shift(bool isShifted)
    {
        // Shift value should only be applied if a shift value is present.
        if (isShifted && !string.IsNullOrEmpty(ShiftValue))
        {
            m_Text.text = ShiftValue;
        }
        else
        {
            m_Text.text = Value;
        }
    }
}
