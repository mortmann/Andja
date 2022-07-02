﻿using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class OutputPrototypData : StructurePrototypeData {
        public float contactRange = 0;
        public bool forMarketplace = true;
        public int maxNumberOfWorker = 1;
        public float produceTime;
        public int maxOutputStorage;
        public float efficiency = 1f;
        public Item[] output;
        public string workSound;
        public string workerID;
        [Ignore] public float ProducePerMinute;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class OutputStructure : TargetStructure {
        public const string INACTIVE_EFFECT_ID = "inactive";

        #region Serialize

        [JsonPropertyAttribute] public List<Worker> Workers;
        [JsonPropertyAttribute] public float ProduceTimer { get; protected set; }
        protected Item[] _output;
        [JsonPropertyAttribute]
        public virtual Item[] Output {
            get {
                if (_output == null) {
                    if (OutputData.output == null) {
                        return null;
                    }
                    _output = new Item[OutputData.output.Length];
                    for (int i = 0; i < OutputData.output.Length; i++) {
                        _output[i] = OutputData.output[i].Clone();
                    }
                }
                return _output;
            }
            set {
                _output = value;
            }
        }
        #endregion Serialize

        #region RuntimeOrOther
        public WorkerPrototypeData _workerPrototypeData;
        public WorkerPrototypeData WorkerPrototypeData {
            get {
                if(_workerPrototypeData == null)
                    _workerPrototypeData = PrototypController.Instance.GetWorkerPrototypDataForID(OutputData.workerID ?? "placeholder");
                return _workerPrototypeData;
            }
        }
        public Dictionary<OutputStructure, Item[]> WorkerJobsToDo;
        public bool outputClaimed;
        protected Action<Structure> cbOutputChange;
        public float ContactRange => OutputData.contactRange; 
        public bool ForMarketplace => OutputData.forMarketplace; 

        public float ProduceTime => CalculateRealValue(nameof(OutputData.produceTime), OutputData.produceTime); 
        public int MaxNumberOfWorker => CalculateRealValue(nameof(OutputData.maxNumberOfWorker), OutputData.maxNumberOfWorker); 
        public float Efficiency => CalculateRealValue(nameof(OutputData.efficiency), OutputData.efficiency); 
        public int MaxOutputStorage => CalculateRealValue(nameof(OutputData.maxOutputStorage), OutputData.maxOutputStorage); 

        protected OutputPrototypData _outputData;

        public OutputPrototypData OutputData {
            get {
                if (_outputData == null) {
                    _outputData = (OutputPrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _outputData;
            }
        }

        #endregion RuntimeOrOther

        public OutputStructure() {
            WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
        }

        protected void OutputCopyData(OutputStructure o) {
            BaseCopyData(o);
        }

        public override bool IsActiveAndWorking => base.IsActiveAndWorking && Efficiency > 0;

        public virtual float EfficiencyPercent {
            get {
                return 100 * Efficiency;
            }
        }

        public virtual float Progress => ProduceTimer;
        public virtual float TotalProgress => ProduceTime;

        public void UpdateWorker(float deltaTime) {
            if (MaxNumberOfWorker <= 0) {
                return;
            }
            TrySendWorker();
            for (int i = Workers.Count - 1; i >= 0; i--) {
                Worker w = Workers[i];
                w.Update(deltaTime);
                if (w.isAtHome) {
                    WorkerComeBack(w);
                }
            }
        }

        public void TrySendWorker() {
            if (Workers == null) {
                Workers = new List<Worker>(MaxNumberOfWorker);
            }
            SendOutWorkerIfCan();
        }

        protected virtual void SendOutWorkerIfCan(float workTime = 1) {
            if (WorkerJobsToDo.Count == 0 || Workers.Count >= MaxNumberOfWorker) {
                return;
            }
            List<OutputStructure> givenJobs = new List<OutputStructure>();
            List<OutputStructure> ordered = WorkerJobsToDo.Keys.OrderByDescending(x => x.Output.Sum(y => y.count)).ToList();
            foreach (OutputStructure jobStr in ordered) {
                if (Workers.Count >= MaxNumberOfWorker) {
                    break;
                }
                if (jobStr.outputClaimed) {
                    continue;
                }
                Item[] items = GetRequiredItems(jobStr, WorkerJobsToDo[jobStr]);
                if (items == null || items.Length <= 0) {
                    continue;
                }
                if (WorkerPrototypeData.hasToFollowRoads && CanReachStructure(jobStr) == false) {
                    continue;
                }
                Worker ws = new Worker(this, jobStr, workTime, OutputData.workerID, items);
                givenJobs.Add(jobStr);
                World.Current.CreateWorkerGameObject(ws);
                Workers.Add(ws);
            }
            foreach (OutputStructure giveJob in givenJobs) {
                if (giveJob != null) {
                    if (giveJob is ProductionStructure) {
                        continue;
                    }
                    WorkerJobsToDo.Remove(giveJob);
                }
            }
        }

        public virtual Item[] GetRequiredItems(OutputStructure str, Item[] items) {
            if (items == null) {
                items = str.Output;
            }
            List<Item> all = new List<Item>();
            for (int i = 0; i < Output.Length; i++) {
                Item item = Array.Find(items, x => x.ID == Output[i].ID)?.Clone();
                if (item != null) {
                    item.count = MaxOutputStorage - Output[i].count;
                    item.count -= Workers.Where(z => z.ToGetItems != null)
                                    .Sum(x => Array.Find(x.ToGetItems, y => items[i].ID == y.ID)?.count ?? 0);
                    all.Add(item);
                }
            }
            return all.Where(x => x.count > 0).ToArray();
        }

        public void WorkerComeBack(Worker w) {
            if (Workers.Contains(w) == false) {
                Debug.LogError("WorkerComeBack - Worker comesback, but doesnt live here!");
                return;
            }
            w.Destroy();
            Workers.Remove(w);
        }

        public void AddToOutput(Inventory inv) {
            for (int i = 0; i < Output.Length; i++) {
                //maybe switch to manually foreach because it may be faster
                //because worker that use this function usually only carry
                //what the home eg this needs
                if (inv.HasAnythingOf(Output[i])) {
                    Item item = inv.GetAllAndRemoveItem(Output[i]);
                    Output[i].count = Mathf.Clamp(Output[i].count + item.count, 0, MaxOutputStorage);
                }
            }
        }

        public Item[] GetOutput() {
            Item[] temp = new Item[Output.Length];
            for (int i = 0; i < Output.Length; i++) {
                temp[i] = Output[i].CloneWithCount();
                Output[i].count = 0;
                CallOutputChangedCB();
            }
            return temp;
        }

        public virtual Item[] GetOutput(Item[] getItems, int[] maxAmounts) {
            Item[] temp = new Item[Output.Length];
            for (int g = 0; g < getItems.Length; g++) {
                for (int i = 0; i < Output.Length; i++) {
                    if (Output[i].ID != getItems[g].ID) {
                        continue;
                    }
                    temp[i] = Output[i].CloneWithCount();
                    temp[i].count = Mathf.Clamp(temp[i].count, 0, maxAmounts[i]);
                    Output[i].count -= temp[i].count;
                    CallOutputChangedCB();
                }
            }
            return temp;
        }

        public virtual Item[] GetOutputWithItemCountAsMax(Item[] getItems) {
            return GetOutput(getItems, getItems.Select(x=>x.count).ToArray());
        }

        public Item GetOneOutput(Item item) {
            if (Output == null) {
                return null;
            }
            Item inItem = Array.Find(Output, x => x.ID == item.ID);
            Item outItem = inItem.CloneWithCount();
            inItem.count = 0;
            return outItem;
        }

        public void CallOutputChangedCB() {
            cbOutputChange?.Invoke(this);
        }

        public void RegisterOutputChanged(Action<Structure> callbackfunc) {
            cbOutputChange += callbackfunc;
        }

        public void UnregisterOutputChanged(Action<Structure> callbackfunc) {
            cbOutputChange -= callbackfunc;
        }

        public void ResetOutputClaimed() {
            this.outputClaimed = false;
            foreach (Item item in Output) {
                if (item.count > 0) {
                    cbOutputChange?.Invoke(this);
                    return;
                }
            }
        }
        public override void ToggleActive() {
            base.ToggleActive();
            if (isActive) {
                RemoveEffect(GetEffect(INACTIVE_EFFECT_ID), true);
            }
            else {
                AddEffect(new Effect(INACTIVE_EFFECT_ID));
            }
        }
        public override void OnDestroy() {
            if (Workers != null) {
                foreach (Worker item in Workers) {
                    item.Destroy();
                }
            }
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _outputData = null;
        }
        public override void Load() {
            base.Load();
            if (Workers != null) {
                foreach (Worker worker in Workers) {
                    worker.Load(this);
                }
            }
            if (_output != null) {
                _output = _output.ReplaceKeepCounts(OutputData.output);
            }
        }
    }
}