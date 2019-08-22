using UnityEngine;
using System.Collections;

public class Resizer : MonoBehaviour {

    // Use this for initialization
    void Start() {
        AdjustSize();
    }

    public void AdjustSize() {
        Vector2 size = this.GetComponent<RectTransform>().sizeDelta;
        this.GetComponent<RectTransform>().sizeDelta = size;
    }
}
