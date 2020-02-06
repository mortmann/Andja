using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum BrushType { Size, Random }
public class BrushSlider : MonoBehaviour {
    public Slider s;
    public Text t;
    public BrushType Type;
    // Use this for initialization
    void Start() {
        switch (Type) {
            case BrushType.Size:
                s.onValueChanged.AddListener(OnSizeSliderChange);
                t.text = 1.ToString();
                break;
            case BrushType.Random:
                EditorController.Instance.OnBrushRandomChange(100);
                s.onValueChanged.AddListener(OnRandomSliderChange);
                t.text = "Off";
                break;
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
