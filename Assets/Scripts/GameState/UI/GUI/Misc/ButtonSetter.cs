using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class ButtonSetter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        private string hoverOver;

        /// <summary>
        /// func : () => { Function( parameter ); return null?; }
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void Set(string name, Func<object> func, Sprite Icon = null, string hoverOver = null) {
            GetComponent<Button>().onClick.AddListener(() => { func(); });
            SetUp(name, Icon, hoverOver);
        }

        /// <summary>
        /// func : () => { Function( parameter ); }
        /// </summary>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public void Set(string name, Action func, Sprite Icon = null, string hoverOver = null) {
            GetComponent<Button>().onClick.AddListener(() => { func(); });
            SetUp(name, Icon, hoverOver);
        }

        private void SetUp(string name, Sprite Icon, string hoverOver) {
            if (Icon == null) {
                GetComponentsInChildren<Image>()[1].gameObject.SetActive(false);
                GetComponentInChildren<Text>().text = name;
            }
            else {
                GetComponentsInChildren<Image>()[1].overrideSprite = Icon;
                GetComponentsInChildren<Image>()[1].preserveAspect = true;
                GetComponentInChildren<Text>().gameObject.SetActive(false);
            }
            this.hoverOver = hoverOver;
        }

        public void Interactable(bool interactable) {
            GetComponent<Button>().interactable = interactable;
            Image i = GetComponentsInChildren<Image>()[1];
            Color c = i.color;
            //if interactable go full otherwise half
            c.a = interactable ? 1 : 0.5f;
            i.color = c;
        }

        public void OnPointerEnter(PointerEventData eventData) {
            GameObject.FindObjectOfType<HoverOverScript>().Show(hoverOver);
        }

        public void OnPointerExit(PointerEventData eventData) {
            GameObject.FindObjectOfType<HoverOverScript>().Unshow();
        }
    }
}