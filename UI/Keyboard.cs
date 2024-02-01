using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LCVR.UI
{
    internal class Keyboard : MonoBehaviour
    {
        private readonly Dictionary<TMP_InputField, UnityAction<string>> inputFields = [];

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
                if (inputFields.ContainsKey(input))
                    continue;

                var action = new UnityAction<string>((_) =>
                {
                    keyboard.InputField = input;
                    keyboard.PresentKeyboard();
                });

                inputFields.Add(input, action);
                input.onSelect.AddListener(action);
            }
        }

        private void OnDestroy()
        {
            foreach (var kv in inputFields)
                kv.Key.onSelect.RemoveListener(kv.Value);
        }
    }
}
