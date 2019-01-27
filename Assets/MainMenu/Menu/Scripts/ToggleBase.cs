using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleBase : MonoBehaviour {
    protected Toggle toggle;
    public GameplaySetting setting;
    public string[] displayLabels;
    protected Text displayValue;

    protected bool IsOn => toggle.isOn;
    protected int DiplayValue => toggle.isOn ? 0 : 1;

    // Use this for initialization
    void OnEnable() {
        toggle = GetComponent<Toggle>();
        displayValue = transform.Find("Value").GetComponent<Text>();
        displayValue.text = displayLabels[DiplayValue];
    }
    void Start() {
        toggle.onValueChanged.AddListener(OnToggleClick);
        OnStart();
    }

    protected virtual void OnStart() {
    }
    protected void OnToggleClick(bool change) {
        displayValue.text = displayLabels[DiplayValue];
        OnClick();
    }
    protected virtual void OnClick() {
    }
}
