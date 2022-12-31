using Andja.Controller;
using Andja.Model;
using Andja.UI.Model;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class EventMessage : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IScrollHandler {
        ScrollRect ParentScroll;
        GameEvent gameEvent;
        BasicInformation Information;
        private Text nameText;
        public Image IconImage;
        ChoiceInformation ChoiceInformation;
        private void Start() {
            ParentScroll = GetComponentInParent<ScrollRect>();
        }
        public void Setup(GameEvent gameEvent) {
            this.gameEvent = gameEvent;
            nameText = GetComponentInChildren<Text>();
            IconImage.sprite = UISpriteController.GetIcon(gameEvent.ID);
            LanguageChanged();
            UILanguageController.Instance.RegisterLanguageChange(LanguageChanged);
        }
        public void Setup(BasicInformation information) {
            Information = information;
            nameText = GetComponentInChildren<Text>();
            LanguageChanged();
            UILanguageController.Instance.RegisterLanguageChange(LanguageChanged);
            IconImage.sprite = UISpriteController.GetIcon(Information.SpriteName);
        }
        public void Setup(ChoiceInformation information) {
            Setup((BasicInformation)information);
            ChoiceInformation = information;
        }
        private void LanguageChanged() {
            if(gameEvent != null) {
                nameText.text = LimitText(gameEvent.Name);
            } else {
                Information.OnLanguageChange();
                nameText.text = LimitText(Information.GetTitle());
            }
        }
        private string LimitText(string text) {
            if (text.Length > 30) {
                text = text.Substring(0, 30) + "...";
            }
            return text;
        }
        public void OnPointerEnter(PointerEventData eventData) {
            if(gameEvent != null) {
                FindObjectOfType<ToolTip>().Show(gameEvent.Name, gameEvent.Description);
            } else {
                FindObjectOfType<ToolTip>().Show(Information.GetTitle(), Information.GetDescription());
            }
        }

        public void OnPointerExit(PointerEventData eventData) {
            FindObjectOfType<ToolTip>().Unshow();
        }

        public void OnPointerClick(PointerEventData eventData) {
            if(eventData.button == PointerEventData.InputButton.Left) {
                if(ChoiceInformation != null) {
                    UIController.Instance.ShowChoiceDialog(ChoiceInformation);
                }
                if (gameEvent != null) {
                    CameraController.Instance.MoveCameraToPosition(gameEvent.GetRealPosition());
                }
                else {
                    CameraController.Instance.MoveCameraToPosition(Information.GetPosition());
                }
            }
            else {
                EventUIManager.Instance.RemoveEvent(this);
            }
        }

        public void OnScroll(PointerEventData eventData) {
            ParentScroll.verticalScrollbar.value += 4 * ParentScroll.scrollSensitivity * Time.deltaTime * eventData.scrollDelta.y;
        }
    }
}