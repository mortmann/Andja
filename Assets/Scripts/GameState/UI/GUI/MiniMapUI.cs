using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMapUI : MonoBehaviour {
    RectTransform rectTransform;
    Vector2 scale;

    void Start() {
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry drag = new EventTrigger.Entry {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((data) => {
            MouseClick(((PointerEventData)data).position);
        });
        trigger.triggers.Add(drag);
        rectTransform = GetComponent<RectTransform>();
        scale = new Vector2 {
            x = ((float)World.Current.Width) / rectTransform.sizeDelta.x,
            y = ((float)World.Current.Height) / rectTransform.sizeDelta.y
        };
    }

    private void MouseClick(Vector2 pressPosition) {
        CameraController.Instance.MoveCameraToPosition(pressPosition * scale);
    }

}
