using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.UI.Model {

    public class EventUIManager : MonoBehaviour {
        public static EventUIManager Instance;
        public float onScreenTimer = 30f;
        List<EventMessage> messages;
        //Mayber move this to EventManager

        public EventMessage EventMessagePrefab;
        public Transform contentTransform;
        Dictionary<object, DateTime> shownToTime = new Dictionary<object, DateTime>();

        private void Start() {
            Instance = this;
            messages = new List<EventMessage>();
            foreach (Transform item in contentTransform) {
                GameObject.Destroy(item.gameObject);
            }
            //AddEVENT(1, "TestEvent With a really long Name What is Happening now!?", new Vector2(50, 50));
        }

        public void AddEvent(GameEvent gameEvent) {
            EventMessage ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(gameEvent);
            messages.Add(ego);
        }

        private void OnDestroy() {
            Instance = null;
        }

        internal void RemoveEvent(EventMessage eventMessage) {
            messages.Remove(eventMessage);
            Destroy(eventMessage.gameObject);
        }

        internal void Show(Unit unit, IWarfare warfare) {
            var value = new KeyValuePair<Unit, IWarfare>(unit, warfare);
            if (CheckShown(value))
                return;
            Show(BasicInformation.CreateUnitDamage(unit, warfare));
        }
        internal void Show(Structure str, IWarfare warfare) {
            var value = new KeyValuePair<Structure, IWarfare>(str, warfare);
            if (CheckShown(value))
                return;
            Show(BasicInformation.CreateStructureDamage(str, warfare));
        }

        private bool CheckShown(object value) {
            if(shownToTime.ContainsKey(value)) {
                if (DateTime.Now.Subtract(shownToTime[value]).TotalSeconds <= onScreenTimer) {
                    return true;
                }
            }
            shownToTime.Add(value, DateTime.Now);
            return false;
        }

        /// <summary>
        /// This does not contain a check for duplicate Information -- Only use this directly if everytime the player should be informed
        /// </summary>
        /// <param name="basicInformation"></param>
        internal void Show(BasicInformation basicInformation) {
            EventMessage ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(basicInformation);
            messages.Add(ego);
        }
    }
}