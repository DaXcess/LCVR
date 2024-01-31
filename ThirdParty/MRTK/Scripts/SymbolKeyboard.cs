// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Experimental.UI
{
    /// <summary>
    /// This class switches back and forth between two symbol boards that otherwise do not fit on the keyboard entirely
    /// </summary>
    public class SymbolKeyboard : MonoBehaviour
    {
        private NonNativeKeyboard keyboard;

        [SerializeField]
        private UnityEngine.UI.Button m_PageBck = null;

        [SerializeField]
        private UnityEngine.UI.Button m_PageFwd = null;

        private void Awake()
        {
            if (keyboard == null)
                keyboard = GetComponentInParent<NonNativeKeyboard>();
        }

        private void Update()
        {
            // Visual reflection of state.
            m_PageBck.interactable = keyboard.IsShifted;
            m_PageFwd.interactable = !keyboard.IsShifted;
        }
    }
}
