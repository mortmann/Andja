using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.UI.Model {

    public class MiniMapUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDragHandler {
        private RectTransform rectTransform;
        private Vector2 scale;
        private bool isOverMap;
        private Vector2 thisPosition;
        private void Start() {
            rectTransform = GetComponent<RectTransform>();
            CanvasScale.RegisterOnResolutionChange(OnResolutionChange);
        }

        private void Move(Vector2 pressPosition) {
            if (isOverMap == false)
                return;
            CameraController.Instance.MoveCameraToPosition(((pressPosition - thisPosition) * scale));
        }

        public void OnPointerClick(PointerEventData eventData) {
            Move(eventData.position);
        }
        public void OnDrag(PointerEventData eventData) {
            Move(eventData.position);
        }
        public void OnPointerEnter(PointerEventData eventData) {
            isOverMap = true;
        }

        public void OnPointerExit(PointerEventData eventData) {
            isOverMap = false;
        }

        public void OnResolutionChange() {
            thisPosition = rectTransform.anchoredPosition * new Vector2(CanvasScale.Width, CanvasScale.Height);
            scale = new Vector2(
                    ((float)World.Current.Width) / (CanvasScale.Width * rectTransform.sizeDelta.x * rectTransform.localScale.x),
                    ((float)World.Current.Height) / (CanvasScale.Height * rectTransform.sizeDelta.y * rectTransform.localScale.y)
                );
        }
    }
}