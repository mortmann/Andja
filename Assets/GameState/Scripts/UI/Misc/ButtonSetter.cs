using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSetter : MonoBehaviour {
	// Use this for initialization
	void Start () {
    }
    /// <summary>
    /// func : () => { Function( parameter ); return null?; }
    /// </summary>
    /// <param name="name"></param>
    /// <param name="func"></param>
    public void Set(string name, Func<object> func) {
        GetComponentInChildren<Text>().text = name;
        GetComponent<Button>().onClick.AddListener(()=> { func(); });
    }
    /// <summary>
    /// func : () => { Function( parameter ); }
    /// </summary>
    /// <param name="name"></param>
    /// <param name="func"></param>
    public void Set(string name, Action func) {
        GetComponentInChildren<Text>().text = name;
        GetComponent<Button>().onClick.AddListener(() => { func(); });
    }
}
