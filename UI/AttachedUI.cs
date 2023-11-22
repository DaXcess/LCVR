using UnityEngine;

namespace LethalCompanyVR
{
    // TODO: Since this game appears to do their UI very differently, this file may not be needed

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
            if (Plugin.MainCamera == null) return;

            transform.position = Plugin.MainCamera.transform.position + Plugin.MainCamera.transform.forward;
            transform.rotation = Plugin.MainCamera.transform.rotation;
        }
    }
}
