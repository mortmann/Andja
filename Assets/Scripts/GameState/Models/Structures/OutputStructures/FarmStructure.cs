using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FarmPrototypeData : OutputPrototypData {
    public GrowableStructure growable;
    public int neededHarvestToProduce;
    public float ProducePerMinute() {
        float tileCount = PrototypeRangeTiles.Count;
        if (growable == null || produceTime * efficiency <= 0 || growable.ProduceTime <= 0)
            return 0;
        if (produceTime * efficiency >= growable.ProduceTime)
            return 60f / produceTime;
        return Mathf.Min(60f / (produceTime * efficiency), ((tileCount / (float)neededHarvestToProduce) * (60f / growable.ProduceTime)));//((((tileCount / (float)neededHarvestToProduce) * (grow.produceTime)) / 60f) / produceTime * efficiency) ;
    }
}


[JsonObject(MemberSerialization.OptIn)]
public class FarmStructure : OutputStructure {


    #region Serialize
    [SerializeField] int currentlyHarvested = 0;
    #endregion
    #region RuntimeOrOther

    public GrowableStructure Growable { get { return FarmData.growable; } }

    public int NeededHarvestForProduce { get { return CalculateRealValue("neededHarvestToProduce", FarmData.neededHarvestToProduce); } }

    public int growableReadyCount;
    public int OnRegisterCallbacks;
    List<GrowableStructure> workingGrowables;
    List<GrowableStructure> doneGrowable;
    public override float Progress => produceTimer;
    public override float TotalProgress => ProduceTime * NeededHarvestForProduce;

    protected FarmPrototypeData _farmData;
    public FarmPrototypeData FarmData {
        get {
            if (_farmData == null) {
                _farmData = (FarmPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _farmData;
        }
    }
    #endregion

    public override float EfficiencyPercent {
        get {
            return Mathf.Round(((float)OnRegisterCallbacks / (float)RangeTiles.Count) * 1000) / 10f;
        }
    }
    public FarmStructure(string id, FarmPrototypeData fpd) {
        _farmData = fpd;
        this.ID = id;
    }
    protected FarmStructure(FarmStructure f) {
        OutputCopyData(f);
    }
    /// <summary>
    /// DO NOT USE
    /// </summary>
    public FarmStructure() {
        workingGrowables = new List<GrowableStructure>();
    }

    public override Structure Clone() {
        return new FarmStructure(this);
    }


    public override void OnBuild() {
        workingGrowables = new List<GrowableStructure>();
        if (Growable == null) {
            return;
        }
        //farm has it needs plant if it can 
        foreach (Tile rangeTile in RangeTiles) {
            if (rangeTile.Structure != null) {
                if (rangeTile.Structure.ID == Growable.ID) {
                    rangeTile.Structure.RegisterOnChangedCallback(OnGrowableChanged);
                    OnRegisterCallbacks++;
                    if (((GrowableStructure)rangeTile.Structure).hasProduced == true) {
                        growableReadyCount++;
                        workingGrowables.Add((GrowableStructure)rangeTile.Structure);
                    }
                }
            }
        }
        foreach (Tile rangeTile in RangeTiles) {
            rangeTile.RegisterTileOldNewStructureChangedCallback(OnTileStructureChange);
        }
    }
    public override void OnUpdate(float deltaTime) {
        if (Output[0].count >= MaxOutputStorage) {
            return;
        }
        if (Growable != null) {
            if (workingGrowables.Count == 0)
                return;
            GrowableStructure g = (GrowableStructure)workingGrowables[0];
            float distance = Vector2.Distance(g.MiddleVector, MiddleVector) - (Width + Height) / 4;
            float walkTime = (2f * distance + 2) / Worker.Speed;
            if (walkTime > ProduceTime || MaxNumberOfWorker == 0) {
                //Display Warning?
                Debug.LogWarning("FARM " + Name + " can not send worker -- ProduceTime to fast.");
                produceTimer += deltaTime * Efficiency;
                if (produceTimer >= ProduceTime) {
                    if (growableReadyCount == 0) {
                        return;
                    }
                    produceTimer = 0;
                    if (Growable != null) {
                        //GrowableStructure g = (GrowableStructure)workingGrowables[0];
                        AddHarvastable();
                        ((GrowableStructure)g).Harvest();
                    }
                }
            } else {
                //does this work?
                produceTimer += deltaTime * Efficiency;
                if (Workers == null) {
                    Workers = new List<Worker>();
                }
                for (int i = Workers.Count - 1; i >= 0; i--) {
                    Worker w = Workers[i];
                    w.Update(deltaTime * Efficiency);
                    if (w.isAtHome) {
                        WorkerComeBack(w);
                    }
                }
                SendOutWorkerIfCan(ProduceTime - walkTime - 3);
            }
        } 
        if (currentlyHarvested >= NeededHarvestForProduce) {
            Output[0].count++;
            cbOutputChange?.Invoke(this);
            currentlyHarvested -= NeededHarvestForProduce;
        }
    }
    public void AddHarvastable() {
        currentlyHarvested++;
    }
    public void OnGrowableChanged(Structure str) {
        if (str is GrowableStructure == false) {
            str.UnregisterOnChangedCallback(OnGrowableChanged);
            return;
        }
        GrowableStructure grow = (GrowableStructure)str;
        if (grow.ID != Growable.ID) {
            grow.UnregisterOnChangedCallback(OnGrowableChanged);
            return;
        }
        if (((GrowableStructure)grow).hasProduced == false) {
            if (workingGrowables.Contains((GrowableStructure)grow)) {
                growableReadyCount--;
                workingGrowables.Remove(grow);
            }
            return;
        }
        workingGrowables.Add(grow);
        growableReadyCount++;
        // send worker todo this job
        // not important right now
    }
    public void OnTileStructureChange(Structure now, Structure old) {
        if (old != null && old.ID == Growable.ID) {
            OnRegisterCallbacks--;
        }
        if (now == null) {
            return;
        }
        if (now.ID == Growable.ID) {
            OnRegisterCallbacks++;
            now.RegisterOnChangedCallback(OnGrowableChanged);
            GrowableStructure g = now as GrowableStructure;
            if (g.hasProduced) {
                //we need to check if its done
                //if so we need to get it queued for work!
                OnGrowableChanged(g);
            }
        }
    }

    public override void SendOutWorkerIfCan(float workTime = 1) {
        if (growableReadyCount == 0) {
            return;
        }
        if (Workers.Count >= MaxNumberOfWorker) {
            return;
        }
        Worker ws = new Worker(this, workingGrowables[0], workTime, Output, WorkerWorkSound, false);
        workingGrowables.RemoveAt(0);
        World.Current.CreateWorkerGameObject(ws);
        Workers.Add(ws);
    }


    protected override void OnDestroy() {
        if (Workers == null) {
            return;
        }
        foreach (Worker item in Workers) {
            item.Destroy();
        }
    }
    public override object GetExtraBuildUIData() {
        return EfficiencyPercent;
    }
    public override void UpdateExtraBuildUI(GameObject parent, Tile t) {
        //FIXME
        //TODO
        HashSet<Tile> hs = this.GetInRangeTiles(t);
        if (hs == null) {
            return;
        }
        float percentage = 0;
        int count = 0;
        foreach (Tile item in hs) {
            if (item == null) {
                continue;
            }
            if (item.Structure != null && item.Structure.ID == Growable.ID) {
                count++;
            }
            else
            if (item.Structure == null && Tile.IsBuildType(item.Type)) {
                count++;
            }
        }
        percentage = Mathf.RoundToInt(((float)count / (float)hs.Count) * 100);

        if (Growable.Fertility != null) {
            if (t.Island == null) {
                return;
            }
            if (t.Island.Fertilities.Contains(Growable.Fertility) == false) {
                percentage = 0;
            }
            else {
                //TODO calculate the perfect grow environment?

            }
        }
        parent.GetComponentInChildren<SpriteSlider>().ChangePercent(percentage);

    }

}
