using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EventTrigger))]
public class TextLanguageSetter : MonoBehaviour {
    // Use this for initialization
    public bool OnlyHoverOver;

    Text myText;
    private string hoverHoverText;

    public string GetRealName() {
        string realname = "";
        Transform current = transform;
        if (name.StartsWith("*") == false) {
            //have to make this like this cause you cant compare transform to null transform
                while (current.parent != null && current.name.StartsWith("#")==false) {
                    realname = current.name + "/" + realname;
                    current = current.parent;
                }
        }
        else {
            realname = name + "/";
        }
        return realname;
    }

    void Start() {
        string realname = GetRealName();
        if (OnlyHoverOver == false) {
            myText = GetComponent<Text>();
            if (myText == null)
                myText = GetComponentInChildren<Text>();
            if (myText == null) {
                Debug.LogError("TextLanguageSetter has no text object! " + name);
                return;
            }
            string text = UILanguageController.Instance?.GetText(realname);
            if(string.IsNullOrEmpty(text) ==false)
                myText.text = text;
            UILanguageController.Instance.RegisterLanguageChange(OnChangeLanguage);
            if (GetComponent<EventTrigger>() == null) {
                this.gameObject.AddComponent<EventTrigger>();
            }
        }
        if (UILanguageController.Instance.HasHoverOverText(realname) == false) {
            return;
        }
        hoverHoverText = UILanguageController.Instance.GetHoverOverText(realname);

        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry enter = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerEnter
        };
        enter.callback.AddListener((data) => {
            OnMousePointerEnter();
        });
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry {
            eventID = EventTriggerType.PointerExit
        };
        exit.callback.AddListener((data) => {
            OnMousePointerExit();
        });
        trigger.triggers.Add(exit);
    }
    void OnChangeLanguage() { 
        string text = UILanguageController.Instance.GetText(name);
        if (string.IsNullOrEmpty(text) == false)
            myText.text = text;
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
    public void OnMousePointerEnter() {
        GameObject.FindObjectOfType<HoverOverScript>().Show(hoverHoverText);
    }
    public void OnMousePointerExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
