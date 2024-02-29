﻿using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI.Settings;

public class SettingsManager : MonoBehaviour
{
    private MenuManager menuManager;

    [Header("Prefabs")]

    [SerializeField] private GameObject categoryTemplate;
    [SerializeField] private GameObject dropdownTemplate;
    [SerializeField] private GameObject textTemplate;
    [SerializeField] private GameObject numberTemplate;
    [SerializeField] private GameObject sliderTemplate;
    [SerializeField] private GameObject booleanTeamplate;

    [SerializeField] private Transform content;

    [Header("Specific Fields")]

    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TMP_Dropdown runtimesDropdown;

    private bool isInitializing = true;

    void Awake()
    {
        menuManager = FindObjectOfType<MenuManager>();
    }

    void Start()
    {
        isInitializing = true;

        // Update version text
        versionText.text = $"LCVR v{Plugin.PLUGIN_VERSION}";

#if DEBUG
        versionText.text += " (DEVELOPMENT)";
#endif

        // Set up OpenXR settings section
        var runtimes = OpenXR.DetectOpenXRRuntimes(out _);
        var selectedIndex = 0;

        if (!string.IsNullOrEmpty(Plugin.Config.OpenXRRuntimeFile.Value))
            for (var i = 0; i < runtimes.Count; i++)
                if (runtimes.ElementAt(i).Value == Plugin.Config.OpenXRRuntimeFile.Value)
                {
                    selectedIndex = i + 1;
                    break;
                }            

        runtimesDropdown.AddOptions([$"System Default", .. runtimes.Keys]);
        runtimesDropdown.value = selectedIndex;

        // Dynamically add sections for other settings

        var categories = new Dictionary<string, List<KeyValuePair<ConfigDefinition, ConfigEntryBase>>>();

        foreach (var entry in Plugin.Config.File)
        {
            // Skip internal
            if (entry.Key.Section.ToLower() == "internal")
                continue;

            if (!categories.TryGetValue(entry.Key.Section, out var list))
            {
                list = [];
                categories.Add(entry.Key.Section, list);
            }

            list.Add(entry);
        }

        foreach ((var category, var settings) in categories)
        {
            var categoryObject = Instantiate(categoryTemplate, content);
            categoryObject.GetComponentInChildren<TextMeshProUGUI>().text = category;

            foreach (var setting in settings)
            {
                var name = setting.Key.Key;
                var config = setting.Value;

                if (config.SettingType.IsEnum)
                {
                    var dropdownUI = Instantiate(dropdownTemplate, categoryObject.transform);
                    var title = dropdownUI.GetComponentInChildren<TextMeshProUGUI>();
                    var dropdown = dropdownUI.GetComponentInChildren<TMP_Dropdown>();
                    var entry = dropdownUI.GetComponentInChildren<ConfigEntry>();
                    var description = dropdownUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(name);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = name;

                    var names = Enum.GetNames(config.SettingType);
                    var idx = Array.FindIndex(names, (name) => name == config.BoxedValue.ToString());

                    dropdown.ClearOptions();
                    dropdown.AddOptions([.. names]);
                    dropdown.SetValueWithoutNotify(idx);
                }
                else if (config.SettingType == typeof(float) && config.Description.AcceptableValues is AcceptableValueRange<float>)
                {
                    var sliderOption = Instantiate(sliderTemplate, categoryObject.transform);
                    var title = sliderOption.GetComponentInChildren<TextMeshProUGUI>();
                    var slider = sliderOption.GetComponentInChildren<Slider>();
                    var input = sliderOption.GetComponentInChildren<TMP_InputField>();
                    var entry = sliderOption.GetComponentInChildren<ConfigEntry>();
                    var description = sliderOption.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(name);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = name;

                    var values = config.Description.AcceptableValues as AcceptableValueRange<float>;
                    
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

                    description.title = title.text = Utils.FormatPascalAndAcronym(name);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = name;

                    input.SetTextWithoutNotify(config.BoxedValue.ToString());
                }
                else if (config.SettingType == typeof(bool))
                {
                    var toggleUI = Instantiate(booleanTeamplate, categoryObject.transform);
                    var title = toggleUI.GetComponentInChildren<TextMeshProUGUI>();
                    var toggle = toggleUI.GetComponentInChildren<Toggle>();
                    var entry = toggleUI.GetComponentInChildren<ConfigEntry>();
                    var description = toggleUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(name);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = name;

                    toggle.SetIsOnWithoutNotify((bool)config.BoxedValue);
                } else if (config.SettingType == typeof(string))
                {
                    var textUI = Instantiate(textTemplate, categoryObject.transform);
                    var title = textUI.GetComponentInChildren<TextMeshProUGUI>();
                    var input = textUI.GetComponentInChildren<TMP_InputField>();
                    var entry = textUI.GetComponentInChildren<ConfigEntry>();
                    var description = textUI.GetComponent<ConfigDescription>();

                    description.title = title.text = Utils.FormatPascalAndAcronym(name);
                    description.description = config.Description.Description;

                    entry.category = category;
                    entry.name = name;

                    input.SetTextWithoutNotify((string)config.BoxedValue);
                }
            }
        }

        isInitializing = false;
    }

    public void PlayButtonPressSFX()
    {
        menuManager.PlayConfirmSFX();
    }

    public void PlayCancelSFX()
    {
        menuManager.PlayCancelSFX();
    }

    public void PlayHoverSFX()
    {
        menuManager.MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonSelectSFX);
    }

    public void PlayChangeSFX()
    {
        menuManager.MenuAudio.PlayOneShot(GameNetworkManager.Instance.buttonTuneSFX);
    }

    public void SetOpenXRRuntime(int index)
    {
        if (index == 0)
        {
            Plugin.Config.OpenXRRuntimeFile.Value = "";
            return;
        }

        var name = runtimesDropdown.options[index].text;
        var runtimes = OpenXR.DetectOpenXRRuntimes(out _);

        if (!runtimes.TryGetValue(name, out var runtimeFile))
        {
            menuManager.DisplayMenuNotification("Failed to update OpenXR runtime", "[ Close ]");
            return;
        }

        Plugin.Config.OpenXRRuntimeFile.Value = runtimeFile;
    }

    public void UpdateValue(string category, string name, object value)
    {
        // Ignore updates when populating initial values
        if (isInitializing)
            return;

        PlayChangeSFX();
        
        Logger.LogDebug($"Updating setting: [{category}] {name} = {value}");

        var entry = Plugin.Config.File[category, name];
        if (entry is not null)
            entry.BoxedValue = value;
    }

    public void UpdateDescription(string title, string description)
    {
        descriptionText.text = $"<b>{title}</b>\n\n{description}";
    }
}