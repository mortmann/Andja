using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NameInputField : MonoBehaviour {
    public InputField NameText;
    public void SetName(string name, UnityAction<string> OnNameEdit) {
        //Make the Name editable
        NameText.onEndEdit.AddListener(OnNameEdit);
        NameText.onEndEdit.AddListener(EndText);
        NameText.readOnly = true;
        NameText.interactable = false;
        NameText.text = name;
        EventTrigger trigger = NameText.GetComponent<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerClick
        };
        click.callback.AddListener((data) => {
            PointerEventData ped = ((PointerEventData)data);
            if (ped.button == PointerEventData.InputButton.Right)
                OnInputFieldClick();
        });
        trigger.triggers.Add(click);
    }
    private void EndText(string end) {
        NameText.readOnly = true;
    }
    private void OnInputFieldClick() {
        if (NameText.readOnly == false)
            return;
        NameText.readOnly = false;
        NameText.interactable = true;
        NameText.Select();
    }
}
