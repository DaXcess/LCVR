using UnityEngine;

namespace LCVR.UI.Environment;

public class SkyboxController : MonoBehaviour
{
    [SerializeField] private Transform sourceTransform;

    private void Update()
    {
        transform.position = sourceTransform.position;
    }
}