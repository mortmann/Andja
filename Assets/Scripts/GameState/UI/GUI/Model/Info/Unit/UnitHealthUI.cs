using Andja.Controller;
using Andja.Model;
using Andja.UI.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI {

    public class UnitHealthUI : MonoBehaviour {
        public Image unitImage;
        public HealthBarUI HealthBar;
        public EffectsUI EffectPrototyp;
        public Transform EffectsTransform;
        public EventTrigger triggers;
        private Unit unit;

        public void OnEnable() {
            foreach (Transform t in EffectsTransform)
                Destroy(t.gameObject);
        }

        public void Show(Unit unit) {
            this.unit = unit;
            EventTrigger.Entry leftclick = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            leftclick.callback.AddListener((data) => {
                if (((PointerEventData)(data)).button != PointerEventData.InputButton.Left) {
                    return;
                }
                CameraController.Instance.MoveCameraToPosition(unit.PositionVector);
            });
            triggers.triggers.Add(leftclick);
        }

        public void AddRightClick(Action<Unit> action) {
            EventTrigger.Entry rightclick = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            rightclick.callback.AddListener((data) => {
                if (((PointerEventData)(data)).button != PointerEventData.InputButton.Right) {
                    return;
                }
                action(unit);
            });
            triggers.triggers.Add(rightclick);
        }

        public void Update() {
            HealthBar.SetHealth(unit.CurrentHealth, unit.MaximumHealth);
        }
    }
}