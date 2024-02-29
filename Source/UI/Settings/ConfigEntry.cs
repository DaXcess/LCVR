using UnityEngine;

namespace LCVR.UI.Settings;

public class ConfigEntry : MonoBehaviour
{
    private SettingsManager settingsManager;

    public string category;
    public new string name;

    void Awake()
    {
        settingsManager = GetComponentInParent<SettingsManager>();
    }

    public void UpdateValue(int value)
    {
        settingsManager.UpdateValue(category, name, value);
    }

    public void UpdateValue(float value)
    {
        settingsManager.UpdateValue(category, name, value);
    }

    public void UpdateValue(string value)
    {
        settingsManager.UpdateValue(category, name, value);
    }

    public void UpdateValue(bool value)
    {
        settingsManager.UpdateValue(category, name, value);
    }
}