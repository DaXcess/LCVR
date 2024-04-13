using System.Collections;
using HarmonyLib;
using UnityEngine;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class CutscenePatches
{
    private static GameObject uiCamera;
    
    [HarmonyPatch(typeof(ColdOpenCinematicCutscene), nameof(ColdOpenCinematicCutscene.Start))]
    [HarmonyPostfix]
    private static void OnCutsceneStarted(ColdOpenCinematicCutscene __instance)
    {
        uiCamera = GameObject.Find("UICamera");
        uiCamera.SetActive(false);
        
        __instance.cam.gameObject.AttachHeadTrackedPoseDriver();
        __instance.cam.targetTexture = null;
        __instance.camContainer.transform.localScale *= 1.8f;

        __instance.gameObject.AddComponent<VRCutscene>();
    }

    [HarmonyPatch(typeof(ColdOpenCinematicCutscene), nameof(ColdOpenCinematicCutscene.EndColdOpenCutscene))]
    [HarmonyPrefix]
    private static void OnCutsceneEnded()
    {
        if (uiCamera)
            uiCamera.SetActive(true);
    }

    private class VRCutscene : MonoBehaviour
    {
        private Transform cameraContainer;
        private Transform cameraTransform;

        private float initialCameraRotation;
        private Vector3 initialCameraPosition;
        
        private void Awake()
        {
            cameraContainer = GameObject.Find("CameraControllerContainer/CameraController").transform;
            cameraTransform = cameraContainer.Find("MainCamera");
            
            // Disable chair since we assume VR players are standing up
            GameObject.Find("ComputerChairAndShelf")?.SetActive(false);

            StartCoroutine(delayedResetHMD());
        }
        
        private void Update()
        {
            if (Mathf.Abs(cameraTransform.localPosition.y + cameraContainer.localPosition.y) > 0.5f)
                ResetHeight();
        }

        private void LateUpdate()
        {
            cameraContainer.transform.localEulerAngles = new Vector3(0, -initialCameraRotation, 0);
        }

        private IEnumerator delayedResetHMD()
        {
            yield return new WaitForSeconds(0.1f);
            
            initialCameraRotation = cameraTransform.localEulerAngles.y;
            initialCameraPosition = (cameraContainer.localPosition - cameraTransform.localPosition) * 1.8f;
            
            ResetHeight();
        }

        private void ResetHeight()
        {
            cameraContainer.localPosition = new Vector3(initialCameraPosition.x, -cameraTransform.localPosition.y, initialCameraPosition.z);
        }
    }
}
