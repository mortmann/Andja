using Andja;
using Andja.Controller;
using Andja.Model;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseThing : GEventable {

    BaseThingData prototypeData;
    private BaseThingData Data => prototypeData ??= GetPrototypeData();

    private BaseThingData GetPrototypeData() {
        if(this is Structure) {
            return PrototypController.Instance.GetStructurePrototypDataForID(ID);
        }
        if (this is Unit) {
            return PrototypController.Instance.GetUnitPrototypDataForID(ID);
        }
        Log.PROTOTYPE_ERROR("No Prototyp Data Type for this " + this);
        return null;
    }

    [JsonPropertyAttribute] public string ID;

    [JsonPropertyAttribute] protected float currentHealth;

    public float MaximumHealth => CalculateRealValue(nameof(Data.maxHealth), Data.maxHealth);
    public int UpkeepCost => CalculateRealValue(nameof(Data.upkeepCost), Data.upkeepCost).ClampZero(); //UNTESTED HOW THIS WILL WORK

    public bool IsDestroyed => CurrentHealth <= 0;
    public bool CanTakeDamage => Data.canTakeDamage;
    public int BuildCost => Data.buildCost;
    public Item[] BuildingItems => Data.buildingItems;
    public string SpriteName => Data.spriteBaseName/*TODO: make multiple saved sprites possible*/;
    public int PopulationLevel => Data.populationLevel;
    public int PopulationCount => Data.populationCount;
    public bool IsStructure => this is Structure;
    public bool IsUnit => this is Unit; 

    public float CurrentHealth {
        get => currentHealth;
        set {
            currentHealth = value;
            if (CanTakeDamage == false) {
                return;
            }
            if (currentHealth <= 0) {
                Destroy();
            }
        }
    }

    public void ReduceHealth(float damage, IWarfare warfare = null) {
        if (CanTakeDamage == false) {
            return;
        }
        if (CurrentHealth <= 0) // fix for killing it too many times -- triggering destroy multiple times
            return;
        if (damage < 0) {
            Debug.LogWarning("Damage should be never smaller than 0 - Fix it!");
            return;
        }
        CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaximumHealth);
        OnReduceHealth(damage, warfare);
    }

    protected virtual void OnReduceHealth(float damage, IWarfare warfare) {
    }

    public void RepairHealth(float heal) {
        if (IsDestroyed) return;
        if (heal < 0) {
            Debug.LogWarning("Healing should be never smaller than 0 - Fix it!");
            return;
        }
        CurrentHealth += heal;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaximumHealth);
    }
    public void Update(float deltaTime) {
        if (CurrentHealth > MaximumHealth) {
            //Values got changed or maybe upgrade lost? we need to reduce it slowly
            CurrentHealth = Mathf.Clamp(CurrentHealth - 10 * deltaTime, MaximumHealth, CurrentHealth);
        }
        UpdateEffects(deltaTime);
        OnUpdate(deltaTime);
    }

    protected virtual void OnUpdate(float deltaTime) {
    }


    public void ChangeHealth(float change) {
        if (change < 0)
            ReduceHealth(-change);
        if (change > 0)
            RepairHealth(change);
    }
    
    /// <summary>
    /// Destroys this immedietly and without any further checks. 
    /// </summary>
    /// <param name="destroyer"></param>
    /// <param name="onLoad"></param>
    /// <returns></returns>
    public virtual bool Destroy(IWarfare destroyer = null, bool onLoad = false) {
        return true;
    }
}