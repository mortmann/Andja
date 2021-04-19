using Andja.Model;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.Editor.UI {

    public class TileTypeSelect : MonoBehaviour {
        public GameObject prefabListItem;
        public GameObject content;
        private TypeGraphicsSelect tgs;

        // Use this for initialization
        private void Start() {
            tgs = GameObject.FindObjectOfType<TypeGraphicsSelect>();
            foreach (TileType item in Enum.GetValues(typeof(TileType))) {
                GameObject g = GameObject.Instantiate(prefabListItem);
                g.transform.SetParent(content.transform);
                g.GetComponentInChildren<Text>().text = item.ToString();
                TileType temp = item;
                EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
                EventTrigger.Entry entry = new EventTrigger.Entry {
                    eventID = EventTriggerType.Select,
                    callback = new EventTrigger.TriggerEvent()
                };
                entry.callback.AddListener((data) => { OnSelect(temp); });
                eventTrigger.triggers.Add(entry);
            }
            OnSelect(TileType.Dirt);
        }

        public void OnSelect(TileType item) {
            EditorController.Instance.selectedTileType = item;
            tgs.ChangeType(item);
        }

        // Update is called once per frame
        private void Update() {
        }
    }
}