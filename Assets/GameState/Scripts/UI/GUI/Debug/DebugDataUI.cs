using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class DebugDataUI : MonoBehaviour {
    public Text nameText;
    public Text valueText;
    public Button showObject;
    public GameObject debugInformationPrefab;

    FieldInfo Field;
    object shownObject;

    public void SetData(FieldInfo field, object obj) {
        nameText.text = field.Name;
        Field = field;
        shownObject = obj;
    }
    public void SetData(string name, object obj) {
        nameText.text = name;
        shownObject = obj;
        showObject.gameObject.SetActive(true);
    }
    public void Update() {
        if (Field != null)
            valueText.text = Field.GetValue(shownObject)?.ToString();
        else
            valueText.text = shownObject?.ToString();
    }

    public void OnShowObject() {
        GameObject g = Instantiate(debugInformationPrefab);
        g.GetComponent<DebugInformation>().Show(shownObject);
        g.transform.SetParent(transform);
    }

}
