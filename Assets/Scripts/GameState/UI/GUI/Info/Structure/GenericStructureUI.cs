using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class GenericStructureUI : MonoBehaviour {
        public HealthBarUI HealthBar;
        public Text NameText;
        public EffectUI EffectPrototyp;
        public Transform EffectsTransform;
        private Dictionary<Effect, EffectUI> effectToUI;
        private Structure structure;

        public void OnEnable() {
            effectToUI = new Dictionary<Effect, EffectUI>();
            foreach (Transform t in EffectsTransform)
                Destroy(t.gameObject);
        }

        public void Show(Structure structure) {
            this.structure = structure;
            if (NameText != null)
                NameText.text = structure.Name;
            if (structure.ReadOnlyEffects != null) {
                foreach (Effect e in structure.ReadOnlyEffects) {
                    OnEffectAdded(e);
                }
            }
            structure.RegisterOnEffectChangedCallback(OnEffectChange);
        }

        private void OnEffectChange(IGEventable target, Effect eff, bool started) {
            if (target != structure)
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
            HealthBar.SetHealth(structure.CurrentHealth, structure.MaxHealth);
        }
    }
}