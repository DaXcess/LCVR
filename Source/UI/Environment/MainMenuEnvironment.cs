using System.Collections;
using HarmonyLib;
using LCVR.UI.Settings;
using Microsoft.MixedReality.Toolkit.Experimental.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LCVR.UI.Environment;

public class MainMenuEnvironment : BaseMenuEnvironment
{
    [SerializeField] protected Canvas headerCanvas;
    [SerializeField] protected Canvas secondaryCanvas;
    [SerializeField] protected NonNativeKeyboard keyboard;
        
    [SerializeField] protected Animator uiAnimator;
    [SerializeField] protected TextMeshProUGUI versionLabel;
    [SerializeField] protected GameObject settingsPanel;
    [SerializeField] protected GameObject controlsPanel;

    protected SettingsManager settingsManager;
    
    protected new void Awake()
    {
        base.Awake();

        // Yucky workaround
        if (headerCanvas.GetComponentInChildren<MeshRenderer>() is { } renderer)
            renderer.material.renderQueue -= 1;
        
        versionLabel.text = $"v{Plugin.PLUGIN_VERSION}";
        
#if DEBUG
        versionLabel.text += " (DEVELOPMENT)";
#endif

        settingsManager = settingsPanel.GetComponent<SettingsManager>();
    }

    public void OpenSettingsMenu()
    {
        settingsPanel.SetActive(true);
        uiAnimator.SetBool("SecondaryVisible", true);

        StartCoroutine(_openSecondaryMenu());
    }

    public void OpenControlsMenu()
    {
        controlsPanel.SetActive(true);
        uiAnimator.SetBool("SecondaryVisible", true);

        StartCoroutine(_openSecondaryMenu());
    }

    public void CloseSecondaryMenu()
    {
        uiAnimator.SetBool("SecondaryVisible", false);
        
        StartCoroutine(_closeSecondaryMenu());
    }

    private IEnumerator _openSecondaryMenu()
    {
        // Disable all header canvas interactions
        headerCanvas.GetComponentsInChildren<Graphic>(true).Do(g => g.raycastTarget = false);
        
        // Fix yucky fucky issues with ScrollRect's and changing UI scales
        
        var rects = secondaryCanvas.GetComponentsInChildren<ScrollRect>();
        var maxTime = uiAnimator.GetCurrentAnimatorStateInfo(0).length;
        var time = 0f;

        while (time < maxTime)
        {
            time += Time.deltaTime;
            
            rects.Do(rect => rect.verticalNormalizedPosition = 1);

            yield return null;
        }
    }

    private IEnumerator _closeSecondaryMenu()
    {
        // Re-enable all header canvas interactions
        headerCanvas.GetComponentsInChildren<Graphic>(true).Do(g => g.raycastTarget = true);
        
        uiAnimator.SetBool("SecondaryVisible", false);

        yield return new WaitForSeconds(uiAnimator.GetCurrentAnimatorStateInfo(0).length);
        
        settingsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }
}