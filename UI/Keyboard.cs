using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace LCVR.UI
{
    internal class Keyboard : MonoBehaviour
    {
        private readonly List<TMP_InputField> inputFields = [];

        public NonNativeKeyboard keyboard;

        private void Awake()
        {
            StartCoroutine(PopulateInputsRoutine());
        }

        private IEnumerator PopulateInputsRoutine()
        {
            while (true)
            {
                PopulateInputs();

                yield return new WaitForSeconds(0.5f);
            }
        }

        private void PopulateInputs()
        {
            var inputs = FindObjectsOfType<TMP_InputField>(true);

            foreach (var input in inputs)
            {
                if (inputFields.Contains(input))
                    continue;

                input.onSelect.AddListener((_) =>
                {
                    keyboard.InputField = input;
                    keyboard.PresentKeyboard();
                });
            }
        }
    }
}
