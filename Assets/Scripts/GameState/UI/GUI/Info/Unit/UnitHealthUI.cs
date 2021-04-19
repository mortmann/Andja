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
        public EffectUI EffectPrototyp;
        public Transform EffectsTransform;
        public EventTrigger triggers;
        private Dictionary<Effect, EffectUI> effectToUI;
        private Unit unit;

        public void OnEnable() {
            effectToUI = new Dictionary<Effect, EffectUI>();
            foreach (Transform t in EffectsTransform)
                Destroy(t.gameObject);
        }

        public void Show(Unit unit) {
            this.unit = unit;
            if (unit.ReadOnlyEffects != null) {
                foreach (Effect e in unit.ReadOnlyEffects) {
                    OnEffectAdded(e);
                }
            }
            unit.RegisterOnEffectChangedCallback(OnEffectChange);

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

        private void OnEffectChange(IGEventable target, Effect eff, bool started) {
            if (target != unit)
                return;
            if (started)
                OnEffectAdded(eff);
            else
                OnEffectRemoved(eff);
        }

        private void OnEffectAdded(Effect effect) {
            if (effectToUI.ContainsKey(effect))
                return;
            EffectUI effectUI = Instantiate(EffectPrototyp);
            effectUI.Show(effect);
            effectUI.transform.SetParent(EffectsTransform, false);
            effectToUI.Add(effect, effectUI);
        }

        private void OnEffectRemoved(Effect effect) {
            if (effectToUI.ContainsKey(effect) == false)
                return;
            Destroy(effectToUI[effect].gameObject);
        }

        public void Update() {
            HealthBar.SetHealth(unit.CurrentHealth, unit.MaxHealth);
        }
    }
}