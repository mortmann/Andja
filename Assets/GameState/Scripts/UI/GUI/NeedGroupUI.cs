using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NeedGroupUI : MonoBehaviour {
    public Text nameText;
    public GameObject listGO;
    public void Show(NeedGroup group) {
        nameText.text = group.Name;
    }
    public void OnDisable() {
        Debug.Log("WHY?!?!?");
    }
}
