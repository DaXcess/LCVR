using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LCVR.UI.Settings;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] private TextMeshProUGUI text;

    private Image background;
    private Color color;

    private void Start()
    {
        background = GetComponent<Image>();
        color = text.color;
    }

    public void OnPointerEnter(PointerEventData _)
    {
        text.color = Color.black;
        background.color = new Color(1, 0.4319f, 0);
    }

    public void OnPointerExit(PointerEventData _)
    {
        text.color = color;
        background.color = new Color(0, 0, 0, 0);
    }

    public void OnPointerDown(PointerEventData _)
    {
        text.color = color;
        background.color = new Color(0, 0, 0, 0);
    }
}