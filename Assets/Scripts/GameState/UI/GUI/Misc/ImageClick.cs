using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ImageClick : MonoBehaviour, IPointerClickHandler {
    public Action<PointerEventData> Click;
    public void OnPointerClick(PointerEventData eventData) {
        Click?.Invoke(eventData);
    }
}
