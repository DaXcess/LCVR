using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using LCVR.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace LCVR.UI.Settings;

public class SettingsManager : MonoBehaviour
{
    [SerializeField] private GameObject categoryTemplate;
    [SerializeField] private GameObject dropdownTemplate;
    [SerializeField] private GameObject textTemplate;
    [SerializeField] private GameObject numberTemplate;
    [SerializeField] private GameObject sliderTemplate;
    [SerializeField] private GameObject booleanTeamplate;

    [SerializeField] private Transform content;

    [SerializeField] private TextMeshProUGUI versionText;

    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TMP_Dropdown runtimesDropdown;

    private readonly List<string> disabledCategories = ["internal"];
    private bool isInitializing = true;

    private void Awake()
    {
    }

    private void Start()
    {
        isInitializing = true;

        // Update version text
        versionText.text = $"LCVR v{Plugin.PLUGIN_VERSION}";

#if DEBUG
        versionText.text += " (DEVELOPMENT)";
#endif

        // Set up OpenXR settings section
        if (OpenXR.GetRuntimes() is var runtimes && runtimes.Count == 0)
            runtimesDropdown.AddOptions(["System Default"]);
        else
        {
            var selectedIndex = 0;

            if (!string.IsNullOrEmpty(Plugin.Config.OpenXRRuntimeFile.Value))
                for (var i = 0; i < runtimes.Count; i++)
                    if (runtimes.ElementAt(i).Path == Plugin.Config.OpenXRRuntimeFile.Value)
                    {
                        selectedIndex = i + 1;
                        break;
                    }

            runtimesDropdown.AddOptions(["System Default", .. runtimes.Select(rt => rt.Name)]);
            runtimesDropdown.value = selectedIndex;
        }

        // Dynamically add sections for other settings

        var categories = new Dictionary<string, List<KeyValuePair<ConfigDefinition, ConfigEntryBase>>>();

        foreach (var entry in Plugin.Config.File)
        {
            // Skip disabled categories
            if (disabledCategories.Contains(entry.Key.Section.ToLowerInvariant()))
                continue;

            if (!categories.TryGetValue(entry.Key.Section, out var list))
            {
                list = [];
                categories.Add(entry.Key.Section, list);
            }

            list.Add(entry);
        }

        foreach (var (category, settings) in categories)
        {
            var categoryObject = Instantiate(categoryTemplate, content);
            categoryObject.GetComponentInChildren<TextMeshProUGUI>().text = category;

            foreach (var (definition, config) in settings)
            {
                if (config.SettingType.IsEnum)
                {
                    var dropdownUI = Instantiate(dropdownTemplate, categoryObject.transform);
                    var title = dropdownUI.GetComponentInChildren<TextMeshProUGUI>();
                    var dropdown = dropdownUI.GetComponentInChildren<TMP_Dropdown>();
                    var entry = dropdownUI.GetComponentInChildren<ConfigEntry>();
                    var description = dropdownUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(definition.Key);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = definition.Key;

                    var names = Enum.GetNames(config.SettingType);
                    var idx = Array.FindIndex(names, name => name == config.BoxedValue.ToString());

                    dropdown.ClearOptions();
                    dropdown.AddOptions([.. names]);
                    dropdown.SetValueWithoutNotify(idx);
                }
                else if (config.SettingType == typeof(float) &&
                         config.Description.AcceptableValues is AcceptableValueRange<float> values)
                {
                    var sliderOption = Instantiate(sliderTemplate, categoryObject.transform);
                    var title = sliderOption.GetComponentInChildren<TextMeshProUGUI>();
                    var slider = sliderOption.GetComponentInChildren<Slider>();
                    var input = sliderOption.GetComponentInChildren<TMP_InputField>();
                    var entry = sliderOption.GetComponentInChildren<ConfigEntry>();
                    var description = sliderOption.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(definition.Key);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = definition.Key;

                    slider.maxValue = values.MaxValue;
                    slider.minValue = values.MinValue;
                    slider.SetValueWithoutNotify((float)config.BoxedValue);
                    input.SetTextWithoutNotify(config.BoxedValue.ToString());
                }
                else if (config.SettingType == typeof(int) || config.SettingType == typeof(float))
                {
                    var numberUI = Instantiate(numberTemplate, categoryObject.transform);
                    var title = numberUI.GetComponentInChildren<TextMeshProUGUI>();
                    var input = numberUI.GetComponentInChildren<TMP_InputField>();
                    var entry = numberUI.GetComponentInChildren<ConfigEntry>();
                    var description = numberUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(definition.Key);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = definition.Key;

                    input.SetTextWithoutNotify(config.BoxedValue.ToString());
                }
                else if (config.SettingType == typeof(bool))
                {
                    var toggleUI = Instantiate(booleanTeamplate, categoryObject.transform);
                    var title = toggleUI.GetComponentInChildren<TextMeshProUGUI>();
                    var toggle = toggleUI.GetComponentInChildren<Toggle>();
                    var entry = toggleUI.GetComponentInChildren<ConfigEntry>();
                    var description = toggleUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(definition.Key);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = definition.Key;

                    toggle.SetIsOnWithoutNotify((bool)config.BoxedValue);
                }
                else if (config.SettingType == typeof(string))
                {
                    var textUI = Instantiate(textTemplate, categoryObject.transform);
                    var title = textUI.GetComponentInChildren<TextMeshProUGUI>();
                    var input = textUI.GetComponentInChildren<TMP_InputField>();
                    var entry = textUI.GetComponentInChildren<ConfigEntry>();
                    var description = textUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(definition.Key);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = definition.Key;

                    input.SetTextWithoutNotify((string)config.BoxedValue);
                }
            }
        }

        isInitializing = false;
    }

    public void DisableCategory(string categoryName)
    {
        disabledCategories.Add(categoryName.ToLowerInvariant());
    }

    public void SetOpenXRRuntime(int index)
    {
        if (index == 0)
        {
            Plugin.Config.OpenXRRuntimeFile.Value = "";
            return;
        }

        var runtimeName = runtimesDropdown.options[index].text;
        var runtimes = OpenXR.GetRuntimes();

        if (!runtimes.TryGetRuntime(runtimeName, out var runtime))
        {
            // menuManager.DisplayMenuNotification("Failed to update OpenXR runtime", "[ Close ]");
            return;
        }

        Plugin.Config.OpenXRRuntimeFile.Value = runtime.Path;
    }

    public void UpdateValue(string section, string key, object value)
    {
        // Ignore updates when populating initial values
        if (isInitializing)
            return;

        // PlayChangeSfx();

        Logger.LogDebug($"Updating setting: [{section}] {key} = {value}");

        var entry = Plugin.Config.File[section, key];
        if (entry is not null)
            entry.BoxedValue = value switch
            {
                string str when entry.SettingType == typeof(int) => int.Parse(str),
                string str when entry.SettingType == typeof(float) => float.Parse(str),
                int num when entry.SettingType.IsEnum => Enum.ToObject(entry.SettingType, num),
                _ => value
            };
    }

    public void UpdateDescription(string title, string description)
    {
        descriptionText.text = $"<b>{title}</b>\n\n{description}";
    }

    /// <summary>
    /// This function gets called when the player closes the settings menu
    /// </summary>
    public void ConfirmSettings()
    {
        if (!VRSession.InVR)
            return;

        #region Reload and apply HDRP pipeline and input settings

        var asset = QualitySettings.renderPipeline as HDRenderPipelineAsset;

        if (!asset)
        {
            Logger.LogError("Failed to apply render pipeline changes: Render pipeline is null??");
            return;
        }

        var settings = asset.currentPlatformRenderPipelineSettings;

        settings.dynamicResolutionSettings.enabled = Plugin.Config.EnableDynamicResolution.Value;
        settings.dynamicResolutionSettings.dynResType = DynamicResolutionType.Hardware;
        settings.dynamicResolutionSettings.upsampleFilter = Plugin.Config.DynamicResolutionUpscaleFilter.Value;
        settings.dynamicResolutionSettings.minPercentage = settings.dynamicResolutionSettings.maxPercentage =
            Plugin.Config.DynamicResolutionPercentage.Value;
        settings.supportMotionVectors = true;

        settings.xrSettings.occlusionMesh = Plugin.Config.EnableOcclusionMesh.Value;
        settings.xrSettings.singlePass = false;

        settings.lodBias =
            new FloatScalableSetting(
                [Plugin.Config.LODBias.Value, Plugin.Config.LODBias.Value, Plugin.Config.LODBias.Value],
                ScalableSettingSchemaId.With3Levels);

        asset.currentPlatformRenderPipelineSettings = settings;

        InputSystem.settings.defaultButtonPressPoint = Plugin.Config.ButtonPressPoint.Value;

        #endregion
    }
}