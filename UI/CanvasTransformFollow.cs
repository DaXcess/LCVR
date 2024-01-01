using LCVR.Input;
using System.Collections;
using UnityEngine;

namespace LCVR.UI
{
    internal class CanvasTransformFollow : MonoBehaviour
    {
        private const float TURN_SMOOTHNESS = 0.05f;
        private const float MAX_TURN_OFFSET = 45f;

        private const float CANVAS_DISTANCE = 5f;
        private const float MAX_DISTANCE_OFFSET = 1f;

        public Transform targetTransform;

        private bool isInitialFrame = true;

        private Quaternion targetRotation;
        private Vector3 targetPosition;

        void Update()
        {
            Logger.LogDebug(Actions.XR_HeadRotation.ReadValueAsObject());

            if (isInitialFrame)
            {
                isInitialFrame = false;

                StartResetPosition();

                transform.position = targetPosition;
                transform.rotation = targetRotation;
                return;
            }

            var diff = Mathf.Abs(targetRotation.eulerAngles.y - targetTransform.eulerAngles.y);
            diff = Mathf.Min(diff, 360 - diff);

            if (diff > MAX_TURN_OFFSET ||
               Mathf.Abs(Vector3.Distance(targetTransform.position, transform.position) - CANVAS_DISTANCE) > MAX_DISTANCE_OFFSET)
            {
                StartResetPosition();
            }

            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, TURN_SMOOTHNESS);
            transform.position = Vector3.Lerp(transform.position, targetPosition, TURN_SMOOTHNESS);
        }

        private void StartResetPosition()
        {
            StartCoroutine(PositionResetRoutine());
        }

        private IEnumerator PositionResetRoutine()
        {
            var start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < .5)
            {
                ResetPosition();
                yield return null;
            }
        }

        private void ResetPosition()
        {
            var rotation = Quaternion.Euler(0, targetTransform.eulerAngles.y, 0);
            var forward = rotation * Vector3.forward;
            var position = forward * CANVAS_DISTANCE;

            targetPosition = new Vector3(position.x + targetTransform.position.x, transform.position.y, position.z + targetTransform.position.z);
            targetRotation = Quaternion.Euler(0, targetTransform.eulerAngles.y, 0);
        }
    }
}
