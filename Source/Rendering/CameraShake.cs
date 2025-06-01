using UnityEngine;
using Random = UnityEngine.Random;

namespace LCVR.Rendering;

public class CameraShake : MonoBehaviour
{
    private const float Range = 0.75f;
    
    private float shakeIntensity;
    private float shakeDuration;
    private float initialDuration;

    private void Update()
    {
        transform.localRotation = Quaternion.identity;
        
        if (shakeDuration == 0)
            return;

        var factor = shakeDuration / initialDuration;
        var position = new Vector3(Random.Range(-Range, Range) * shakeIntensity * factor,
            Random.Range(-Range, Range) * shakeIntensity * factor, 0);

        transform.localPosition = position;

        shakeDuration -= Time.deltaTime;

        if (shakeDuration <= 0) {
            shakeDuration = 0;
            transform.localPosition = Vector3.zero;
        }
    }

    private void StartShake(float duration, float intensity)
    {
        shakeDuration = initialDuration = duration;
        shakeIntensity = intensity;
    }

    public void ShakeCamera(ScreenShakeType shakeType)
    {
        if (Plugin.Config.DisableCameraShake.Value)
            return;
        
        switch (shakeType)
        {
            case ScreenShakeType.Small:
                StartShake(0.22f, 0.04f);
                break;
            
            case ScreenShakeType.Big:
                StartShake(0.6f, 0.08f);
                break;
            
            case ScreenShakeType.Long:
                StartShake(1.4f, 0.06f);
                break;
            
            case ScreenShakeType.VeryStrong:
                StartShake(1.47f, 0.09f);
                break;
        }
    }
}