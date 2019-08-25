using System;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldNumberRange : MonoBehaviour {

    InputField inputfield;

    public float Minimum = 0;
    public float Maximum = 10;

    void Start () {
        if (Maximum < Minimum)
            Debug.LogWarning("Maximum is lower than Minimum! Please fix.");
        inputfield = GetComponent<InputField>();
        inputfield.onEndEdit.AddListener(CheckForNegative);

    }

    private void CheckForNegative(string text) {
        float value = 0;
        if(float.TryParse(text, out value) == false && text.Length>0) {
            Debug.LogError(transform.parent?.name + " Inputfield is not number only ");
        }
        if (value < Minimum)
            text = ""+ Minimum;
        if (value > Maximum)
            text = "" + Maximum;
        inputfield.text = text;
    }
    private void OnDisable() {
        CheckForNegative(inputfield.text);
    }
}
