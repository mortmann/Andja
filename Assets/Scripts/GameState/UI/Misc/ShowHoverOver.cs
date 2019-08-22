using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class ShowHoverOver : MonoBehaviour {
    public string Text;
    public void Start() {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener((data) => {
            OnPointerEnter();
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener((data) => {
            OnPointerExit();
        });
        trigger.triggers.Add(exit);
    }
    public void OnPointerEnter() {
        GameObject.FindObjectOfType<HoverOverScript>().Show(Text);
    }
    public void OnPointerExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
