using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LCVR.UI;

public class CaveDwellerScript : MonoBehaviour
{
    [SerializeField] private GameObject babyContainer;
    [SerializeField] private AudioSource cryingAudio;
    [SerializeField] private ParticleSystem tearsParticle;
    [SerializeField] private Animator babyCreatureAnimator;

    [SerializeField] private GameObject adultContainer;

    [SerializeField] private AudioSource creatureSFX;
    [SerializeField] private AudioClip transformationSFX;

    private bool isReset;
    private bool isAdult;
    private bool isSitting;
    private bool babyCrying;
    
    private Camera camera;
    private float losTime;
    private float brokenLosTime;

    private LineOfSightAction losAction;
    private LineOfSightAction longLosAction;

    private bool hasTriggered;
    private bool hasLongTriggered;
    
    private void Awake()
    {
        camera = GameObject.Find("UICamera").GetComponent<Camera>();
        
        GetComponentsInChildren<Light>().Do(light => light.hideFlags = HideFlags.HideAndDontSave);
        ResetEnemy();
    }

    private void ResetEnemy()
    {
        SetCrying(false);

        isReset = true;
        isAdult = false;
        hasTriggered = false;
        hasLongTriggered = false;
        losTime = 0;
        brokenLosTime = 0;

        adultContainer.SetActive(false);
        babyContainer.SetActive(true);

        babyCreatureAnimator.SetBool("Transform", false);

        isSitting = Random.Range(0, 100) > 70;
        losAction = (LineOfSightAction)Random.Range(0, Enum.GetValues(typeof(LineOfSightAction)).Length);
        longLosAction =
            (LineOfSightAction)Random.Range((int)losAction, Enum.GetValues(typeof(LineOfSightAction)).Length - 1) + 1;
    }

    private void Update()
    {
        if (!isAdult)
            BabyUpdate();
        
        if (!camera.isActiveAndEnabled)
        {
            if (!isReset)
                ResetEnemy();

            return;
        }

        var los = HasLineOfSight();

        if (los)
        {
            isReset = false;
            
            brokenLosTime = 0;
            losTime += Time.deltaTime;

            if (!hasTriggered && losTime > 0.5f)
            {
                hasTriggered = true;
                OnLookedAt(false);
            }

            if (losTime > 4 && !hasLongTriggered)
            {
                hasLongTriggered = true;
                OnLookedAt(true);
            }
        }
        else if (!isReset)
        {
            brokenLosTime += Time.deltaTime;
            losTime = 0;
            
            if (brokenLosTime > 2.5f)
                ResetEnemy();
        }
    }

    private void OnLookedAt(bool staredDown)
    {
        var action = staredDown ? longLosAction : losAction;

        switch (action)
        {
            case LineOfSightAction.Nothing:
                break;
            
            case LineOfSightAction.Sit:
                isSitting = true;
                break;
            
            case LineOfSightAction.Cry:
                SetCrying(true);
                break;
            
            case LineOfSightAction.Transform:
                TransformIntoAdult();
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void BabyUpdate()
    {
        babyCreatureAnimator.SetBool("BabyCrying", babyCrying);
        babyCreatureAnimator.SetBool("Sitting", babyCrying || isSitting);

        if (babyCrying)
        {
            cryingAudio.pitch = Mathf.Lerp(cryingAudio.pitch, 1, 2 * Time.deltaTime);
            cryingAudio.volume = Mathf.Lerp(cryingAudio.volume, 1, 2 * Time.deltaTime);
        }
        else
        {
            cryingAudio.pitch = Mathf.Lerp(cryingAudio.pitch, 0.85f, 3 * Time.deltaTime);
            cryingAudio.volume = Mathf.Lerp(cryingAudio.volume, 0, 3.6f * Time.deltaTime);

            if (cryingAudio.volume <= 0.07f && cryingAudio.isPlaying)
                cryingAudio.Stop();
        }
    }

    private void SetCrying(bool setCrying)
    {
        if (!babyCrying && setCrying)
        {
            babyCrying = true;
            cryingAudio.Play();
            tearsParticle.Play();
        }

        if (!setCrying && babyCrying)
        {
            babyCrying = false;
            tearsParticle.Stop();
        }
    }
    
    private void TransformIntoAdult()
    {
        StartCoroutine(TransformRoutine());

        return;

        IEnumerator TransformRoutine()
        {
            SetCrying(false);

            babyCreatureAnimator.SetBool("Transform", true);
            creatureSFX.PlayOneShot(transformationSFX);

            yield return new WaitForSeconds(0.5f);

            babyContainer.SetActive(false);
            adultContainer.SetActive(true);
        }
    }
    
    private bool HasLineOfSight()
    {
        return Vector3.Angle(camera.transform.forward, transform.position - camera.transform.position) < 40f;
    }

    private enum LineOfSightAction
    {
        Nothing,
        Sit,
        Cry,
        Transform
    }
}
