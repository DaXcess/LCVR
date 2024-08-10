using System.Collections;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LCVR.UI;

public class MenuCar : MonoBehaviour
{
    private const float MAX_LIGHT_BRIGHTNESS = 400f;
    
    [SerializeField] private Light[] lights;
    [SerializeField] private AudioSource hornAudio;
    [SerializeField] private AudioSource skidAudio;
    
    private Camera camera;

    private bool isHonkingHorn;
    private float timeLookedAt;
    private bool hasTriggered;
    private Coroutine currentRoutine;
    
    private void Awake()
    {
        camera = GameObject.Find("UICamera").GetComponent<Camera>();

        lights.Do(light => light.hideFlags = HideFlags.HideAndDontSave);
    }

    private void Update()
    {
        if (!camera.isActiveAndEnabled)
        {
            hasTriggered = false;
            isHonkingHorn = false;
            skidAudio.Stop();
            
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);
        }

        var los = HasLineOfSight();

        if (camera.isActiveAndEnabled && los && !hasTriggered)
        {
            timeLookedAt += Time.deltaTime;

            if (timeLookedAt > 0.333f)
            {
                hasTriggered = true;

                // 20% chance to drive towards player
                currentRoutine = StartCoroutine(Random.Range(0, 100) < 20 ? DriveTowardsPlayer() : HonkHorn());
            }
        }

        if (!los)
            timeLookedAt = 0;
        
        if (isHonkingHorn)
        {
            if (!hornAudio.isPlaying)
            {
                hornAudio.Play();
                hornAudio.pitch = 1;
            }
            
            lights.Do(light =>
                light.intensity = Mathf.Min(light.intensity + Time.deltaTime * 1500f, MAX_LIGHT_BRIGHTNESS));
        }
        else
        {
            hornAudio.pitch = Mathf.Max(hornAudio.pitch - Time.deltaTime * 6f, 0.01f);
            if (hornAudio.pitch < 0.02f)
                hornAudio.Stop();

            lights.Do(light => light.intensity = Mathf.Max(light.intensity - Time.deltaTime * 1500f, 0));
        }
    }

    private IEnumerator HonkHorn()
    {
        isHonkingHorn = true;

        yield return new WaitForSeconds(2);
        
        isHonkingHorn = false;
    }

    private IEnumerator DriveTowardsPlayer()
    {
        isHonkingHorn = true;
        transform.position += transform.forward * -100;

        yield return new WaitForSeconds(0.5f);
        
        skidAudio.Play();

        const float speed = 50f;
        var distanceDriven = 0f;
        
        while (distanceDriven < 100 && hasTriggered)
        {
            if (distanceDriven > 80)
            {
                isHonkingHorn = false;
                skidAudio.Stop();
            }

            var movement = transform.forward * (speed * Time.deltaTime);
            transform.position += movement;

            distanceDriven += movement.magnitude;
            
            yield return null;
        }
    }

    private bool HasLineOfSight()
    {
        return Vector3.Angle(camera.transform.forward, transform.position - camera.transform.position) < 40f;
    }
}
