using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MiniMapUI : MonoBehaviour {
    RectTransform rectTransform;
    Vector2 scale;
    bool isOverMap;
    Vector2 thisPosition;
    void Start() {
        rectTransform = GetComponent<RectTransform>();
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry drag = new EventTrigger.Entry {
            eventID = EventTriggerType.Drag
        };
        drag.callback.AddListener((data) => {
            Move(((PointerEventData)data).position);
        });
        trigger.triggers.Add(drag);
        EventTrigger.Entry down = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerDown
        };
        down.callback.AddListener((data) => {
            Move(((PointerEventData)data).position);
        });
        trigger.triggers.Add(down);

        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener((data) => {
            isOverMap = true;
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry leave = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        leave.callback.AddListener((data) => {
            isOverMap = false;
        });
        trigger.triggers.Add(leave);

        float canvasScaleWidth = Screen.width / GetComponentInParent<UnityEngine.UI.CanvasScaler>().referenceResolution.x;
        float canvasScaleHeight = Screen.height / GetComponentInParent<UnityEngine.UI.CanvasScaler>().referenceResolution.y;
        thisPosition = rectTransform.anchoredPosition * new Vector2(canvasScaleWidth, canvasScaleHeight);
        scale = new Vector2 {
            x = ((float)World.Current.Width) / (canvasScaleWidth * rectTransform.sizeDelta.x * rectTransform.localScale.x),
            y = ((float)World.Current.Height) / (canvasScaleHeight * rectTransform.sizeDelta.y * rectTransform.localScale.y)
        };
    }

    private void Move(Vector2 pressPosition) {
        if (isOverMap == false)
            return;
        CameraController.Instance.MoveCameraToPosition(((pressPosition - thisPosition) * scale) );
    }

}
