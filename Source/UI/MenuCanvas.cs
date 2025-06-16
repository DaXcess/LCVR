using System.Collections;
using LCVR.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LCVR.UI;

public class MenuCanvas : MonoBehaviour
{
    [SerializeField] private Transform sourceTransform;
    [SerializeField] private float canvasDistance = 5f;
    [SerializeField] private float heightOffset = 1f;
    [SerializeField] private float turnSmoothness = 1f;
    
    private Quaternion targetRotation;
    private Vector3 targetPosition;
    
    private void Awake()
    {
        Actions.Instance["Reset Height"].performed += OnResetHeight;
        
        StartCoroutine(Init());
    }

    private void OnDestroy()
    {
        Actions.Instance["Reset Height"].performed -= OnResetHeight;
    }

    private void Update()
    {
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSmoothness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, turnSmoothness * Time.deltaTime);
    }

    public void ResetPosition(bool force = false)
    {
        var rotation = Quaternion.Euler(0, sourceTransform.eulerAngles.y, 0);
        var forward = rotation * Vector3.forward;
        var position = forward * canvasDistance;

        targetPosition = new Vector3(position.x + sourceTransform.position.x, heightOffset,
            position.z + sourceTransform.position.z);
        targetRotation = Quaternion.Euler(0, sourceTransform.eulerAngles.y, 0);

        if (!force)
            return;

        transform.rotation = targetRotation;
        transform.position = targetPosition;
    }

    public void SetHeightOffset(float offset)
    {
        heightOffset = offset;
    }

    private IEnumerator Init()
    {
        yield return new WaitUntil(() => sourceTransform.position != Vector3.zero);

        ResetPosition(true);
    }

    private void OnResetHeight(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        ResetPosition();
    }
}