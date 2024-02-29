using UnityEngine;

namespace LCVR.Experiments.MenuScripts;

/// <summary>
/// Makes the attached object look at the UI camera
/// </summary>
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