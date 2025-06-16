using LCVR.Assets;
using TMPro;
using UnityEngine;

namespace LCVR.UI;

public class PopupText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textElement;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private Vector3 targetPosition;

    private float timer;
    private bool destroyed;

    private void Awake()
    {
        canvasGroup.alpha = 0;
        
        textElement.m_fontMaterial = textElement.CreateMaterialInstance(textElement.m_sharedMaterial);
        textElement.m_sharedMaterial = textElement.m_fontMaterial;
        textElement.m_sharedMaterial.shader = AssetManager.TMPAlwaysOnTop;
    }

    private void Update()
    {
        transform.localPosition = Vector3.Slerp(transform.localPosition, targetPosition, 8 * Time.deltaTime);
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, destroyed ? 0 : 1, (destroyed ? 5 : 8) * Time.deltaTime);

        timer -= Time.deltaTime;

        if (timer <= 0)
            destroyed = true;

        if (!destroyed)
            return;
        
        if (canvasGroup.alpha <= 0.01f)
            Destroy(gameObject);
    }

    public void UpdateText(string text, float time = 2)
    {
        if (destroyed)
            return;
        
        textElement.text = text;
        timer = time;
    }

    public static PopupText Create(Transform origin, Vector3 positionOffset, string text, float time = 2)
    {
        var position = origin.position + positionOffset;
        var popup = Instantiate(AssetManager.PopupText, position + Vector3.down * 0.15f, Quaternion.Euler(0, origin.eulerAngles.y, 0))
            .GetComponent<PopupText>();

        popup.targetPosition = position;
        popup.textElement.text = text;
        popup.timer = time;

        return popup;
    }
}
