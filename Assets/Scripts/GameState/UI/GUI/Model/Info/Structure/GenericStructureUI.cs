using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {
    /// <summary>
    /// Obsolet
    /// </summary>
    public class GenericStructureUI : MonoBehaviour {
        public HealthBarUI HealthBar;
        public Text NameText;
        public EffectsUI EffectPrototyp;
        public Transform EffectsTransform;
        private Dictionary<Effect, EffectsUI> effectToUI;
        private Structure structure;

        public void OnEnable() {
            effectToUI = new Dictionary<Effect, EffectsUI>();
            foreach (Transform t in EffectsTransform)
                Destroy(t.gameObject);
        }

        public void Show(Structure structure) {
            this.structure = structure;
            if (NameText != null)
                NameText.text = structure.Name;
        }



        public void Update() {
            HealthBar.SetHealth(structure.CurrentHealth, structure.MaximumHealth);
        }
    }
}