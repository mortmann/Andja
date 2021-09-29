using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class EffectsUI : MonoBehaviour {
        public ShowToolTip SimpleImage;
        Dictionary<Effect, GameObject> effectToGO;
        IGEventable eventable;
        public void Show(IGEventable eventable) {
            foreach(Transform t in transform) {
                Destroy(t.gameObject);
            }
            effectToGO = new Dictionary<Effect, GameObject>();
            eventable.RegisterOnEffectChangedCallback(OnEffectChange);
            this.eventable = eventable;
            if(eventable.Effects != null) {
                foreach (Effect effect in eventable.Effects) {
                    AddEffect(effect);
                }
            }
        }

        private void AddEffect(Effect effect) {
            ShowToolTip image = Instantiate(SimpleImage);
            image.GetComponent<Image>().sprite = UISpriteController.GetIcon(effect.ID);
            image.SetVariable(effect.EffectPrototypData, true);
            image.transform.SetParent(transform, false);
            effectToGO[effect] = image.gameObject;
        }

        private void OnEffectChange(IGEventable target, Effect effect, bool add) {
            if(add) {
                AddEffect(effect);
            } else {
                Destroy(effectToGO[effect]);
                effectToGO.Remove(effect);
            }
        }
        private void OnDisable() {
            eventable?.UnregisterOnEffectChangedCallback(OnEffectChange);
        }
    }
}