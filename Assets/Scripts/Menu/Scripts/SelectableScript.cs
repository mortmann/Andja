using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SelectableScript : MonoBehaviour {

    public void OnPointerClick() {
        GetComponent<Image>().color = Color.red;
    }
    public void OnDeselectCall() {
        GetComponent<Image>().color = Color.white;
    }
}
