using HarmonyLib;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;

namespace LCVR.UI
{
    internal class Keyboard : MonoBehaviour
    {
        private TMP_InputField[] inputFields;

        public NonNativeKeyboard keyboard;

        private void Awake()
        {
            inputFields = FindObjectsOfType<TMP_InputField>(true);

            inputFields.Do(input =>
            {
                input.onSelect.AddListener((_) =>
                {
                    keyboard.InputField = input;
                    keyboard.PresentKeyboard();
                });
            });
        }
    }
}
