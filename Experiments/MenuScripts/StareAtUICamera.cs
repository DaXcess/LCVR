using UnityEngine;

namespace LCVR.Experiments.MenuScripts
{
    public class StareAtUICamera : MonoBehaviour
    {
        [SerializeField]
        private Vector3 rotationOffset;

        private Transform targetTransform;

        void Start()
        {
            targetTransform = GameObject.Find("UICamera").transform;
        }

        void Update()
        {
            var rotation = Quaternion.LookRotation(targetTransform.position - transform.position) * Quaternion.Euler(rotationOffset);

            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, 0.05f);
        }
    }
}