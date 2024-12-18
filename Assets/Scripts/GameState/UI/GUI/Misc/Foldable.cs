﻿using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class Foldable : MonoBehaviour {
        public Image Image;
        public Text Title;
        public GameObject TitleGO;
        public RectTransform Content;
        private EventTrigger triggers;
        private bool isActive;

        private void Start() {
            triggers = GetComponentInChildren<EventTrigger>();
            EventTrigger.Entry click = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            click.callback.AddListener((data) => {
                OnMouseClick();
            });
            triggers.triggers.Add(click);
            if (GetComponentInParent<ScrollRect>() != null) {
                ScrollRect sr = GetComponentInParent<ScrollRect>();
                EventTrigger.Entry scroll = new EventTrigger.Entry {
                    eventID = EventTriggerType.Scroll
                };
                scroll.callback.AddListener((data) => {
                    sr.verticalScrollbar.value += 4 * sr.scrollSensitivity * Time.deltaTime * ((PointerEventData)data).scrollDelta.y;
                });
                triggers.triggers.Add(scroll);
            }
        }

        public void Set(string title) {
            Title.text = title;
        }

        private void OnMouseClick() {
            if (isActive == false)
                return;
            Content.gameObject.SetActive(!Content.gameObject.activeSelf);
        }

        public void Add(GameObject go) {
            go.transform.SetParent(Content, false);
            //Some how the Mine-Structure Content always is bugged out - this is an fix to that
            LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
        }

        internal void Check() {
            foreach (Transform t in Content) {
                if (t.gameObject.activeSelf) {
                    isActive = true;
                    GetComponent<CanvasGroup>().alpha = 1;
                    Content.gameObject.SetActive(true);
                    return;
                }
            }
            GetComponent<CanvasGroup>().alpha = 0.5f;
            Content.gameObject.SetActive(false);
            isActive = false;
        }
    }
}