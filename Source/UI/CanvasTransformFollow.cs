using LCVR.Assets;
using LCVR.Input;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.UI;

internal class CanvasTransformFollow : MonoBehaviour
{
    private const float TURN_SMOOTHNESS = 0.05f;
    private const float CANVAS_DISTANCE = 5f;

    public Transform sourceTransform;
    public float heightOffset;

    private Quaternion targetRotation;
    private Vector3 targetPosition;

    private Transform enemyTransform;

    private void Awake()
    {
        Actions.Instance["Reset Height"].performed += OnResetHeight;

        enemyTransform = Instantiate(AssetManager.EnemyPrefab).transform;

        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        Actions.Instance["Reset Height"].performed -= OnResetHeight;
        
        if (enemyTransform != null)
            Destroy(enemyTransform.gameObject);
    }

    private void Update()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, TURN_SMOOTHNESS);
        transform.position = Vector3.Lerp(transform.position, targetPosition, TURN_SMOOTHNESS);
    }

    public void ResetPosition(bool force = false)
    {
        var rotation = Quaternion.Euler(0, sourceTransform.eulerAngles.y, 0);
        var forward = rotation * Vector3.forward;
        var position = forward * CANVAS_DISTANCE;
        var enemyPosition = -forward * (CANVAS_DISTANCE * 3);

        targetPosition = new Vector3(position.x + sourceTransform.position.x, heightOffset,
            position.z + sourceTransform.position.z);
        targetRotation = Quaternion.Euler(0, sourceTransform.eulerAngles.y, 0);

        enemyTransform.position = new Vector3(enemyPosition.x + sourceTransform.position.x, heightOffset - 3.5f,
            enemyPosition.z + sourceTransform.position.z);
        enemyTransform.rotation = Quaternion.Euler(0, sourceTransform.eulerAngles.y + 180, 0);

        if (force)
        {
            transform.rotation = targetRotation;
            transform.position = targetPosition;
        }
    }

    private IEnumerator Init()
    {
        yield return null;

        ResetPosition(true);
    }

    private void OnResetHeight(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ResetPosition();
    }
}
