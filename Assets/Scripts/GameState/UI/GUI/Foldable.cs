using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class Foldable : MonoBehaviour {
    public Image Image;
    public Text Title;
    public GameObject TitleGO;
    public Transform Content;
    EventTrigger triggers;
    bool isActive;
    void Start() {
        triggers = GetComponentInChildren<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        click.callback.AddListener((data) => {
            OnMouseClick();
        });
        triggers.triggers.Add(click);
        if (GetComponentInParent<ScrollRect>() != null) {
            EventTrigger.Entry scroll = new EventTrigger.Entry {
                eventID = EventTriggerType.Scroll
            };
            scroll.callback.AddListener((data) => {
                ScrollRect sr = GetComponentInParent<ScrollRect>();
                sr.verticalScrollbar.value += sr.scrollSensitivity * Time.deltaTime * ((PointerEventData)data).scrollDelta.y;
            });
            triggers.triggers.Add(scroll);
        }
    }
    public void Set(string title) {
        Title.text = title;
    }
    private void OnMouseClick() {
        if (isActive==false)
            return;
        Content.gameObject.SetActive(!Content.gameObject.activeSelf);
    }

    public void Add(GameObject go) {
        go.transform.SetParent(Content);
    }

    internal void Check() {
        foreach(Transform t in Content) {
            if (t.gameObject.activeSelf) {
                isActive = true;
                GetComponent<CanvasGroup>().alpha = 1;
                return;
            }
        }
        GetComponent<CanvasGroup>().alpha = 0.5f;
        Content.gameObject.SetActive(false);
        isActive = false;
    }
}
