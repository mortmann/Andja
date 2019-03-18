using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

public class OutputPrototypData : StructurePrototypeData {
    public float contactRange = 0;
    public bool forMarketplace = true;
    public int maxNumberOfWorker = 1;
    public float produceTime;
    public int maxOutputStorage;
    public float efficiency = 1f;
    public Item[] output;
}


[JsonObject(MemberSerialization.OptIn)]
public abstract class OutputStructure : TargetStructure {
    #region Serialize
    [JsonPropertyAttribute] public List<Worker> myWorker;
    [JsonPropertyAttribute] public float produceCountdown;
    protected Item[] _output; // FIXME DOESNT GET LOADED IN!??!? why? fixed?
    #endregion
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
    public Dictionary<OutputStructure, Item[]> jobsToDo;
    public bool outputClaimed;
    protected Action<Structure> cbOutputChange;
    bool CanWork { get { return EfficiencyPercent > 0; } }
    public bool workersHasToFollowRoads = false;
    public float ContactRange { get { return OutputData.contactRange; } }
    public bool ForMarketplace { get { return OutputData.forMarketplace; } }

    public float ProduceTime { get { return CalculateRealValue("produceTime", OutputData.produceTime); } }
    public int MaxNumberOfWorker { get { return CalculateRealValue("maxNumberOfWorker", OutputData.maxNumberOfWorker); } }
    public float Efficiency { get { return CalculateRealValue("efficiency", OutputData.efficiency); } }
    public int MaxOutputStorage { get { return CalculateRealValue("maxOutputStorage", OutputData.maxOutputStorage); } }

    protected OutputPrototypData _outputData;
    public OutputPrototypData OutputData {
        get {
            if (_outputData == null) {
                _outputData = (OutputPrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _outputData;
        }
    }
    #endregion

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
                return myStructureTiles[0];
            }
            else {
                return _jobTile;
            }
        }
        set {
            _jobTile = value;
        }
    }
    public void Update_Worker(float deltaTime) {
        if (MaxNumberOfWorker <= 0) {
            return;
        }
        if (myWorker == null) {
            myWorker = new List<Worker>();
        }
        for (int i = myWorker.Count - 1; i >= 0; i--) {
            Worker w = myWorker[i];
            w.Update(deltaTime);
            if (w.isAtHome) {
                WorkerComeBack(w);
            }
        }

        SendOutWorkerIfCan();
    }
    public virtual void SendOutWorkerIfCan() {
        if (jobsToDo.Count == 0) {
            return;
        }
        List<OutputStructure> givenJobs = new List<OutputStructure>();
        foreach (OutputStructure jobStr in jobsToDo.Keys) {
            if (myWorker.Count >= MaxNumberOfWorker) {
                break;
            }
            if (jobStr.outputClaimed) {
                continue;
            }
            Item[] items = GetRequieredItems(jobStr, jobsToDo[jobStr]);
            if (items == null || items.Length <= 0) {
                continue;
            }
            if(workersHasToFollowRoads && CanReachStructure(jobStr) == false) {
                continue;
            }
            Worker ws = new Worker(this, jobStr, 1, items, workersHasToFollowRoads);

            givenJobs.Add(jobStr);
            World.Current.CreateWorkerGameObject(ws);
            myWorker.Add(ws);
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
            int id = Output[i].ID;
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
        if (myWorker.Contains(w) == false) {
            Debug.LogError("WorkerComeBack - Worker comesback, but doesnt live here!");
            return;
        }
        w.Destroy();
        myWorker.Remove(w);
    }

    public void AddToOutput(Inventory inv) {
        for (int i = 0; i < Output.Length; i++) {
            //maybe switch to manually foreach because it may be faster
            //because worker that use this function usually only carry 
            //what the home eg this needs
            if (inv.ContainsItemWithID(Output[i].ID)) {
                Item item = inv.GetAllOfItem(Output[i]);
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
        Item[] temp = new Item[Output.Length];
        for (int g = 0; g < getItems.Length; g++) {
            for (int i = 0; i < Output.Length; i++) {
                if (Output[i].ID != getItems[g].ID) {
                    continue;
                }
                if (Output[i].count == 0) {
                    Debug.LogWarning("output[i].count ==  0");
                }
                temp[i] = Output[i].CloneWithCount();
                temp[i].count = Mathf.Clamp(temp[i].count, 0, getItems[i].count);
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
    public List<Route> GetMyRoutes() {
        List<Route> myRoutes = new List<Route>();
        HashSet<Tile> neighbourTiles = new HashSet<Tile>();
        foreach (Tile item in myStructureTiles) {
            foreach (Tile nbt in item.GetNeighbours()) {
                if (myStructureTiles.Contains(nbt) == false) {
                    neighbourTiles.Add(nbt);
                }
            }
        }
        foreach (Tile t in neighbourTiles) {
            if (t.Structure == null) {
                continue;
            }
            if (t.Structure is RoadStructure) {
                myRoutes.Add(((RoadStructure)t.Structure).Route);
            }
        }
        return myRoutes;
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
    protected override void OnDestroy() {
        if (myWorker != null) {
            foreach (Worker item in myWorker) {
                item.Destroy();
            }
        }

    }

}
