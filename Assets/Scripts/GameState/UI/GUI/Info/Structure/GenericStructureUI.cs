using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenericStructureUI : MonoBehaviour {
    public HealthBarUI HealthBar;
    public Text NameText;
    public EffectUI EffectPrototyp;
    public Transform EffectsTransform;
    Dictionary<Effect, EffectUI> effectToUI;
    Structure structure;
    public void OnEnable() {
        effectToUI = new Dictionary<Effect, EffectUI>();
        foreach (Transform t in EffectsTransform)
            Destroy(t.gameObject);
    }
    public void Show(Structure structure) {
        this.structure = structure;
        NameText.text = structure.Name;
        if(structure.ReadOnlyEffects != null) {
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

    void OnEffectAdded(Effect effect) {
        if (effectToUI.ContainsKey(effect))
            return;
        EffectUI effectUI = Instantiate(EffectPrototyp);
        effectUI.Show(effect);
        effectUI.transform.SetParent(EffectsTransform);
        effectToUI.Add(effect, effectUI);
    }
    void OnEffectRemoved(Effect effect) {
        if (effectToUI.ContainsKey(effect) == false)
            return;
        Destroy(effectToUI[effect].gameObject);
    }
    public void Update() {
        HealthBar.SetHealth(structure.CurrentHealth, structure.MaxHealth);
    }
}
