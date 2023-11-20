using LethalCompanyVR.UI.Patches;
using UnityEngine;

namespace LethalCompanyVR.UI
{
    // TODO: This looks like absolute garbage rn
    public class AttachedUI : MonoBehaviour
    {
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        public static AttachedUI Create(Canvas canvas, float scale = 0)
        {
            var instance = canvas.gameObject.AddComponent<AttachedUI>();
            if (scale > 0) canvas.transform.localScale = Vector3.one * scale;
            canvas.renderMode = RenderMode.WorldSpace;

            return instance;
        }

        protected virtual void Update()
        {
            UpdateTransform();

            if (UIPatches.UICamera != null)
            {
                if (targetPosition == null || Vector3.Distance(UIPatches.UICamera.transform.position, targetPosition) > 10f)
                {
                    var forward = UIPatches.UICamera.transform.forward;
                    forward.y = 0;
                    forward.Normalize();

                    var newPosition = UIPatches.UICamera.transform.position + forward * 5;
                    newPosition.y = 0;

                    SetTargetTransform(newPosition, Quaternion.Euler(0, UIPatches.UICamera.transform.rotation.eulerAngles.y, 0));
                }
            }
        }

        public void SetTargetTransform(Vector3 targetPosition, Quaternion targetRotation)
        {
            this.targetPosition = targetPosition;
            this.targetRotation = targetRotation;
        }

        private void UpdateTransform()
        {
            if (targetPosition != null && targetRotation != null)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }
    }
}
