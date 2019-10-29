using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(EventTrigger))]
public class ButtonSetter : MonoBehaviour {
	// Use this for initialization
	void Start () {
    }
    /// <summary>
    /// func : () => { Function( parameter ); return null?; }
    /// </summary>
    /// <param name="name"></param>
    /// <param name="func"></param>
    public void Set(string name, Func<object> func, Sprite Icon = null, string hoverOver = null) {
        GetComponent<Button>().onClick.AddListener(()=> { func(); });
        SetUp(name, Icon, hoverOver);
    }
    /// <summary>
    /// func : () => { Function( parameter ); }
    /// </summary>
    /// <param name="name"></param>
    /// <param name="func"></param>
    public void Set(string name, Action func, Sprite Icon = null, string hoverOver = null) {
        GetComponent<Button>().onClick.AddListener(() => { func(); });
        SetUp(name, Icon, hoverOver);
    }
    void SetUp(string name, Sprite Icon, string hoverOver) {
        if (Icon == null) {
            GetComponentsInChildren<Image>()[1].gameObject.SetActive(false);
            GetComponentInChildren<Text>().text = name;
        } else {
            GetComponentsInChildren<Image>()[1].overrideSprite = Icon;
            GetComponentInChildren<Text>().gameObject.SetActive(false);
        }
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (hoverOver != null) {
            EventTrigger.Entry enter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener((data) => {
                OnMouseEnter(hoverOver);
            });
            trigger.triggers.Add(enter);

            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener((data) => {
                OnMouseExit();
            });
            trigger.triggers.Add(exit);
        }
    }
    public void OnMouseEnter(string hoverOver) {
        GameObject.FindObjectOfType<HoverOverScript>().Show(hoverOver);
    }
    public void OnMouseExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
    public void Interactable(bool interactable) {
        GetComponent<Button>().interactable = interactable;
        Image i = GetComponentsInChildren<Image>()[1];
        Color c = i.color;
        //if interactable go full otherwise half
        c.a = interactable ? 1 : 0.5f;
        i.color = c;
    }
}
