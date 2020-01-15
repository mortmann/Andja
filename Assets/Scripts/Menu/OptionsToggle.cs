using UnityEngine;
using System.Collections;
using System;

public class OptionsToggle : MonoBehaviour {
    public static Action<bool> ChangedState;

    public GameObject[] childrens;
    int lastOpen = 0;
    void OnEnable() {
        ChangedState?.Invoke(true);
        Show(lastOpen);
    }
    public void Show(int numberOfChildToShow) {
        if (childrens == null)
            return;
        for (int i = 0; i < childrens.Length; i++) {
            childrens[i].SetActive(false);
        }
        childrens[numberOfChildToShow].SetActive(true);
        lastOpen = numberOfChildToShow;
    }
    private void OnDisable() {
        for (int i = 0; i < childrens.Length; i++) {
            childrens[i].SetActive(false);
        }
        ChangedState?.Invoke(false);
        Debug.Log("OnDisable");
    }
}
