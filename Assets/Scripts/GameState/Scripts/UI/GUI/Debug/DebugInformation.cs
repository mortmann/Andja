using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class DebugInformation : MonoBehaviour {
    public GameObject buttons;
    public GameObject contentList;
    public GameObject debugdataprefab;
    public GameObject debuglistdataprefab;
    object currentObject;
    private Vector2 dragOffset;

    public void OnEnable() {
        foreach (Transform t in contentList.transform) {
            Destroy(t.gameObject);
        }
    }
    public void Show(object obj) {
        transform.SetParent(UIController.Instance.mainCanvas.transform);
        transform.position = new Vector3(Screen.width / 2, Screen.height / 2);

        currentObject = obj;
        List<FieldInfo> all = new List<FieldInfo>(obj.GetType().GetFields()); //public fields
        all.AddRange(obj.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)); //private,protected fields
        foreach (FieldInfo field in all) {
            if (field.FieldType.GetInterface(nameof(IEnumerable)) == null && field.GetType().IsArray == false
                || field.FieldType == typeof(string)) {
                GameObject fieldGO = Instantiate(debugdataprefab);
                fieldGO.transform.SetParent(contentList.transform);
                fieldGO.GetComponent<DebugDataUI>().SetData(field, obj);
            }
            else
            if (field.FieldType.GetInterface(nameof(IEnumerable)) != null || field.GetType().IsArray) {
                GameObject fieldGO = Instantiate(debuglistdataprefab);
                fieldGO.transform.SetParent(contentList.transform);
                fieldGO.GetComponent<DebugListDataUI>().SetData(field, obj);
            }
            else {
                Debug.LogWarning("!?!?!?");
            }
        }
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry drag = new EventTrigger.Entry {
            eventID = EventTriggerType.Drag
        };
        EventTrigger.Entry beginDrag = new EventTrigger.Entry {
            eventID = EventTriggerType.BeginDrag
        };
        beginDrag.callback.AddListener((data) => { OnBeginDragDelegate((PointerEventData)data); });
        drag.callback.AddListener((data) => { OnDragDelegate((PointerEventData)data); });
        trigger.triggers.Add(beginDrag);
        trigger.triggers.Add(drag);
        RectTransform rectTransform = GetComponent<RectTransform>();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        transform.position = new Vector3(
            Mathf.Clamp(Screen.width / 2 - rectTransform.sizeDelta.x / 2, 0,Screen.width),
            Mathf.Clamp(Screen.height / 2 + rectTransform.sizeDelta.y / 2, 0, Screen.height)
        );

    }

    private void OnBeginDragDelegate(PointerEventData data) {
        dragOffset = new Vector2(transform.position.x, transform.position.y) - data.pressPosition;
    }

    private void OnDragDelegate(PointerEventData data) {
        transform.position = data.position + (dragOffset);
    }

    public void Reload() {
        foreach (Transform t in transform) {
            if (t.gameObject == buttons.gameObject)
                continue;
            Destroy(t.gameObject);
        }
        Show(currentObject);
    }
    // Update is called once per frame
    void Update() {

    }
    public void Close() {
        Destroy(this.gameObject);
    }
}
