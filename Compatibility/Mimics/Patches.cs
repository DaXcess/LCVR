using HarmonyLib;
using LCVR.Patches;
using LCVR.UI;
using Mimics;
using UnityEngine;

namespace LCVR.Compatibility
{
    [LCVRPatch(dependency: "Mimics")]
    [HarmonyPatch]
    internal static class MimicUIPatches
    {
        [HarmonyPatch(typeof(RoundManager), "SetExitIDs")]
        [HarmonyPostfix]
        [HarmonyAfter(["x753.Mimics"])]
        private static void AssignInteractTriggerOffset()
        {
            // The difference between the original fire door and the mimic 
            var offset = new Vector3(0, 1.6307f, -0.35f);
            
            foreach (var mimicDoor in MimicDoor.allMimics)
            {
                mimicDoor.interactTrigger.gameObject.AddComponent<InteractCanvasPositionOffset>().offset = offset;
            }
        }
    }
}
