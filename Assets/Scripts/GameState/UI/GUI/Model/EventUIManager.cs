﻿using Andja.Controller;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.UI.Model {
    //TODO: hook it up
    public class EventUIManager : MonoBehaviour {
        public float onScreenTimer = 30f;
        private Dictionary<uint, GameObject> idToEventGO;

        //Mayber move this to EventManager
        private Dictionary<uint, float> idToCountTimer;

        public GameObject EventMessagePrefab;
        public Transform contentTransform;

        private void Start() {
            idToEventGO = new Dictionary<uint, GameObject>();
            idToCountTimer = new Dictionary<uint, float>();
            foreach (Transform item in contentTransform) {
                GameObject.Destroy(item.gameObject);
            }
            //AddEVENT(1, "TestEvent With a really long Name What is Happening now!?", new Vector2(50, 50));
        }

        public void AddEVENT(uint id, string name, Vector2 position) {
            GameObject ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(name, position);
            idToEventGO.Add(id, ego);
            idToCountTimer.Add(id, onScreenTimer);
        }

        // Update is called once per frame
        private void Update() {
            if (WorldController.Instance.IsPaused) {
                return;
            }
            List<uint> ids = new List<uint>(idToCountTimer.Keys);
            foreach (uint i in ids) {
                idToCountTimer[i] = idToCountTimer[i] - Time.deltaTime;
                if (idToCountTimer[i] <= 0) {
                    idToCountTimer.Remove(i);
                }
            }
        }
    }
}