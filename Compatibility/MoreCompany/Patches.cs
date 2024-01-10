using HarmonyLib;
using LCVR.Patches;
using MoreCompany.Behaviors;
using MoreCompany.Cosmetics;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine;

namespace LCVR.Compatibility
{
    [LCVRPatch(dependency: "MoreCompany")]
    [HarmonyPatch]
    internal static class MoreCompanyUIPatches
    {
        [HarmonyPatch(typeof(CosmeticRegistry), "UpdateCosmeticsOnDisplayGuy")]
        [HarmonyPostfix]
        private static void AfterUpdateCosmetics()
        {
            var cosmeticApplication = (CosmeticApplication)typeof(CosmeticRegistry).GetField("cosmeticApplication", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            cosmeticApplication.spawnedCosmetics.Do(cosmetic => cosmetic.transform.localScale *= 0.5f);
        }

        // Spin dragger patches
        private static int pointer = -1;
        private static XRRayInteractor leftInteractor;
        private static XRRayInteractor rightInteractor;
        private static Vector2 lastRayPosition = Vector2.zero;
        private static Vector3 rotationalVelocity = Vector3.zero;

        [HarmonyPatch(typeof(SpinDragger), "Update")]
        [HarmonyPrefix]
        private static bool UpdateSpinDragger(SpinDragger __instance)
        {
            if (pointer != -1)
            {
                var position = Vector2.zero;

                var interactor = (pointer == 1 ? rightInteractor : leftInteractor);
                if (interactor.TryGetCurrentUIRaycastResult(out var res))
                    position = res.screenPosition;

                var delta = position - lastRayPosition;
                rotationalVelocity = new Vector3(0, -delta.x, 0) * __instance.dragSpeed;
                lastRayPosition = position;
            }

            rotationalVelocity *= __instance.airDrag;

            __instance.target.transform.Rotate(rotationalVelocity * Time.deltaTime * __instance.speed, Space.World);

            return false;
        }

        [HarmonyPatch(typeof(SpinDragger), "OnPointerDown")]
        [HarmonyPrefix]
        private static bool OnPointerDown(SpinDragger __instance, PointerEventData eventData)
        {
            __instance.dragSpeed = 10;

            leftInteractor = GameObject.Find("Left Controller").GetComponent<XRRayInteractor>();
            rightInteractor = GameObject.Find("Right Controller").GetComponent<XRRayInteractor>();

            pointer = eventData.pointerId;

            if (pointer != 1 && pointer != 2)
            {
                pointer = -1;
                return false;
            }

            var interactor = (pointer == 1 ? rightInteractor : leftInteractor);
            if (interactor.TryGetCurrentUIRaycastResult(out var res))
                lastRayPosition = res.screenPosition;
            else
                lastRayPosition = Vector2.zero;

            return false;
        }

        [HarmonyPatch(typeof(SpinDragger), "OnPointerUp")]
        [HarmonyPrefix]
        private static bool OnPointerUp()
        {
            pointer = -1;

            return false;
        }
    }
}
