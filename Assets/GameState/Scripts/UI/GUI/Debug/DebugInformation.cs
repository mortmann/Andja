using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class DebugInformation : MonoBehaviour {
    public GameObject buttons;

    public GameObject debugdataprefab;
    public GameObject debuglistdataprefab;
    object currentObject;
    public void OnEnable() {
        foreach(Transform t in transform) {
            if (t == buttons.transform)
                continue;
            Destroy(t.gameObject);
        }
    }
    public void Show(object obj) {
        currentObject = obj;
        foreach (FieldInfo field in obj.GetType().GetFields()) {
            if (field.FieldType.GetInterface(nameof(IEnumerable)) == null && field.GetType().IsArray == false) {
                GameObject fieldGO = Instantiate(debugdataprefab);
                fieldGO.transform.SetParent(this.transform);
                fieldGO.GetComponent<DebugDataUI>().SetData(field, obj);
            }
            else
            if (field.FieldType.GetInterface(nameof(IEnumerable)) != null || field.GetType().IsArray) {
                GameObject fieldGO = Instantiate(debuglistdataprefab);
                fieldGO.transform.SetParent(this.transform);
                fieldGO.GetComponent<DebugListDataUI>().SetData(field, obj);
            }
            else {
                Debug.LogWarning("!?!?!?");
            }
        }
    }
    public void Reload() {
        foreach (Transform t in transform) {
            Destroy(t.gameObject);
        }
        Show(currentObject);
    }
	// Update is called once per frame
	void Update () {
		
	}
    public void Close() {
        Destroy(this);
    }
}
