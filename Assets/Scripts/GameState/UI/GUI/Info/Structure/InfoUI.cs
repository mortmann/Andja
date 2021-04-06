using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InfoUI : MonoBehaviour {
    public static InfoUI Open;
    public void Show(object Info) {
        if (Open != null)
            Open.gameObject.SetActive(false);
        Open = this;
        OnShow(Info);
    }
    public abstract void OnShow(object Info);
    public abstract void OnClose();
    private void OnDisable() {
        Open = null;
        OnClose();
    }
}
