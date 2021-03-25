using UnityEngine;
using UnityEngine.EventSystems;

public class ShowHoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public string Text;
    public void OnPointerEnter(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Show(Text);
    }
    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
