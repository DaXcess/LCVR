using UnityEngine;
using Random = UnityEngine.Random;

namespace LCVR.UI;

public class EnemyAudio : MonoBehaviour
{
    private AudioSource source;
    
    [SerializeField] private AudioClip foundClip;
    [SerializeField] private AudioClip[] rustleClips;
    [SerializeField] private AudioClip[] stepClips;

    private void Awake()
    {
        source = GetComponentInParent<AudioSource>();
    }

    public void PlayFound()
    {
        source.PlayOneShot(foundClip);
    }

    public void PlayRustle()
    {
        var num = Random.Range(0, rustleClips.Length);
        if (rustleClips[num] == null)
            return;
        
        source.PlayOneShot(rustleClips[num]);
    }

    public void PlayStep()
    {
        var num = Random.Range(0, stepClips.Length);
        if (stepClips[num] == null)
            return;
        
        source.PlayOneShot(stepClips[num]);
    }
}