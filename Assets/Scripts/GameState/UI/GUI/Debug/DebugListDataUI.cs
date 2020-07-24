using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    Func<string> GetCount;
    bool isNull = false;
    public void SetData(FieldInfo field, object obj) {
        if (field.FieldType.GetInterface(nameof(IEnumerable)) == null && field.GetType().IsArray == false) {
            return;
        }
        foreach (Transform t in listgameObject.transform) {
            Destroy(t.gameObject);
        }
        nameText.text = field.Name;
        Field = field;
        shownObject = obj;
        MakeList();
        ToggleListDetails();
    }

    private void MakeList() {
        Type elementType = Field.FieldType.GetElementType();
        if (elementType == null)
            elementType = GetListType(Field.FieldType);// Field.FieldType.GetGenericArguments()[0];

        switch (Type.GetTypeCode(elementType)) {
            case TypeCode.Boolean:
                SetChilds<bool>();
                break;
            case TypeCode.Single:
                SetChilds<float>();
                break;
            case TypeCode.Byte:
                SetChilds<byte>();
                break;
            case TypeCode.Char:
                SetChilds<char>();
                break;
            case TypeCode.Decimal:
                SetChilds<decimal>();
                break;
            case TypeCode.Double:
                SetChilds<double>();
                break;
            case TypeCode.Int32:
                SetChilds<int>();
                break;
            case TypeCode.Object:
                SetChilds<object>();
                break;
            case TypeCode.String:
                SetChilds<string>();
                break;
            default:
                Debug.LogError("Was to lazy to add this one: " + Type.GetTypeCode(elementType));
                break;
        }
    }

    private void SetChilds<T>() {
        if (Field.GetValue(shownObject) == null) {
            isNull = true;
            valueText.text = "";
            return;
        }
        foreach (Transform t in listgameObject.transform) {
            Destroy(t.gameObject);
        }
        List<T> childs = null;
        List<string> strings = new List<string>();

        if (Field.FieldType.GetInterface(nameof(IEnumerable)) != null)
            try {
                if(Field.FieldType.GetInterface(nameof(IDictionary)) != null) {
                    IDictionary dic = ((IDictionary)Field.GetValue(shownObject));
                    foreach (object o in dic.Keys) {
                        strings.Add(o.ToString() + " = " + dic[o].ToString());
                    }
                } else {
                    childs = new List<T>((IEnumerable<T>)Field.GetValue(shownObject));
                }
            } catch {
                Debug.LogError("Cant show this?" + Field.GetValue(shownObject));
            }
        else
                if (Field.GetType().IsArray)
            childs = new List<T>((T[])Field.GetValue(shownObject));
        int i = 0;

        if (childs != null) {
            foreach (object o in (IList)childs) {
                GameObject fieldGO = Instantiate(debugdataprefab);
                fieldGO.transform.SetParent(listgameObject.gameObject.transform, false);
                fieldGO.GetComponent<DebugDataUI>().SetData(i + ".", o);
                i++;
            }
            valueText.text = ((IList)childs)?.Count.ToString();
            GetCount = () => ((IList)childs)?.Count.ToString();
        }
        if (strings != null) {
            foreach (string o in strings) {
                GameObject fieldGO = Instantiate(debugdataprefab);
                fieldGO.transform.SetParent(listgameObject.gameObject.transform, false);
                fieldGO.GetComponent<DebugDataUI>().SetData(i + ".", o);
                i++;
            }
            valueText.text = (strings)?.Count.ToString();
            GetCount = () => (strings)?.Count.ToString();
        }
    }

    public void Update() {
        if (GetCount != null)
            valueText.text = GetCount();
        if (isNull)
            MakeList(); //maybe after some time?
    }
    public void ToggleListDetails() {
        listgameObject.SetActive(!listgameObject.activeSelf);
    }

    static Type GetListType(Type enumerable) {
        var enumerableType = enumerable
            .GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .First();
        return enumerableType.GetGenericArguments()[0];
    }
}
