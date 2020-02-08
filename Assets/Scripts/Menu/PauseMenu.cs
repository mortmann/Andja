using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PauseMenu : MonoBehaviour {
    public static bool IsOpen = false;
    public static Action<bool> ChangedState;
    private void Awake() {
        Disable();
    }
    public void Toggle() {
        if(IsOpen) {
            Disable();
        } else {
            Enable();
        }
    }
    public void Enable() {
        IsOpen = true;
        transform.GetChild(0).gameObject.SetActive(true);
        ChangedState?.Invoke(true);
    }
    public void Disable() {
        IsOpen = false;
        transform.GetChild(0).gameObject.SetActive(false);
        ChangedState?.Invoke(false);
    }
    private void OnDestroy() {
        IsOpen = false;
    }
}
