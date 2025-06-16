using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Rendering;

public class VolumeManager : MonoBehaviour
{
    private Coroutine takeDamageCoroutine;

    private Volume volume;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private Color vignetteColor = Color.black;
    private float vignetteIntensity;
    private float vignetteLerpMul = 1;

    private float saturation;

    public float VignetteIntensity
    {
        get => vignetteIntensity;
        set => vignetteIntensity = Mathf.Clamp(value, 0, 1);
    }

    public float Saturation
    {
        get => saturation;
        set => saturation = Mathf.Clamp(value, -100, 100);
    }

    private void Awake()
    {
        volume = GetComponent<Volume>();

        volume.sharedProfile.TryGet(out vignette);
        volume.sharedProfile.TryGet(out colorAdjustments);
    }

    private void Update()
    {
        vignette.color.value = Color.Lerp(vignette.color.value, vignetteColor, Time.deltaTime);
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, VignetteIntensity, Time.deltaTime * vignetteLerpMul);

        colorAdjustments.postExposure.value = Mathf.Lerp(colorAdjustments.postExposure.value, 0.2f, Time.deltaTime * 0.5f);
        colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, saturation, Time.deltaTime * 0.5f);
    }

    public void TakeDamage()
    {
        if (takeDamageCoroutine != null)
            StopCoroutine(takeDamageCoroutine);

        takeDamageCoroutine = StartCoroutine(takeDamageAnimation());
    }

    public void Muffle(float counter)
    {
        // Don't apply muffle vignette if damage taking vignette is present
        if (takeDamageCoroutine != null)
            return;

        vignetteColor = Color.black;
        vignetteIntensity = Mathf.Lerp(0, 0.65f, Mathf.Max((counter - 5) / 15, 0));
    }

    public void Death()
    {
        if (takeDamageCoroutine != null)
            StopCoroutine(takeDamageCoroutine);

        StartCoroutine(deathAnimation());
    }

    private IEnumerator takeDamageAnimation()
    {
        // Force reset intensity to zero
        vignetteColor = new Color(0.8f, 0, 0);
        vignetteLerpMul = 700;
        vignetteIntensity = 0;

        yield return null;

        vignetteLerpMul = 7;
        vignetteIntensity = 0.5f;

        yield return new WaitForSeconds(0.75f);

        vignetteLerpMul = 0.5f;
        vignetteIntensity = 0;

        yield return new WaitForSeconds(1.5f);

        vignetteLerpMul = 1;
        takeDamageCoroutine = null;
    }

    private IEnumerator deathAnimation()
    {
        yield return null;
        
        vignetteColor = Color.black;
        vignetteIntensity = 1f;
        vignetteLerpMul = 700;
        
        colorAdjustments.postExposure.value = -15f;
        saturation = -50;

        yield return new WaitForSeconds(0.5f);

        vignetteLerpMul = 0.5f;
        vignetteIntensity = 0.25f;
    }

    public static void RegisterCustomPostProcessShaders()
    {
        RegisterCustomPostProcess<Vignette>();
    }

    private static void RegisterCustomPostProcess<T>(
        CustomPostProcessInjectionPoint injectionPoint = CustomPostProcessInjectionPoint.AfterPostProcess
    ) where T : CustomPostProcessVolumeComponent
    {
        var pipeline = (HDRenderPipelineAsset)QualitySettings.renderPipeline;

        if (pipeline.globalSettings.IsCustomPostProcessRegistered(typeof(T)))
            return;

        switch (injectionPoint)
        {
            case CustomPostProcessInjectionPoint.AfterPostProcess:
                pipeline.globalSettings.afterPostProcessCustomPostProcesses.Add(typeof(T).AssemblyQualifiedName);
                break;

            case CustomPostProcessInjectionPoint.AfterPostProcessBlurs:
                pipeline.globalSettings.afterPostProcessBlursCustomPostProcesses.Add(typeof(T).AssemblyQualifiedName);
                break;

            case CustomPostProcessInjectionPoint.BeforePostProcess:
                pipeline.globalSettings.beforePostProcessCustomPostProcesses.Add(typeof(T).AssemblyQualifiedName);
                break;

            case CustomPostProcessInjectionPoint.BeforeTAA:
                pipeline.globalSettings.beforeTAACustomPostProcesses.Add(typeof(T).AssemblyQualifiedName);
                break;

            case CustomPostProcessInjectionPoint.AfterOpaqueAndSky:
            default:
                throw new ArgumentOutOfRangeException(nameof(injectionPoint), injectionPoint, null);
        }
    }
}