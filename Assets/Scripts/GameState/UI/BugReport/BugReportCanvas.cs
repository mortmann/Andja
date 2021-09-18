using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.UI {
    public class BugReportCanvas : MonoBehaviour {
        void Start() {
            GetComponent<Canvas>().worldCamera = Camera.main;
            transform.GetChild(0).gameObject.SetActive(false);
            new InputHandler();
        }
        void Update() {
            if (InputHandler.GetButtonDown(InputName.BugReport)) {
                transform.GetChild(0).gameObject.SetActive(!transform.GetChild(0).gameObject.activeSelf);
            }
        }
        internal void ShowUI(bool show) {
            transform.GetChild(0).GetComponent<CanvasGroup>().alpha = show ? 1 : 0;
        }
    }
}