﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;
using UnityEngine.UI;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI;

/// <summary>
/// Updates the visual state of the text based on the buttons state
/// </summary>
public class SymbolDisableHighlight : MonoBehaviour
{
    /// <summary>
    /// The text field to update.
    /// </summary>
    //[Experimental]
    [SerializeField]
    private Text m_TextField = null;

    /// <summary>
    /// The text field to update.
    /// </summary>
    [SerializeField]
    private Image m_ImageField = null;

    /// <summary>
    /// The color to switch to when the button is disabled.
    /// </summary>
    [SerializeField]
    private Color m_DisabledColor = Color.grey;

    /// <summary>
    /// The color the text field starts as.
    /// </summary>
    private Color m_StartingColor = Color.white;

    /// <summary>
    /// The button to check for disabled/enabled.
    /// </summary>
    private Button m_Button = null;

    /// <summary>
    /// Standard Unity start.
    /// </summary>
    private void Start()
    {
        if (m_TextField != null)
        {
            m_StartingColor = m_TextField.color;
        }

        if (m_ImageField != null)
        {
            m_StartingColor = m_ImageField.color;
        }

        m_Button = GetComponentInParent<Button>();

        UpdateState();
    }

    /// <summary>
    /// Standard Unity update.
    /// </summary>
    private void Update()
    {
        UpdateState();
    }

    /// <summary>
    /// Updates the visual state of the text based on the buttons state.
    /// </summary>
    private void UpdateState()
    {
        if (m_TextField != null && m_Button != null)
        {
            m_TextField.color = m_Button.interactable ? m_StartingColor : m_DisabledColor;
        }

        if (m_ImageField != null && m_Button != null)
        {
            m_ImageField.color = m_Button.interactable ? m_StartingColor : m_DisabledColor;
        }
    }
}
