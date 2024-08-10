using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI.Settings;

public class InputSlider : MonoBehaviour
{
    private TMP_InputField inputField;
    private Slider slider;

    private void Start()
    {
        inputField = GetComponentInChildren<TMP_InputField>();
        slider = GetComponentInChildren<Slider>();

        inputField.onSubmit.AddListener(OnInputFieldChanged);
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDestroy()
    {
        inputField.onSubmit.RemoveListener(OnInputFieldChanged);
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnInputFieldChanged(string value)
    {
        if (!float.TryParse(value, out var floatValue))
            return;

        var newValue = Mathf.Clamp(floatValue, slider.minValue, slider.maxValue);

        if (newValue != floatValue)
            inputField.text = newValue.ToString();

        slider.value = newValue;
    }

    private void OnSliderValueChanged(float value)
    {
        string textValue = (value >= 1000 ? Mathf.Round(value) : Mathf.Round(value * 100) / 100).ToString();

        if (inputField.text != textValue)
            inputField.text = textValue;
    }
}