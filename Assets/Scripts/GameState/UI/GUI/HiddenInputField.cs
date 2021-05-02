using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class HiddenInputField : MonoBehaviour, IPointerClickHandler {
        public InputField NameText;
        bool CanBeChanged;
        bool RightClick = false;
        Action LeftClick;
        public void Set(string name, UnityAction<string> OnNameEdit, bool CanBeChanged = true, 
                    bool rightClick = false, Action leftClick = null) {
            Set(name);
            LeftClick = leftClick;
            RightClick = rightClick;
            //Make the Name editable
            NameText.onEndEdit.AddListener(OnNameEdit);
            NameText.onEndEdit.AddListener(EndText);
            this.CanBeChanged = CanBeChanged;
        }
        public void Set(int name, UnityAction<int> OnNameEdit, bool CanBeChanged = true,
                    Func<int> MinValue = null, Func<int> MaxValue = null) {
            Set(name + "");
            this.CanBeChanged = CanBeChanged;
            NameText.onValueChanged.AddListener(x => {
                if(MinValue!=null) {
                    if (int.Parse(x) < MinValue()) {
                        NameText.text = MinValue().ToString();
                    }
                    if (int.Parse(x) > MaxValue()) {
                        NameText.text = MaxValue().ToString();
                    }
                }
            });
            NameText.onEndEdit.AddListener((x)=> { OnNameEdit.Invoke(int.Parse(x)); });
            NameText.contentType = InputField.ContentType.IntegerNumber;

        }

        public void Set(string name) {
            NameText.readOnly = true;
            NameText.interactable = false;
            NameText.text = name;
        }
        private void EndText(string end) {
            NameText.readOnly = true;
            NameText.interactable = false;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (CanBeChanged == false || NameText.readOnly == false)
                return;
            if (eventData.button == PointerEventData.InputButton.Left)
                LeftClick?.Invoke();
            if (eventData.button != PointerEventData.InputButton.Right && RightClick)
                return;
            NameText.readOnly = false;
            NameText.interactable = true;
            EventSystem.current.SetSelectedGameObject(null);
            NameText.Select();
        }
    }
}