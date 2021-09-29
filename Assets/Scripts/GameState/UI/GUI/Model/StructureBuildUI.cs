using Andja.Controller;
using Andja.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class StructureBuildUI : MonoBehaviour, 
                    IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IScrollHandler {
        public static StructureBuildUI Instance { get; protected set; }

        private bool hoverOver;
        public Structure structure;

        public void Show(Structure str, bool hoverOver = true) {
            this.hoverOver = hoverOver;
            this.structure = str;
            if (UISpriteController.HasIcon(str.ID) == false) {
                GetComponentInChildren<Text>().text = str.SpriteName;
                if (GetComponentsInChildren<Image>().Length > 1)
                    GetComponentsInChildren<Image>()[1].gameObject.SetActive(false);
            }
            else {
                GetComponentInChildren<Text>()?.gameObject.SetActive(false);
                GetComponentsInChildren<Image>()[1].overrideSprite = UISpriteController.GetIcon(str.ID);
            }

        }

        public void OnBeginDrag(PointerEventData eventData) {
            UIController.Instance.SetDragAndDropBuild(this.gameObject, transform.InverseTransformPoint(eventData.pressPosition));
        }

        public void OnEndDrag(PointerEventData eventData) {
            UIController.Instance.StopDragAndDropBuild();
        }

        public void OnPointerEnter(PointerEventData eventData) {
            if (hoverOver == false)
                return;
            FindObjectOfType<ToolTip>().Show(structure, PlayerController.CurrentPlayer.HasStructureUnlocked(structure.ID));
        }

        public void OnPointerExit(PointerEventData eventData) {
            FindObjectOfType<ToolTip>().Unshow();
        }

        public void OnScroll(PointerEventData eventData) {
            ScrollRect sr = GetComponentInParent<ScrollRect>();
            if(sr != null) {
                sr.verticalScrollbar.value += sr.scrollSensitivity * Time.deltaTime * eventData.scrollDelta.y;
            }
        }
    }
}