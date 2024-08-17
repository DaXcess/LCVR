using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Animations;

namespace LCVR.Patches;

[LCVRPatch]
[HarmonyPatch]
internal static class CutscenePatches
{
    private static GameObject uiCamera;
    
    [HarmonyPatch(typeof(InitializeGame), nameof(InitializeGame.Awake))]
    [HarmonyPostfix]
    private static void ForceCutscene(InitializeGame __instance)
    {
        __instance.playColdOpenCinematic2 = !ES3.Load("PlayedCinematic2VR", "LCGeneralSaveData", false);
        ES3.Save("PlayedCinematic2VR", true, "LCGeneralSaveData");
    }

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

    [HarmonyPatch(typeof(ColdOpenCinematicCutscene), nameof(ColdOpenCinematicCutscene.Update))]
    [HarmonyPrefix]
    private static bool PreventFlatscreenHeadMovement()
    {
        return false;
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
        private Transform helmetTransform;
        
        private float initialCameraRotation;
        private Vector3 initialCameraPosition;

        private void Awake()
        {
            helmetTransform = GameObject.Find("ColdOpen2AnimContainer/ScavengerHelmet").transform;
            cameraContainer = GameObject.Find("CameraControllerContainer/CameraController").transform;
            cameraTransform = cameraContainer.Find("MainCamera");
            
            Destroy(helmetTransform.GetComponent<ParentConstraint>());

            StartCoroutine(delayedResetHMD());
        }

        private void Update()
        {
            if (Mathf.Abs(cameraTransform.localPosition.y + cameraContainer.localPosition.y) > 0.5f)
                ResetHeight();
        }

        private void LateUpdate()
        {
            helmetTransform.position = cameraTransform.TransformPoint(0, -0.1f, -0.04f);
            helmetTransform.rotation = cameraTransform.rotation * Quaternion.Euler(270, 0, 0);

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
            cameraContainer.localPosition = new Vector3(initialCameraPosition.x,
                -cameraTransform.localPosition.y - 0.75f,
                initialCameraPosition.z);
        }
    }
}