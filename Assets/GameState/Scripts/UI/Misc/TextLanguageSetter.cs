using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
public class TextLanguageSetter : MonoBehaviour {
    // Use this for initialization
    Text myText;
    void Start() {
        myText = GetComponent<Text>();
        if (myText == null)
            myText = GetComponentInChildren<Text>();
        if (myText == null) {
            Debug.LogError("TextLanguageSetter has no text object! " + name);
        }
        myText.text = UILanguageController.Instance.GetText(name);
        UILanguageController.Instance.RegisterLanguageChange(OnChangeLanguage);
        if (GetComponent<EventTrigger>() == null) {
            this.gameObject.AddComponent<EventTrigger>();
        }
        if (UILanguageController.Instance.HasHoverOverText(name) == false) {
            return;
        }
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
    void OnChangeLanguage() {
        myText.text = UILanguageController.Instance.GetText(name);
    }
    void OnDestroy() {
        if (UILanguageController.Instance == null)
            return;
        UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
    }
    void OnDisable() {
        if (UILanguageController.Instance == null)
            return;
        UILanguageController.Instance.UnregisterLanguageChange(OnChangeLanguage);
    }
    public void OnMouseEnter() {
        GameObject.FindObjectOfType<HoverOverScript>().Show(UILanguageController.Instance.GetHoverOverText(name));
    }
    public void OnMouseExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
