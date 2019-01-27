using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class BrushSlider : MonoBehaviour {
    public Slider s;
    public Text t;
    public bool size;
    // Use this for initialization
    void Start() {
        if (size) {
            s.onValueChanged.AddListener(OnSizeSliderChange);
            t.text = 1.ToString();
        }
        else {
            EditorController.Instance.OnBrushRandomChange(100);
            s.onValueChanged.AddListener(OnRandomSliderChange);
            t.text = "Off";
        }
    }
    public void OnSizeSliderChange(float f) {
        t.text = f.ToString();
        EditorController.Instance.SetBrushSize((int)f);
    }
    public void OnRandomSliderChange(float f) {
        if (f == 100) {
            t.text = "Off";
        }
        else {
            t.text = (f).ToString();
        }
        EditorController.Instance.OnBrushRandomChange(f);
    }
}
