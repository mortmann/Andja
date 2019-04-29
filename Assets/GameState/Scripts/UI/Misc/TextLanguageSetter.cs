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
        // if it contains * then its used in other with same name
        if (name.StartsWith("*") == false) {
            while (current.parent != null) {
                realname = current.name + "/" + realname;
                current = current.parent;
            }
        }
        else {
            realname = name + "/";
        }
        if (realname == "Health/")
            Debug.Log("blub " + gameObject);
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
            if(text != null)
                myText.text = text;
            UILanguageController.Instance.RegisterLanguageChange(OnChangeLanguage);
            if (GetComponent<EventTrigger>() == null) {
                this.gameObject.AddComponent<EventTrigger>();
            }
        }
        hoverHoverText = UILanguageController.Instance.GetHoverOverText(realname);
        if (UILanguageController.Instance.HasHoverOverText(realname) == false) {
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
        GameObject.FindObjectOfType<HoverOverScript>().Show(hoverHoverText);
    }
    public void OnMouseExit() {
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();
    }
}
