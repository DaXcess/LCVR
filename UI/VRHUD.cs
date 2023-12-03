using UnityEngine;

namespace LethalCompanyVR
{
    internal class VRHUD : MonoBehaviour
    {
        private Canvas canvas;
        private Camera camera;

        public void Initialize(Camera camera)
        {
            this.camera = camera;

            canvas = gameObject.AddComponent<Canvas>();
            canvas.worldCamera = camera;
            canvas.renderMode = RenderMode.WorldSpace;

            //GameObject.Find("IngamePlayerHUD").transform.SetParent(transform, false);
            GameObject.Find("PlayerCursor").transform.SetParent(transform, false);

            transform.localScale = Vector3.one * 0.0007f;
        }

        private void LateUpdate()
        {
            transform.position = camera.transform.position + camera.transform.forward * 0.5f;
            transform.rotation = camera.transform.rotation;
        }
    }
}
