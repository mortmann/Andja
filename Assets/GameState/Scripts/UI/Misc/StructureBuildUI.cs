﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StructureBuildUI : MonoBehaviour {

    public GameObject mouseOverPrefab;
    public Structure structure;

    // Use this for initialization
    public void Show(Structure str, bool hoverOver = true) {
        this.structure = str;
        GetComponentInChildren<Text>().text = str.SpriteName;
        EventTrigger trigger = GetComponent<EventTrigger>();
        if (hoverOver) {
            EventTrigger.Entry enter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener((data) => {
                OnMouseEnter();
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


        EventTrigger.Entry dragStart = new EventTrigger.Entry {
            eventID = EventTriggerType.BeginDrag
        };
        dragStart.callback.AddListener((data) => {
            OnDragStart();
        });
        trigger.triggers.Add(dragStart);


        EventTrigger.Entry dragStop = new EventTrigger.Entry {
            eventID = EventTriggerType.EndDrag
        };
        dragStop.callback.AddListener((data) => {
            OnDragEnd();
        });
        trigger.triggers.Add(dragStop);
    }
    public void OnMouseEnter() {
        //		hoverover = true;
        GameObject.FindObjectOfType<HoverOverScript>().Show(structure.SpriteName);
    }
    public void OnMouseExit() {
        //TODO: reset hovertime better
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();

    }
    public void OnDragStart() {
        UIController.Instance.SetDragAndDropBuild(this.gameObject);
    }
    public void OnDragEnd() {
        UIController.Instance.StopDragAndDropBuild();
    }

}