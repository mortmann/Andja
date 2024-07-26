using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Menu {
    public class GS_SelectableDropdown : Dropdown, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, ISubmitHandler, ISelectHandler {
        bool open;

        RectTransform contentPanel;
        RectTransform selectedRectTransform;
        GameObject lastSelected;
        Color normalColor;
        Color highlightColorInitial = new Color32(120, 120, 120, 120);
        Color highlightColorFadeTo = new Color32(80, 80, 80, 80);
        private Vector3 lastPointerPositon;
        private float t;
        private bool fadeDown = true;
        protected override void Awake() {
            //this needs to be done because as a subclass of a unity class the inspector does not 
            //show public variables so instead of making a custom inspector class to make it work
            //here just use the existing ColorBlock to work with this
            if(transition != Transition.ColorTint) {
                Debug.LogError("GS_SelectableDropdown depends on ColorBlock of the transition to setup colors.");
                return;
            }
            highlightColorInitial = colors.highlightedColor;
            highlightColorFadeTo = colors.selectedColor;
            normalColor = colors.normalColor;
            transition = Transition.None; // then disable the transition
            onValueChanged.AddListener((f) => MenuAudioManager.Instance.PlayClickSound());
        }
        public override void OnPointerEnter(PointerEventData eventData) {
            base.OnPointerEnter(eventData);
            MenuController.Instance.currentMouseOverGameObject = gameObject;
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public override void OnPointerExit(PointerEventData eventData) {
            base.OnPointerExit(eventData);
            MenuController.Instance.currentMouseOverGameObject = null;
        }
        protected override GameObject CreateDropdownList(GameObject template) {
            open = true;
            return base.CreateDropdownList(template);
        }
        protected override void DestroyDropdownList(GameObject dropdownList) {
            base.DestroyDropdownList(dropdownList);
            open = false;
        }
        private void Update() {
            if (open) {
                if (contentPanel == null)
                    contentPanel = GetComponentInChildren<ScrollRect>().content;
                GameObject selected = EventSystem.current.currentSelectedGameObject;
                if (selected == null) {
                    return;
                }
                if (selected.transform.parent != contentPanel.transform) {
                    return;
                }
                if (selected == lastSelected) {
                    return;
                }
                selectedRectTransform = selected.GetComponent<RectTransform>();
                contentPanel.anchoredPosition = new Vector2(contentPanel.anchoredPosition.x, -(selectedRectTransform.localPosition.y) - (selectedRectTransform.rect.height / 2));

                lastSelected = selected;
            }
            // If we're currently hovering over the button and we moved our mouse
            // switch the selection to this button.
            if (MenuController.Instance.currentMouseOverGameObject == gameObject && Input.mousePosition != lastPointerPositon) {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            if (interactable == false) {
                return;
            }
            // Set the color of the button depending on the selected state.
            if (EventSystem.current.currentSelectedGameObject == gameObject) {
                if (t <= 1f) {
                    t += Time.deltaTime / this.alphaFadeSpeed;
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
         * Handle navigating to the button with the arrow keys on the keyboard.
         */

        public override void OnSelect(BaseEventData eventData) {
            base.OnSelect(eventData);
            MenuAudioManager.Instance.PlayHoverSound();
        }

        /**
         * Handle clicking the button with the mouse.
         */

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);

        }

        /**
         * Handle pressing ENTER or SPACE on the keyboard.
         */

        public override void OnSubmit(BaseEventData eventData) {
            base.OnSubmit(eventData);
        }
    }
}

