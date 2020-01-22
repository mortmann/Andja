using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class StructureBuildUI : MonoBehaviour {
    public static StructureBuildUI Instance { get; protected set; }
    public GameObject mouseOverPrefab;
    public Structure structure;
    // Use this for initialization
    public void Show(Structure str, bool hoverOver = true) {
        this.structure = str;
        if (IconSpriteController.HasIcon(str.ID) == false) {
            GetComponentInChildren<Text>().text = str.SpriteName;
            if(GetComponentsInChildren<Image>().Length>1)
                GetComponentsInChildren<Image>()[1].gameObject.SetActive(false);
        }
        else {
            GetComponentInChildren<Text>()?.gameObject.SetActive(false);
            GetComponentsInChildren<Image>()[1].overrideSprite = IconSpriteController.GetIcon(str.ID);
        }

        EventTrigger trigger = GetComponent<EventTrigger>();
        if (hoverOver) {
            EventTrigger.Entry enter = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerEnter
            };
            enter.callback.AddListener((data) => {
                
                OnMouseEnter();
            });
            trigger.triggers.Add(enter);

            trigger.triggers.Add(enter);
            EventTrigger.Entry exit = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener((data) => {
                OnMouseExit();
            });
            trigger.triggers.Add(exit);
        }


        EventTrigger.Entry dragStart = new EventTrigger.Entry {
            eventID = EventTriggerType.BeginDrag
        };
        dragStart.callback.AddListener((data) => {
            OnDragStart(((PointerEventData)data).pressPosition);
        });
        trigger.triggers.Add(dragStart);


        EventTrigger.Entry dragStop = new EventTrigger.Entry {
            eventID = EventTriggerType.EndDrag
        };
        dragStop.callback.AddListener((data) => {
            OnDragEnd();
        });
        trigger.triggers.Add(dragStop);

        if (GetComponentInParent<ScrollRect>() != null) {
            EventTrigger.Entry scroll = new EventTrigger.Entry {
                eventID = EventTriggerType.Scroll
            };
            scroll.callback.AddListener((data) => {
                ScrollRect sr = GetComponentInParent<ScrollRect>();
                sr.verticalScrollbar.value += sr.scrollSensitivity * Time.deltaTime * ((PointerEventData)data).scrollDelta.y;
            });
            trigger.triggers.Add(scroll);
        }
    }
    public void OnMouseEnter() {
        GameObject.FindObjectOfType<HoverOverScript>().Show(structure.Name);
    }
    public void OnMouseExit() {
        //TODO: reset hovertime better
        GameObject.FindObjectOfType<HoverOverScript>().Unshow();

    }
    public void OnDragStart(Vector3 position) {
        //RectTransformUtility.ScreenPointToLocalPointInRectangle(GetComponent<RectTransform>(), Input.mousePosition, Camera.main, out offset);
        UIController.Instance.SetDragAndDropBuild(this.gameObject, transform.InverseTransformPoint(position));
    }
    public void OnDragEnd() {
        UIController.Instance.StopDragAndDropBuild();
    }

}
