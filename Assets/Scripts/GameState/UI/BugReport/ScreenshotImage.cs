using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.Utility {
    public class ScreenshotImage : MonoBehaviour, IPointerClickHandler {
        public Image image;
        public void OnPointerClick(PointerEventData eventData) {
            StartCoroutine(TakeScreenShot());
        }

        private IEnumerator TakeScreenShot() {
            UI.BugReportCanvas bug = FindObjectOfType<UI.BugReportCanvas>();
            bug.ShowUI(false);
            yield return new WaitForEndOfFrame();
            Texture2D thumb = ScreenCapture.CaptureScreenshotAsTexture();
            thumb.Apply();
            bug.ShowUI(true);
            image.sprite = Sprite.Create(thumb, new Rect(0, 0, thumb.width, thumb.height), new Vector2(0, 0));
            Color c = image.color;
            c.a = 255;
            image.color = c;
        }

        void Start() {
            GetComponentInChildren<Button>().onClick.AddListener(DeleteImage);
        }
        public Texture2D GetImage() {
            if (image.sprite == null)
                return null;
            return image.sprite.texture;
        }
        public void DeleteImage() {
            image.sprite = null;
            Color c = image.color;
            c.a = 0;
            image.color = c;
        }
    }

}