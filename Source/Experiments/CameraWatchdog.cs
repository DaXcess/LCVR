using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace LCVR.Experiments;

/// <summary>
/// This behaviour attempts to detect when the XR rendering is failing due to no active XR cameras being present in
/// the scene. If this is the case, it will log warnings into the console.
/// </summary>
public class CameraWatchdog : MonoBehaviour
{
    private static Camera ActiveCamera => Camera.allCameras.FirstOrDefault(cam => cam.targetTexture == null);

    private int cameraMisses;
    
    private void Awake()
    {
        StartCoroutine(checkForActiveCameraLoop());
    }

    private IEnumerator checkForActiveCameraLoop()
    {
        while (true)
        {
            var activeCamera = ActiveCamera;
            cameraMisses = Mathf.Clamp(cameraMisses, 0, 5);
            
            if (cameraMisses > 5)
            {
                Logger.LogWarning("[CameraWatchdog] Unresponsive XR rendering setup detected!");
                Logger.LogWarning($"[CameraWatchdog] Current active camera: {(activeCamera ? activeCamera : "<NONE>")}");
            }
            
            yield return new WaitForSeconds(2f);
            
            if (activeCamera is null)
            {
                cameraMisses++;
                continue;
            }

            var hdCamera = activeCamera.GetComponent<HDAdditionalCameraData>();
            if (hdCamera.xrRendering)
            {
                cameraMisses--;
                continue;
            }

            cameraMisses++;
        }
    }
}
