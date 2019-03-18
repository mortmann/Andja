using Newtonsoft.Json;
using System;
using System.Collections.Generic;

public enum ServiceTarget { All, Damageable, Military, Homes, Production, Service, NeedStructure, SpecificRange, City, None }
public enum ServiceFunction { None, Repair, AddEffect, RemoveEffect, PreventEffect }
public class ServiceStructurePrototypeData : StructurePrototypeData {
    public ServiceTarget targets = ServiceTarget.All;
    public ServiceFunction function;
    public Structure[] specificRange = null;
    public Effect[] effectsOnTargets;
    public int maxNumberOfWorker = 1;
    public float workSpeed = 0.01f;
}
[JsonObject(MemberSerialization.OptIn)]
public class ServiceStructure : Structure {
    [JsonPropertyAttribute] List<Worker> workers;
    List<Structure> jobsToDo;

    ServiceFunction Function => ServiceData.function;
    ServiceTarget Targets => ServiceData.targets;
    Effect[] EffectsOnTargets => ServiceData.effectsOnTargets;
    Structure[] SpecificRange => ServiceData.specificRange;
    int MaxNumberOfWorker => ServiceData.maxNumberOfWorker;
    public float WorkSpeed => ServiceData.workSpeed;

    public Func<Structure, float, bool> WorkOnTarget { get; protected set; }
    Action<Structure> todoOnNewTarget;
    Action<Structure> onTargetChanged;
    Action<IGEventable, Effect, bool> onTargetEffectChange;


    protected ServiceStructurePrototypeData _servicveData;
    public ServiceStructurePrototypeData ServiceData {
        get {
            if (_servicveData == null) {
                _servicveData = (ServiceStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _servicveData;
        }
    }
    public ServiceStructure() {
        jobsToDo = new List<Structure>();
    }
    protected ServiceStructure(ServiceStructure s) : base() {
        BaseCopyData(s);
    }

    public ServiceStructure(int iD) {
        ID = iD;
    }

    public override Structure Clone() {
        return new ServiceStructure(this);
    }

    public override void OnBuild() {
        if(Targets == ServiceTarget.City) {
            EffectCity();
            return;
        }
        switch (Function) {
            case ServiceFunction.None:
                break;
            case ServiceFunction.Repair:
                WorkOnTarget = RepairStructure;
                todoOnNewTarget = RegisterOnStructureChange;
                onTargetChanged = CheckHealth;
                break;
            case ServiceFunction.AddEffect:
                todoOnNewTarget += RegisterOnStructureChange;
                todoOnNewTarget += ImproveStructure;
                break;
            case ServiceFunction.RemoveEffect:
                WorkOnTarget = RemoveEffect;
                todoOnNewTarget = RegisterOnStructureChange;
                onTargetEffectChange += CheckEffect;
                break;
            case ServiceFunction.PreventEffect:
                todoOnNewTarget += RegisterOnStructureChange;
                onTargetEffectChange += PreventEffect;
                break;
        }
        foreach(Tile t in myRangeTiles) {
            if (t.Structure == null)
                continue;
            if(SpecificRange != null) {
                foreach(Structure str in SpecificRange) {
                    if(str.ID == t.Structure.ID) {
                        todoOnNewTarget(t.Structure);
                        break;
                    }
                }
            } else {
                todoOnNewTarget(t.Structure);
            }
        }
        City.RegisterStructureAdded(OnAddedStructure);
    }

    private void CheckEffect(IGEventable arg1, Effect arg2, bool arg3) {
        throw new NotImplementedException();
    }

    private void CheckHealth(Structure obj) {
        if (obj.NeedsRepair == false)
            return;
        EnqueueJob(obj);
    }

    private void EnqueueJob(Structure structure) {
        jobsToDo.Add(structure);
    }

    private void OnAddedStructure(Structure obj) {
        bool InRange = false;
        foreach(Tile t in obj.myStructureTiles) {
            if (myRangeTiles.Contains(t)) {
                InRange = true;
                break;
            }
        }
        if (InRange == false)
            return;
        todoOnNewTarget(obj);
    }

    public bool RepairStructure(Structure str,float deltaTime) {
        str.RepairHealth(WorkSpeed * deltaTime);
        if (str.CurrentHealth >= str.MaxHealth)
            return true;
        return false;
    }
    public void ImproveStructure(Structure str) {
        foreach(Effect eff in EffectsOnTargets) {
            //structure will check if its a valid effect
            str.AddEffect(eff);
        }
    }
    public bool RemoveEffect(Structure str, float deltaTime) {
        //structure will check if its a valid effect
        foreach (Effect eff in EffectsOnTargets) {
            Effect strEffect = str.GetEffect(eff.ID);
            strEffect.WorkAmount += deltaTime * WorkSpeed;
            if(strEffect.WorkAmount >= 1)
                str.RemoveEffect(eff);
        }
        return true;
    }
    public void PreventEffect(IGEventable str, Effect effect, bool added) {
        if (added == false)
            return;
        foreach (Effect eff in EffectsOnTargets) {
            if(eff.ID == effect.ID) {
                str.RemoveEffect(effect);
            }
        }
    }
    public override void OnUpdate(float deltaTime) {
        if (WorkOnTarget == null)
            return;
        SendOutWorkerIfCan();
        for (int i = workers.Count - 1; i >= 0; i--) {
            workers[i].Update(deltaTime);
            if (workers[i].isAtHome)
                workers.RemoveAt(i);
        }
    }

    private void SendOutWorkerIfCan() {
        if(workers.Count >= MaxNumberOfWorker) {
            return;
        }
        int i = 0;
        Structure s = null;
        foreach(Structure str in jobsToDo) {
            if (Function == ServiceFunction.Repair && str.HasNegativEffect)
                continue;
            if (CanReachStructure(s) == false)
                continue;
            s = str;
            break;
        }
        jobsToDo.RemoveAt(i);
        Worker w = new Worker(this, s, WorkSpeed);
        workers.Add(w);
    }

    public void RegisterOnStructureChange(Structure str) {
        str.RegisterOnChangedCallback(onTargetChanged);
    }
    public void UnregisterOnStructureChange(Structure str) {
        str.UnregisterOnChangedCallback(onTargetChanged);
    }
    public void RegisterOnStructureEffectChanged(Structure str) {
        str.RegisterOnEffectChangedCallback(onTargetEffectChange);
    }
    public void UnregisterOnStructureEffectChanged(Structure str) {
        str.UnregisterOnEffectChangedCallback(onTargetEffectChange);
    }

    public void EffectCity() {
        //Will run once on build
    }
    public void RemoveEffectCity() {
        //removed on destroy
    }
    protected override void OnDestroy() {
        if (Targets == ServiceTarget.City) {
            RemoveEffectCity();
            return;
        }
        for (int i = workers.Count - 1; i >= 0; i--) {
            workers[i].Destroy();
        }
        foreach (Tile t in myRangeTiles) {
            if (t.Structure == null)
                continue;
            if (SpecificRange != null) {
                foreach (Structure str in SpecificRange) {
                    if (str.ID == t.Structure.ID) {
                        UnregisterOnStructureChange(t.Structure);
                        UnregisterOnStructureEffectChanged(t.Structure);
                        continue;
                    }
                }
            }
        }
        City.UnregisterStructureAdded(OnAddedStructure);
    }
}
