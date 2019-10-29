using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EffectUI : MonoBehaviour {
    public Image EffectImage;
    Effect effect;
    // Use this for initialization
	public void Show (Effect effect) {
        //TODO: add way to get the image for the effect 
        this.effect = effect;
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener((data) => {
            OnMouseEnter();
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener((data) => {
            OnMouseExit();
        });
        trigger.triggers.Add(exit);
    }

    public void OnMouseEnter() {
        GameObject.FindObjectOfType<HoverOverScript>().Show(effect.Name);
    }
    public void OnMouseExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
