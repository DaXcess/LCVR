using UnityEngine;

namespace LethalCompanyVR
{
    /// <summary>
    /// Attach a canvas as a world space rendered canvas to the VR camera
    /// </summary>
    public class AttachedUI : MonoBehaviour
    {
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
        }

        private void UpdateTransform()
        {
            if (Plugin.VR_CAMERA == null) return;

            transform.position = Plugin.VR_CAMERA.transform.position + Plugin.VR_CAMERA.transform.forward;
            transform.rotation = Plugin.VR_CAMERA.transform.rotation;
        }
    }
}
