using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GS_Selectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, ISubmitHandler, ISelectHandler {

    // The colors for the states for this button.
    public Color normalColor;
    Color highlightColorInitial = new Color32(120,120,120,120);
    Color highlightColorFadeTo = new Color32(80,80,80,80);
    public float fadeSpeed = 0.75f;
    float t;
    bool fadeDown = true;

    Button button;
    Slider slider;

    Image image;
    Vector3 lastPointerPositon;

    void Start() {
        image = GetComponent<Image>();

        button = GetComponent<Button>();
        slider = GetComponent<Slider>();

        if (slider != null) {
            slider.onValueChanged.AddListener(delegate { OnSliderValueChange(); });
        }
    }

    /**
     * All the fluff we're doing in the events below should allow the mouse
     * and the keyobard to interact properly together when switching between
     * mouse and keyboard for navigating our menus. If anyone knows of an easier
     * way please let me know. :p
     */
    void Update() {
        // If we're currently hovering over the button and we moved our mouse
        // switch the selection to this button.
		if (MenuController.instance.currentMouseOverGameObject == gameObject && Input.mousePosition != lastPointerPositon) {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        // Set the color of the button depending on the selected state.
        if (EventSystem.current.currentSelectedGameObject == gameObject) {
            if (t <= 1f) {
                t += Time.deltaTime / fadeSpeed;
                if (fadeDown) {
                    image.color = Color.Lerp(highlightColorInitial, highlightColorFadeTo, t);
                }
                else {
                    image.color = Color.Lerp(highlightColorFadeTo, highlightColorInitial, t);
                }

            }
            else {
                t = 0;
                fadeDown = !fadeDown;
            }
        }
        else {
            image.color = normalColor;
        }

        // Save the current mouse position for next frame so we can compare it.
        lastPointerPositon = Input.mousePosition;
    }

    /**
     * Handle hovering over the button with the mouse.
     */
    public void OnPointerEnter(PointerEventData eventData) {
		MenuController.instance.currentMouseOverGameObject = gameObject;
        EventSystem.current.SetSelectedGameObject(gameObject);
    }
    public void OnPointerExit(PointerEventData eventData) {
		MenuController.instance.currentMouseOverGameObject = null;
    }

    /**
     * Handle navigating to the button with the arrow keys on the keyboard.
     */
    public void OnSelect(BaseEventData eventData) {
        MenuAudioManager.instance.PlayHoverSound();
    }

    /**
     * Handle clicking the button with the mouse.
     */
    public void OnPointerUp(PointerEventData eventData) {
        // We only need this for buttons.
        if (button == null) {
            return;
        }

        // Set the selected game object in the event system to this button so
        // it works properly if we switch to the keyboard.
		MenuController.instance.currentButton = button;
        MenuAudioManager.instance.PlayClickSound();
    }

    /**
     * Handle pressing ENTER or SPACE on the keyboard.
     */
    public void OnSubmit(BaseEventData eventData) {
        // We only need this for buttons.
        if (button == null) {
            return;
        }

		MenuController.instance.currentButton = button;
        MenuAudioManager.instance.PlayClickSound();
    }

    /**
     * Play the 'tick' sound when we move a slider.
     */
    public void OnSliderValueChange() {
//        MenuAudioManager.instance.PlaySliderSound();
    }
}
