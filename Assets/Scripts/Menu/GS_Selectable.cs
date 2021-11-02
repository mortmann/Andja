using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class GS_Selectable : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, ISubmitHandler, ISelectHandler {

        // The colors for the states for this button.
        public Color normalColor;

        private Color highlightColorInitial = new Color32(120, 120, 120, 120);
        private Color highlightColorFadeTo = new Color32(80, 80, 80, 80);
        public float fadeSpeed = 0.75f;
        private float t;
        private bool fadeDown = true;

        private Button button;

        private Image image;
        private Vector3 lastPointerPositon;

        private void Start() {
            image = GetComponent<Image>();

            button = GetComponent<Button>();

        }

        private void Update() {
            // If we're currently hovering over the button and we moved our mouse
            // switch the selection to this button.
            if (MenuController.Instance.currentMouseOverGameObject == gameObject && Input.mousePosition != lastPointerPositon) {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            if (button != null && button.interactable == false) {
                return;
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
            MenuController.Instance.currentMouseOverGameObject = gameObject;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData) {
            MenuController.Instance.currentMouseOverGameObject = null;
        }

        /**
         * Handle navigating to the button with the arrow keys on the keyboard.
         */

        public void OnSelect(BaseEventData eventData) {
            MenuAudioManager.Instance.PlayHoverSound();
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
            MenuController.Instance.currentButton = button;
            MenuAudioManager.Instance.PlayClickSound();
        }

        /**
         * Handle pressing ENTER or SPACE on the keyboard.
         */

        public void OnSubmit(BaseEventData eventData) {
            // We only need this for buttons.
            if (button == null) {
                return;
            }

            MenuController.Instance.currentButton = button;
            MenuAudioManager.Instance.PlayClickSound();
        }

    }
}