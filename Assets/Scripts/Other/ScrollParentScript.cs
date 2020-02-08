using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(requiredComponent:typeof(EventTrigger))]
public class ScrollParentScript : MonoBehaviour {
    void Start() {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        EventTrigger.Entry scroll = new EventTrigger.Entry {
            eventID = EventTriggerType.Scroll
        };
        scroll.callback.AddListener((data) => {
            ScrollRect sr = GetComponentInParent<ScrollRect>();
            sr.verticalScrollbar.value += sr.scrollSensitivity * Time.deltaTime * ((PointerEventData)data).scrollDelta.y;
        });
        trigger.triggers.Add(scroll);
        trigger.triggers.Add(click);
    }
}
