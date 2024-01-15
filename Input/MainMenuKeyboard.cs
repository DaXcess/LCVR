using HarmonyLib;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;

namespace LCVR.Input
{
    internal class MainMenuKeyboard : MonoBehaviour
    {
        private TMP_InputField[] inputFields;

        private void Awake()
        {
            inputFields = FindObjectsOfType<TMP_InputField>(true);

            inputFields.Do(input =>
            {
                input.onSelect.AddListener((_) =>
                {
                    NonNativeKeyboard.Instance.InputField = input;
                    NonNativeKeyboard.Instance.PresentKeyboard();
                });
            });
        }
    }
}
