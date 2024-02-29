using UnityEngine;
using UnityEngine.EventSystems;

namespace LCVR.UI.Settings;

public class ConfigDescription : MonoBehaviour, IPointerEnterHandler
{
    public string title = "";
    public string description = "";

    private SettingsManager settingsManager;

    void Start()
    {
        settingsManager = GetComponentInParent<SettingsManager>();
    }

    public void OnPointerEnter(PointerEventData _)
    {
        settingsManager.UpdateDescription(title, description);
    }
}
