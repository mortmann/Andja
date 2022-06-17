using Andja.Controller;
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

        #region Serialize

        [JsonPropertyAttribute] protected List<Worker> Workers;
        [JsonPropertyAttribute] protected float produceTimer;
        protected Item[] _output; 

        #endregion Serialize

        #region RuntimeOrOther

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
        public WorkerPrototypeData _workerPrototypeData;
        public WorkerPrototypeData WorkerPrototypeData {
            get {
                if(_workerPrototypeData == null)
                    _workerPrototypeData = PrototypController.Instance.GetWorkerPrototypDataForID(OutputData.workerID ?? "placeholder");
                return _workerPrototypeData;
            }
        }
        public Dictionary<OutputStructure, Item[]> jobsToDo;
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
            jobsToDo = new Dictionary<OutputStructure, Item[]>();
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

        protected Tile _jobTile;

        public Tile JobTile {
            get {
                if (_jobTile == null) {
                    return Tiles[0];
                }
                else {
                    return _jobTile;
                }
            }
            set {
                _jobTile = value;
            }
        }

        public virtual float Progress => produceTimer;
        public virtual float TotalProgress => ProduceTime;

        public void UpdateWorker(float deltaTime) {
            if (MaxNumberOfWorker <= 0) {
                return;
            }
            if (Workers == null) {
                Workers = new List<Worker>(MaxNumberOfWorker);
            }
            for (int i = Workers.Count - 1; i >= 0; i--) {
                Worker w = Workers[i];
                w.Update(deltaTime);
                if (w.isAtHome) {
                    WorkerComeBack(w);
                }
            }
            SendOutWorkerIfCan();
        }

        public virtual void SendOutWorkerIfCan(float workTime = 1) {
            if (jobsToDo.Count == 0 || Workers.Count >= MaxNumberOfWorker) {
                return;
            }
            List<OutputStructure> givenJobs = new List<OutputStructure>();
            List<OutputStructure> ordered = jobsToDo.Keys.OrderByDescending(x => x.Output.Sum(y => y.count)).ToList();
            foreach (OutputStructure jobStr in ordered) {
                if (Workers.Count >= MaxNumberOfWorker) {
                    break;
                }
                if (jobStr.outputClaimed) {
                    continue;
                }
                Item[] items = GetRequieredItems(jobStr, jobsToDo[jobStr]);
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
                    jobsToDo.Remove(giveJob);
                }
            }
        }

        public virtual Item[] GetRequieredItems(OutputStructure str, Item[] items) {
            if (items == null) {
                items = str.Output;
            }
            List<Item> all = new List<Item>();
            for (int i = Output.Length - 1; i >= 0; i--) {
                string id = Output[i].ID;
                for (int s = 0; s < items.Length; s++) {
                    if (items[i].ID == id) {
                        Item item = items[i].Clone();
                        item.count = MaxOutputStorage - Output[i].count;
                        if (item.count > 0)
                            all.Add(item);
                    }
                }
            }
            return all.ToArray();
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
            Item[] temp = new Item[getItems.Length];
            for (int g = 0; g < getItems.Length; g++) {
                for (int i = 0; i < Output.Length; i++) {
                    if (Output[i].ID != getItems[g].ID) {
                        continue;
                    }
                    if (Output[i].count == 0) {
                        Debug.LogWarning("output[i].count ==  0");
                    }
                    temp[g] = Output[i].CloneWithCount();
                    temp[g].count = Mathf.Clamp(temp[i].count, 0, getItems[g].count);
                    Output[i].count -= temp[i].count;
                    CallOutputChangedCB();
                }
            }
            return temp;
        }

        public Item GetOneOutput(Item item) {
            if (Output == null) {
                return null;
            }
            for (int i = 0; i < Output.Length; i++) {
                if (item.ID != Output[i].ID) {
                    continue;
                }
                if (Output[i].count > 0) {
                    Item temp = Output[i].CloneWithCount();
                    Output[i].count = 0;
                    CallbackChangeIfnotNull();
                    return temp;
                }
            }
            return null;
        }

        public override Item[] GetBuildingItems() {
            return BuildingItems;
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
        internal override void ToggleActive() {
            base.ToggleActive();
            if (isActive) {
                RemoveEffect(GetEffect("inactive"), true);
            }
            else {
                AddEffect(new Effect("inactive"));
            }
        }
        protected override void OnDestroy() {
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