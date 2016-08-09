using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class GraphicsSettings : MonoBehaviour {

    [HideInInspector]
    public UnityEvent lowPresetEvent;
    [HideInInspector]
    public UnityEvent mediumPresetEvent;
    [HideInInspector]
    public UnityEvent highPresetEvent;
    [HideInInspector]
    public UnityEvent ultraPresetEvent;

    // A reference to the slider for setting graphics preset so we can update it.
    public Slider presetSlider;

    // All the settings register their listeners in Awake().
    void Awake() {
        lowPresetEvent = new UnityEvent();
        mediumPresetEvent = new UnityEvent();
        highPresetEvent = new UnityEvent();
        ultraPresetEvent = new UnityEvent();
    }

    // So we can invoke safely invoke them in Start().
    void Start() {
        // We need to "flash" the graphics panel on the screen because I've
        // designed this like a dork and all the graphics settings are part
        // of the graphics panel and won't be called if it's not active.
        // Not sure how to best fix this.
        ShowPanels.instance.panels[0].SetActive(true);
        ultraPresetEvent.Invoke();
        presetSlider.value = 3;
        ShowPanels.instance.panels[0].SetActive(false);
    }

    public void SetGraphicsPreset(float value) {
        switch ((int)value) {
            // Low.
            case 0:
                lowPresetEvent.Invoke();
                break;
            // Medium.
            case 1:
                mediumPresetEvent.Invoke();
                break;
            // High.
            case 2:
                highPresetEvent.Invoke();
                break;
            // Ultra.
            case 3:
                ultraPresetEvent.Invoke();
                break;
            // Custom. Do nothing.
            default:
                break;
        }
    }
}
