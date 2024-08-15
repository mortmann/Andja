using Andja.Controller;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public enum ServiceTarget { All, Damageable, Military, Homes, Production, Service, NeedStructure, SpecificRange, City, None }

    public enum ServiceFunction { None, Repair, AddEffect, RemoveEffect, PreventEffect }

    public class ServiceStructurePrototypeData : StructurePrototypeData {
        public ServiceTarget targets = ServiceTarget.All;
        public ServiceFunction function;
        public Structure[] specificRange = null;
        public Effect[] effectsOnTargets;
        //IF the order of this changes it will not have massive effect
        //but it will change how much of each is in it atm look at remainingItems
        public Item[] usageItems; 
        public float[] usagePerTick;
        public int maxNumberOfWorker = 1;
        public float workSpeed = 0.01f;
        public bool hasToEnterWorkStructure = true;
        public float usageTickTime = 60f;
        public string workerID;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ServiceStructure : Structure {
        [JsonPropertyAttribute] protected List<Worker> workers;
        [JsonPropertyAttribute] public float usageTickTimer { get; protected set; }
        [JsonPropertyAttribute] public bool CanWork { get; protected set; }
        [JsonPropertyAttribute] public float[] remainingUsageItems { get; protected set; }
        public float[] UsagePerTick => CalculateRealValue(nameof(ServiceData.usagePerTick), ServiceData.usagePerTick);
        public Item[] UsageItems => ServiceData.usageItems;
        public float UsageTickTime => ServiceData.usageTickTime;
        protected List<Structure> jobsToDo;
        private ServiceFunction Function => ServiceData.function;
        private ServiceTarget Targets => ServiceData.targets;
        private Effect[] EffectsOnTargets => ServiceData.effectsOnTargets;
        private Structure[] SpecificRange => ServiceData.specificRange;
        private int MaxNumberOfWorker => ServiceData.maxNumberOfWorker;
        public float WorkSpeed => ServiceData.workSpeed;
        public bool HasToEnterWorkStructure => ServiceData.hasToEnterWorkStructure;
        public override bool IsActiveAndWorking => base.IsActiveAndWorking && CanWork;
        public Func<Structure, float, bool> WorkOnTarget { get; protected set; }
        protected Action<Structure> todoOnNewTarget;
        protected Action<Structure> onTargetChanged;
        protected Action<Structure, IWarfare> onTargetDestroy;
        protected Action<Structure> onSelfDestroy;

        protected Action<GEventable, Effect, bool> onTargetEffectChange;

        protected ServiceStructurePrototypeData serviceData;
        //TODO: make it possible service structure to need certain items every time unit to function(otherwise inactive)
        public ServiceStructurePrototypeData ServiceData => serviceData ??= (ServiceStructurePrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);


        public ServiceStructure() {
        }

        protected ServiceStructure(ServiceStructure s) : base() {
            BaseCopyData(s);
        }

        public ServiceStructure(string iD, ServiceStructurePrototypeData sspd) {
            ID = iD;
            serviceData = sspd;
        }

        public override Structure Clone() {
            return new ServiceStructure(this);
        }

        public override void OnBuild(bool loading = false) {
            if (remainingUsageItems == null && UsageItems != null) {
                remainingUsageItems = new float[UsageItems.Length];
            }
            if (Targets == ServiceTarget.City) {
                AddEffectCity();
                return;
            }
            jobsToDo = new List<Structure>();
            SetCallbacks();
            SetUpStructures();
        }

        protected void SetUpStructures() {
            todoOnNewTarget += RegisterOnStructureDestroy;
            foreach (var t in GetEffectedStructuresInRange()) {
                todoOnNewTarget.Invoke(t.Structure);
                if (onTargetEffectChange == null || t.Structure.Effects == null) continue;
                foreach (Effect e in t.Structure.Effects) {
                    onTargetEffectChange.Invoke(t.Structure, e, true);
                }
            }

            City.RegisterStructureAdded(OnAddedStructure);
        }

        private IEnumerable<Tile> GetEffectedStructuresInRange() {
            return RangeTiles.GroupBy(x=>x.Structure).SelectMany(group => group.ToList()).Where(IsStructureTileEffected);
        }

        private bool IsStructureTileEffected(Tile t) {
            if (t.Structure == null) return false;
            return SpecificRange == null || SpecificRange.Any(str => str.ID == t.Structure.ID);
        }

        protected void SetCallbacks() {
            switch (Function) {
                case ServiceFunction.None:
                    break;

                case ServiceFunction.Repair:
                    WorkOnTarget = RepairStructure;
                    todoOnNewTarget = RegisterOnStructureChange;
                    onTargetChanged = CheckStructureHealth;
                    onTargetDestroy += UnregisterOnStructureChange;
                    onTargetDestroy += RemoveFromJobs;
                    break;

                case ServiceFunction.AddEffect:
                    //only needed when a structure can have a effect once else always add it
                    if (Array.Exists(EffectsOnTargets, element => element.IsUnique)) {
                        todoOnNewTarget += RegisterOnStructureEffectChanged;
                        onTargetEffectChange += CheckForMissingEffect;
                    }
                    onTargetDestroy += UnregisterOnStructureEffectChanged;
                    todoOnNewTarget += ImproveTarget;
                    onSelfDestroy += RemoveTargetEffect;
                    break;

                case ServiceFunction.RemoveEffect:
                    //what to do on new structure
                    WorkOnTarget = RemoveEffectOverTime;
                    todoOnNewTarget = RegisterOnStructureEffectChanged;
                    //what to do on effect on a target changed
                    onTargetEffectChange += CheckStructureEffectEnqueueJob;
                    //what to do on structure gets destroyed
                    onTargetDestroy += UnregisterOnStructureEffectChanged;
                    onTargetDestroy += RemoveFromJobs;
                    break;

                case ServiceFunction.PreventEffect:
                    todoOnNewTarget += RegisterOnStructureEffectChanged;
                    onTargetEffectChange += PreventEffect;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        protected void CheckForMissingEffect(IGEventable eventable, Effect effect, bool started) {
            if (started) {
                return;
            }
            if (Array.Exists(EffectsOnTargets, element => element.ID == effect.ID) == false) {
                return;
            }
            if (eventable.HasEffect(effect) && effect.IsUnique)
                return;
            eventable.AddEffect(new Effect(effect.ID));
        }

        private void RemoveFromJobs(Structure str, IWarfare destroyer) {
            if (jobsToDo.Contains(str))
                jobsToDo.Remove(str);
        }

        private void RegisterOnStructureDestroy(Structure str) {
            str.RegisterOnDestroyCallback(onTargetDestroy);
        }

        protected void CheckStructureEffectEnqueueJob(IGEventable eventable, Effect eff, bool started) {
            if (!(eventable is Structure structure))
                return;
            if (Array.Exists(EffectsOnTargets, element => element.ID == eff.ID) == false) {
                return;
            }
            if (started == false) {
                if (jobsToDo.Contains(structure))
                    jobsToDo.Remove(structure);
                return;
            }
            EnqueueJob(structure);
        }

        protected void CheckStructureHealth(Structure obj) {
            if (obj.NeedsRepair == false) {
                if (jobsToDo.Contains(obj))
                    jobsToDo.Remove(obj);
                return;
            }
            EnqueueJob(obj);
        }

        private void EnqueueJob(Structure structure) {
            jobsToDo ??= new List<Structure>();
            jobsToDo.Add(structure);
        }

        protected void OnAddedStructure(Structure obj) {
            if (obj.Tiles.Any(t => RangeTiles.Contains(t)) == false) return;
            todoOnNewTarget(obj);
        }

        protected bool RepairStructure(Structure str, float deltaTime) {
            str.RepairHealth(WorkSpeed * deltaTime);
            return str.CurrentHealth >= str.MaximumHealth;
        }

        protected void ImproveTarget(IGEventable target) {
            foreach (Effect eff in EffectsOnTargets) {
                target.AddEffect(eff);
            }
        }

        protected bool RemoveEffectOverTime(Structure str, float deltaTime) {
            // does the structure have a effect that this handles? if not worker is done
            if (str.HasAnyEffect(EffectsOnTargets) == false)
                return true;
            //structure will check if its a valid effect
            foreach (Effect eff in EffectsOnTargets) {
                Effect strEffect = str.GetEffect(eff.ID);
                strEffect.WorkAmount += deltaTime * WorkSpeed;
                if (strEffect.WorkAmount >= 1)
                    str.RemoveEffect(strEffect);
            }
            return false;
        }

        protected void RemoveTargetEffect(IGEventable target) {
            foreach (Effect eff in EffectsOnTargets) {
                target.RemoveEffect(eff);
            }
        }

        protected void PreventEffect(IGEventable str, Effect effect, bool added) {
            if (added == false)
                return;
            if (Array.Exists<Effect>(EffectsOnTargets, x => x.ID == effect.ID)) {
                str.RemoveEffect(effect, true);
            }
        }

        protected override void OnUpdate(float deltaTime) {
            if(UsageItems != null) {
                if(usageTickTimer > 0) {
                    usageTickTimer = Mathf.Clamp(usageTickTimer - deltaTime, 0, UsageTickTime);
                } else {
                    for (int i = 0; i < remainingUsageItems.Length; i++) {
                        if(remainingUsageItems[i] < UsagePerTick[i]) {
                            if (City.HasEnoughOfItem(UsageItems[i])) {
                                //has not enough and can get more
                                City.Inventory.RemoveItemAmount(UsageItems[i]);
                                remainingUsageItems[i] += UsageItems[i].count;
                                CanWork = true;
                            }
                            else {
                                //does not have enough & can't get it
                                CanWork = false;
                                break;
                            }
                        } else {
                            //has enough in internal stock for operation
                            remainingUsageItems[i] -= UsageItems[i].count;
                            CanWork = true;
                            usageTickTimer = UsageTickTime;
                        }
                    }
                }
            }
            SendOutWorkerIfCan();
            if (workers == null)
                return;
            for (int i = workers.Count - 1; i >= 0; i--) {
                workers[i].Update(deltaTime);
                if (workers[i].isAtHome == false) continue;
                workers[i].Destroy();
                workers.RemoveAt(i);
            }
        }

        private void SendOutWorkerIfCan() {
            if (jobsToDo == null || jobsToDo.Count == 0)
                return;
            workers ??= new List<Worker>();
            if(UsageItems != null) {
                if (CanWork == false)
                    return;
            }
            if (workers.Count >= MaxNumberOfWorker) {
                return;
            }
            Structure s = jobsToDo.Where(str => Function != ServiceFunction.Repair 
                                                || str.HasNegativeEffect == false)
                                  .FirstOrDefault(CanReachStructure);
            jobsToDo.Remove(s);
            Worker w = new Worker(this, s, WorkSpeed, ServiceData.workerID ?? "placeholder_road");
            World.Current.CreateWorkerGameObject(w);
            workers.Add(w);
        }

        public void RegisterOnStructureChange(Structure str) {
            str.RegisterOnChangedCallback(onTargetChanged);
        }

        public void UnregisterOnStructureChange(Structure str, IWarfare destroyer) {
            str.UnregisterOnChangedCallback(onTargetChanged);
        }

        public void RegisterOnStructureEffectChanged(Structure str) {
            str.RegisterOnEffectChangedCallback(onTargetEffectChange);
        }

        public void UnregisterOnStructureEffectChanged(Structure str, IWarfare destroyer) {
            str.UnregisterOnEffectChangedCallback(onTargetEffectChange);
        }

        protected void AddEffectCity() {
            foreach (Effect eff in EffectsOnTargets) {
                //will check if its a valid effect
                City.AddEffect(eff);
            }
        }

        protected void RemoveEffectCity() {
            //removed on destroy
            foreach (Effect eff in EffectsOnTargets) {
                //will check if its a valid effect
                City.RemoveEffect(eff);
            }
        }

        public override void OnDestroy() {
            if (Targets == ServiceTarget.City) {
                RemoveEffectCity();
                return;
            }
            if (workers != null) {
                for (int i = workers.Count - 1; i >= 0; i--) {
                    workers[i].Destroy();
                }
            }
            if (RangeTiles != null) {
                foreach (var t in GetEffectedStructuresInRange()) {
                    foreach (Effect effect in EffectsOnTargets) {
                        t.Structure.RemoveEffect(effect);
                    }
                    UnregisterOnStructureChange(t.Structure, null);
                    UnregisterOnStructureEffectChanged(t.Structure, null);
                    onSelfDestroy?.Invoke(t.Structure);
                }
            }
            City.UnregisterStructureAdded(OnAddedStructure);
        }
        public override void Load() {
            base.Load();
            if (UsageItems != null && (remainingUsageItems == null || remainingUsageItems.Length != UsageItems.Length)) {
                float[] temp = remainingUsageItems;
                remainingUsageItems = new float[UsageItems.Length];
                if (temp != null) {
                    for (int i = 0; i < Math.Min(UsageItems.Length, temp.Length); i++) {
                        remainingUsageItems[i] = temp[i];
                    }
                }
            }
            if (workers == null) return;
            foreach (var worker in workers) {
                worker.Load(this);
            }
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            serviceData = null;
        }
    }
}