using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class NameInputField : MonoBehaviour, IPointerClickHandler {
        public InputField NameText;

        public void SetName(string name, UnityAction<string> OnNameEdit) {
            //Make the Name editable
            NameText.onEndEdit.AddListener(OnNameEdit);
            NameText.onEndEdit.AddListener(EndText);
            NameText.readOnly = true;
            NameText.interactable = false;
            NameText.text = name;
        }

        private void EndText(string end) {
            NameText.readOnly = true;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (NameText.readOnly == false)
                return;
            NameText.readOnly = false;
            NameText.interactable = true;
            NameText.Select();
        }
    }
}