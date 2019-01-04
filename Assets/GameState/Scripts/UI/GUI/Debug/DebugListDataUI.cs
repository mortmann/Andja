using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class DebugListDataUI : MonoBehaviour {

    public Text nameText;
    public Text valueText;
    public GameObject debugdataprefab;
    public GameObject listgameObject;

    FieldInfo Field;
    object shownObject;
    List<object> childs;

    public void SetData(FieldInfo field, object obj) {
        if (field.FieldType.GetInterface(nameof(IEnumerable)) == null && field.GetType().IsArray == false) {
            return;
        }
        foreach(Transform t in listgameObject.transform) {
            Destroy(t.gameObject);
        }
        nameText.text = field.Name;
        Field = field;
        shownObject = obj;
        Debug.Log(Field.);
        if(Field.MemberType.GetType() == typeof(object)) {
            Debug.Log("jap");
        }
        SetChilds<float>();

        ToggleListDetails();
    }

    private void SetChilds<T>() {
        if (Field.GetValue(shownObject) == null)
            return;
        foreach (Transform t in listgameObject.transform) {
            Destroy(t.gameObject);
        }
        Debug.Log(Field.Name + " " + Field.GetValue(shownObject));

        if (Field.FieldType.GetInterface(nameof(IEnumerable)) != null)
            childs = new List<object>((IEnumerable<object>)Field.GetValue(shownObject));
        else
                if (Field.GetType().IsArray)
            childs = new List<object>((object[])Field.GetValue(shownObject));
        int i = 0;
        foreach (object o in childs) {
            GameObject fieldGO = Instantiate(debugdataprefab);
            fieldGO.transform.SetParent(listgameObject.gameObject.transform);
            fieldGO.GetComponent<DebugDataUI>().SetData(i + ".", o);
            i++;
        }
    }

    public void Update() {
        valueText.text = childs?.Count.ToString();
        //if (childs == null)
        //    SetChilds(); //maybe after some time?
    }
    public void ToggleListDetails() {
        listgameObject.SetActive(!listgameObject.activeSelf);
    }
}
