using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;


/// <summary>
/// TODO: change building over to structure names!
/// </summary>
public class EditorBuild : MonoBehaviour {
    public GameObject prefabListItem;
    public GameObject BuildingSelectContent;
    public GameObject BuildingSettingsContent;

    // Use this for initialization
    void Start() {
        bool first = true;
        foreach (string item in PrototypController.Instance.StructurePrototypes.Keys) {
            GameObject g = GameObject.Instantiate(prefabListItem);
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
            if (first)
                OnBuildingSelect(temp);
        }

    }

    public void OnBuildingSelect(string id) {
        EditorController.Instance.changeTileType = false;
        EditorController.Instance.SetStructure(id);
        if (PrototypController.Instance.StructurePrototypes[id] is GrowableStructure == false) {
            return;
        }
        GrowableStructure gr = PrototypController.Instance.StructurePrototypes[id] as GrowableStructure;
        int ages = gr.AgeStages;
        foreach (Transform item in BuildingSettingsContent.transform) {
            GameObject.Destroy(item.gameObject);
        }
        for (int i = 0; i <= ages; i++) {
            GameObject g = GameObject.Instantiate(prefabListItem);
            g.transform.SetParent(BuildingSettingsContent.transform);
            g.GetComponentInChildren<Text>().text = i.ToString();
            int temp = i;
            EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry {
                eventID = EventTriggerType.Select,
                callback = new EventTrigger.TriggerEvent()
            };
            entry.callback.AddListener((data) => { OnAgeSelect(temp); });
            eventTrigger.triggers.Add(entry);
        }
    }
    public void OnAgeSelect(int age) {
        EditorController.Instance.SetAge(age);
        //		EditorStructureSpriteController.Instance.growableLevel = age;
    }
}
