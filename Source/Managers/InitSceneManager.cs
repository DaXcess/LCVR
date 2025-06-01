using System.Collections;
using LCVR.Assets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace LCVR.Managers;

public class InitSceneManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverAudio;
    [SerializeField] private AudioClip submitAudio;
    [SerializeField] private VolumeProfile sceneVolume;
    [SerializeField] private AnimationCurve fadeCurve;
    [SerializeField] private float fadeSpeed;

    private Volume defaultVolume;
    private ColorAdjustments colorAdjustments;
    private bool hasConfirmed;

    private void Awake()
    {
        defaultVolume = StaticAssets.GetRootComponent<Volume>("Default Volume");
        defaultVolume.enabled = false;

        sceneVolume.TryGet(out colorAdjustments);

        StartCoroutine(FadeScene(false));
    }

    public void HoverSfx()
    {
        audioSource.PlayOneShot(hoverAudio);
    }

    public void ConfirmOnline()
    {
        if (hasConfirmed)
            return;

        hasConfirmed = true;
            
        StartCoroutine(StartGame(false));
    }

    public void ConfirmLocal()
    {
        if (hasConfirmed)
            return;

        hasConfirmed = true;

        StartCoroutine(StartGame(true));
    }

    private IEnumerator StartGame(bool local)
    {
        audioSource.PlayOneShot(submitAudio);

        yield return new WaitForSeconds(0.5f);
        yield return FadeScene(true);

        defaultVolume.enabled = true;

        SceneManager.LoadScene(local ? "InitSceneLANMode" : "InitScene");
    }

    private IEnumerator FadeScene(bool backwards)
    {
        var time = backwards ? 1f : 0f;

        while (backwards ? time > 0 : time < 1)
        {
            colorAdjustments.postExposure.value = -10 + 10 * fadeCurve.Evaluate(time);
            time += (backwards ? -1 : 1) * Time.deltaTime * fadeSpeed;

            yield return null;
        }
    }
}