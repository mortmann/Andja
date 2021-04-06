using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowHoverOver : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    LanguageVariables Variables;
    bool showName;
    public void OnPointerEnter(PointerEventData eventData) {
        if(showName) {
            GameObject.FindObjectOfType<HoverOverScript>().Show(Variables.Name);
        }
        else {
            GameObject.FindObjectOfType<HoverOverScript>().Show(Variables.HoverOver);
        }
    }
    public void OnPointerExit(PointerEventData eventData) {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }

    internal void SetVariable(LanguageVariables data, bool showName) {
        Variables = data;
        this.showName = showName;
    }
}
