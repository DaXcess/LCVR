using System.Collections;
using UnityEngine;

namespace LCVR.UI;

public class Enemy : MonoBehaviour
{   
    private static readonly int SpeedMultiplier = Animator.StringToHash("speedMultiplier");
    private static readonly int Sneak = Animator.StringToHash("sneak");
    private static readonly int VelocityZ = Animator.StringToHash("VelocityZ");

    private Camera camera;
    private Animator animator;
    private AudioSource source;
    
    private uint state;
    private float speed;

    private void Awake()
    {
        camera = GameObject.Find("UICamera").GetComponent<Camera>();
        animator = GetComponentInChildren<Animator>();
        source = GetComponent<AudioSource>();
        
        animator.SetFloat(SpeedMultiplier, 1f);
    }

    private void Update()
    {
        if (!camera.isActiveAndEnabled)
        {
            animator.SetBool(Sneak, true);
            animator.SetFloat(VelocityZ, 0);
            speed = 0;
            state = 0;
            source.volume = 1;

            return;
        }
        
        if (HasLineOfSight() && state == 0)
        {
            state = 1;
            animator.SetBool(Sneak, false);
            StartCoroutine(walkBackwards());
        }

        if (state == 1)
        {
            transform.position -= speed * transform.forward;
            source.volume -= speed * 0.03f;
        }
    }

    private IEnumerator walkBackwards()
    {
        yield return new WaitForSeconds(1.5f);

        while (1 - speed > 0.01f && camera.isActiveAndEnabled)
        {
            speed = Mathf.Lerp(speed, 0.1f, Time.deltaTime);
            animator.SetFloat(VelocityZ, -speed * 100);
            yield return null;
        }
    }

    private bool HasLineOfSight()
    {
        return Vector3.Angle(camera.transform.forward, transform.position - camera.transform.position) < 40f;
    }
}
