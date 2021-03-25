using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EventMessage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    string eventName;
    public Vector2 position;

    public void Setup(string name, Vector2 position) {
        this.position = position;
        this.eventName = name;
        if (name.Length > 30) {
            name = name.Substring(0, 30) + "...";
        }
        GetComponentInChildren<Text>().text = name;
        //TODO change Image here also
        //Probably load the sprites in EventUIManager and get it from there 
    }

    public void OnPointerEnter(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Show(eventName);
    }

    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (position.x < 0 || position.y < 0) {
            return;
        }
        CameraController.Instance.MoveCameraToPosition(position);
    }
}
