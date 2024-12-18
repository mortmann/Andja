﻿using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.Editor.UI {

    public class TypeGraphicsSelect : MonoBehaviour {
        public GameObject prefabListItem;
        public GameObject content;
        public Dictionary<string, List<string>> typeTotileSpriteNames = new Dictionary<string, List<string>>();
        public Dictionary<string, List<GameObject>> typeToGameObjects;
        private TileType currentSelected;

        // Use this for initialization
        private void OnEnable() {
            typeToGameObjects = new Dictionary<string, List<GameObject>>();
            LoadSprites();
            foreach (string type in typeTotileSpriteNames.Keys) {
                List<GameObject> gos = new List<GameObject>();
                int number = 0;
                if (typeTotileSpriteNames[type] == null || typeTotileSpriteNames[type].Count == 0) {
                    continue;
                }
                foreach (string sprite in typeTotileSpriteNames[type]) {
                    //need to find all sprites for that type
                    GameObject g = GameObject.Instantiate(prefabListItem);
                    g.transform.SetParent(content.transform);

                    g.GetComponentInChildren<Text>().text = sprite;
                    //set the trigger up
                    EventTrigger eventTrigger = g.GetComponent<EventTrigger>();
                    EventTrigger.Entry entry = new EventTrigger.Entry {
                        eventID = EventTriggerType.Select,
                        callback = new EventTrigger.TriggerEvent()
                    };
                    int temp = number;
                    number++;
                    entry.callback.AddListener((data) => {
                        OnSelect(temp);
                    });
                    eventTrigger.triggers.Add(entry);
                    g.SetActive(false);
                    gos.Add(g);
                }
                //			Debug.Log ("typeToGameObjects |" + type + "|");
                typeToGameObjects.Add(type, gos);
            }
        }

        public void ChangeType(TileType item) {
            foreach (Transform t in content.transform) {
                t.gameObject.SetActive(false);
            }
            if (item == TileType.Ocean) {
                return;
            }
            if (typeToGameObjects.ContainsKey(item.ToString()) == false) {
                return;
            }
            foreach (GameObject go in typeToGameObjects[item.ToString()]) {
                go.SetActive(true);
            }
            currentSelected = item;
            OnSelect(0);
        }

        public void OnSelect(int number) {
            EditorController.Instance.changeMode = ChangeMode.Tile;
            EditorController.Instance.spriteName = typeTotileSpriteNames[currentSelected.ToString()][number];
        }

        private void LoadSprites() {
            if (typeTotileSpriteNames.Count > 0) {
                return;
            }
            foreach (TileType tt in Enum.GetValues(typeof(TileType))) {
                typeTotileSpriteNames.Add(tt.ToString(), TileSpriteController.GetAllSpriteNamesForType(tt, EditorController.climate));
            }
        }
    }
}