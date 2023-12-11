using System.Linq;
using UnityEngine.InputSystem;

namespace LethalCompanyVR.Input
{
    internal class VRControls
    {
        public static void InsertVRControls()
        {
            // This doesn't work!
            IngamePlayerSettings.Instance.playerInput.actions.LoadBindingOverridesFromJson(Properties.Resources.inputs);
        }

        private static void AddBinding(string actionName, string bindingPath)
        {
            var action = IngamePlayerSettings.Instance.playerInput.actions.FindAction(actionName);
            if (action == null) return;

            if (action.bindings.Any(binding => binding.path == bindingPath)) return;

            action.AddBinding(bindingPath);
        }
    }
}
