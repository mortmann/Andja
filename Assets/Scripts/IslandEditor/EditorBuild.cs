using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.Editor {

    /// <summary>
    /// TODO: change building over to structure names!
    /// </summary>
    public class EditorBuild : MonoBehaviour {
        public GameObject toggleListItem;
        public GameObject sliderListItem;
        public GameObject listItem;

        public GameObject BuildingSelectContent;
        public GameObject BuildingSettingsContent;

        private void Start() {
            bool first = true;
            foreach (string item in PrototypController.Instance.StructurePrototypes.Keys) {
                GameObject g = GameObject.Instantiate(listItem);
                g.transform.SetParent(BuildingSelectContent.transform);
                g.GetComponentInChildren<Text>().text = PrototypController.Instance.StructurePrototypes[item].SpriteName;
                string temp = item;
                EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry {
                    eventID = EventTriggerType.Select,
                    callback = new EventTrigger.TriggerEvent()
                };
                entry.callback.AddListener((data) => { OnBuildingSelect(temp); });
                eventTrigger.triggers.Add(entry);
                if (first) {
                    OnBuildingSelect(temp);
                    first = false;
                }
            }
        }

        public void OnBuildingSelect(string id) {
            EditorController.Instance.changeMode = ChangeMode.Structure;
            EditorController.Instance.SetStructure(id);
            Structure str = PrototypController.Instance.StructurePrototypes[id];

            foreach (Transform item in BuildingSettingsContent.transform) {
                GameObject.Destroy(item.gameObject);
            }
            EditorController.Instance.ResetSetStructure();
            Type strType = str.GetType();
            HashSet<FieldInfo> all = new HashSet<FieldInfo>(strType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            foreach (FieldInfo fi in all) {
                EditorSetFieldAttribute edsfa = fi.GetCustomAttribute<EditorSetFieldAttribute>();
                if (edsfa == null)
                    continue;
                if (IsNumeric(fi.FieldType)) {
                    GameObject g = GameObject.Instantiate(sliderListItem);
                    g.transform.SetParent(BuildingSettingsContent.transform);
                    g.GetComponentInChildren<Text>().text = fi.Name;
                    Slider s = g.GetComponentInChildren<Slider>();
                    s.minValue = edsfa.minValue;
                    if (edsfa.minValueName != null)
                        s.minValue = (int)strType.GetProperty(edsfa.minValueName)?.GetValue(str);
                    s.maxValue = edsfa.maxValue;
                    if (edsfa.maxValueName != null)
                        s.maxValue = (int)strType.GetProperty(edsfa.maxValueName)?.GetValue(str);

                    s.wholeNumbers = edsfa.wholeNumbers;
                    s.onValueChanged.AddListener(x => s.GetComponentInChildren<Text>().text = "" + x);
                    EditorController.Instance.SetStructureVariablesList.Add(
                            x => fi.SetValue(x, System.Convert.ChangeType(s.value, fi.FieldType))
                    );
                    Action<Structure> random = y => fi.SetValue(y, System.Convert.ChangeType(
                                                        UnityEngine.Random.Range(s.minValue, s.maxValue),
                                                        fi.FieldType));
                    g.GetComponentInChildren<Toggle>().onValueChanged.AddListener((x) => {
                        if (x) {
                            EditorController.Instance.SetStructureVariablesList.Add(random);
                        }
                        else {
                            EditorController.Instance.SetStructureVariablesList.Remove(random);
                        }
                    });
                    //trigger listener
                    s.value = s.maxValue;
                    s.value = s.minValue;
                }
                else
                if (fi.FieldType == typeof(bool)) {
                    GameObject g = GameObject.Instantiate(toggleListItem);
                    g.transform.SetParent(BuildingSettingsContent.transform);
                    g.GetComponentInChildren<Text>().text = fi.Name;
                    g.GetComponentInChildren<Toggle>().onValueChanged.AddListener(x => fi.SetValue(str, x));
                }
            }
        }

        private static readonly HashSet<Type> NumericTypes = new HashSet<Type> {
        typeof(int),  typeof(double),  typeof(decimal),
        typeof(long), typeof(short),   typeof(sbyte),
        typeof(byte), typeof(ulong),   typeof(ushort),
        typeof(uint), typeof(float)
    };

        public static bool IsNumeric(Type myType) {
            return NumericTypes.Contains(Nullable.GetUnderlyingType(myType) ?? myType);
        }
    }

    public class EditorSetFieldAttribute : Attribute {
        public string maxValueName;
        public float maxValue = 1000;
        public string minValueName;
        public float minValue = 0;
        public bool wholeNumbers = true;
        public Func<object> change;
    }
}