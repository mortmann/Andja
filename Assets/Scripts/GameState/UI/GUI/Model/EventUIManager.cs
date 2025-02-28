using Andja.Controller;
using Andja.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Andja.UI.EventMessage;

namespace Andja.UI.Model {
    public class EventUIManager : MonoBehaviour {
        public static EventUIManager Instance;
        public float onScreenTimer = 30f;

        List<EventMessage> messages;

        //Mayber move this to EventManager
        public EventMessage EventMessagePrefab;
        public Transform contentTransform;

        private void Start() {
            Instance = this;
            messages = new List<EventMessage>();
            foreach (Transform item in contentTransform) {
                GameObject.Destroy(item.gameObject);
            }
        }

        public EventMessage AddEvent(GameEvent gameEvent) {
            EventMessage ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(gameEvent);
            messages.Add(ego);
            return ego;
        }

        private void OnDestroy() {
            Instance = null;
        }

        internal void RemoveEvent(EventMessage eventMessage) {
            messages.Remove(eventMessage);
            Destroy(eventMessage.gameObject);
        }

        public void Show(BaseThing baseThing, IAttack attack) {
            if (baseThing is Unit unit) {
                Show(unit, attack);
            }

            if (baseThing is Structure structure) {
                Show(structure, attack);
            }
        }

        private void Show(Unit unit, IAttack attack) {
            if (CheckShown(unit.BuildID, attack))
                return;
            Show(BasicInformation.CreateUnitDamage(unit, attack.Parent));
        }

        private void Show(Structure str, IAttack attack) {
            if (CheckShown(str.BuildID, attack))
                return;
            Show(BasicInformation.CreateStructureDamage(str, attack.Parent));
        }

        private bool CheckShown(uint eventable, IAttack attack) {
            return messages.Exists(m => m.Information is AttackInformation a && a.IsSame(eventable, attack)
                                                                             && DateTime.Now.Subtract(m.ShownTime)
                                                                                 .TotalSeconds <= onScreenTimer);
        }

        /// <summary>
        /// This does not contain a check for duplicate Information -- Only use this directly if everytime the player should be informed
        /// </summary>
        /// <param name="basicInformation"></param>
        internal EventMessage Show(BasicInformation basicInformation) {
            EventMessage ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(basicInformation);
            messages.Add(ego);
            return ego;
        }

        /// <summary>
        /// This does not contain a check for duplicate Information -- Only use this directly if everytime the player should be informed
        /// </summary>
        /// <param name="basicInformation"></param>
        internal EventMessage Show(ChoiceInformation basicInformation) {
            EventMessage ego = Instantiate(EventMessagePrefab);
            ego.transform.SetParent(contentTransform, false);
            ego.GetComponent<EventMessage>().Setup(basicInformation);
            basicInformation.OnClose = () => { RemoveEvent(ego); };
            messages.Add(ego);
            return ego;
        }

        internal EventUISave GetSave() {
            return new EventUISave { Messages = messages.Select(m => m.GetSave()).ToArray() };
        }

        internal void Load(EventUISave save) {
            save.Messages?.OrderBy(m => m.ShownTime).ToList().ForEach(m => LoadMessage(m));
        }

        private void LoadMessage(EventMessageSave load) {
            EventMessage loaded;
            if (load.Information != null) {
                loaded = Show(load.Information.Load());
            }
            else {
                loaded = AddEvent(EventController.Instance.GetEventByID(load.gameEventID.Value));
            }

            loaded.ShownTime = load.ShownTime;
        }

        public class EventUISave {
            public EventMessageSave[] Messages;
        }
    }
}