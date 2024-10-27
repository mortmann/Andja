using System;
using UnityEngine;
namespace Andja.Model.Components {
    public class EventHoldingScript : MonoBehaviour {
        public GameEvent gameEvent;


        void Start() {
            gameEvent.CbEventEnded += OnEnded;
            transform.position = gameEvent.DefinedPosition;
            GetComponent<CircleCollider2D>().radius = gameEvent.Radius;
        }

        private void OnEnded(GameEvent obj) {
            GetComponent<CircleCollider2D>().radius = 0;
            Destroy(gameObject);
        }

        //THIS one is the one that works for now! Because itself is a trigger!
        private void OnTriggerEnter2D(Collider2D collider) {
            ITargetableHoldingScript iths = collider.GetComponent<ITargetableHoldingScript>();
            if (iths.Holding is IGEventable eventable && gameEvent.IsTarget(eventable)) {
                eventable.OnEventCreate(gameEvent);
            }
        }
        private void OnTriggerExit2D(Collider2D collider) {
            ITargetableHoldingScript iths = collider.GetComponent<ITargetableHoldingScript>();
            if (iths.Holding is IGEventable eventable && gameEvent.IsTarget(eventable)) {
                eventable.OnEventEnded(gameEvent);
            }
        }
    }
}

