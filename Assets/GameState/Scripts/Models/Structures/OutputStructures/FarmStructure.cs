using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

public class FarmPrototypData : OutputPrototypData {
    public GrowableStructure growable;
    public int neededHarvestToProduce;
}


[JsonObject(MemberSerialization.OptIn)]
public class FarmStructure : OutputStructure {


    #region Serialize
    [SerializeField] int currentlyHarvested = 0;
    #endregion
    #region RuntimeOrOther

    public GrowableStructure Growable { get { return FarmData.growable; } }
    public int NeededHarvestForProduce { get { return FarmData.neededHarvestToProduce; } }

    public int growableReadyCount;
    public int OnRegisterCallbacks;
    List<GrowableStructure> workingGrowables;

    protected FarmPrototypData _farmData;
    public FarmPrototypData FarmData {
        get {
            if (_farmData == null) {
                _farmData = (FarmPrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
            }
            return _farmData;
        }
    }
    #endregion

    public override float Efficiency {
        get {
            return Mathf.Round(((float)OnRegisterCallbacks / (float)myRangeTiles.Count) * 1000) / 10f;
        }
    }
    public FarmStructure(int id, FarmPrototypData fpd) {
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
        foreach (Tile rangeTile in myRangeTiles) {
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
        foreach (Tile rangeTile in myRangeTiles) {
            rangeTile.RegisterTileOldNewStructureChangedCallback(OnTileStructureChange);
        }
    }
    public override void Update(float deltaTime) {
        if (growableReadyCount == 0) {
            return;
        }
        if (Output[0].count >= MaxOutputStorage) {
            return;
        }
        //TODO: send out worker to collect goods
        produceCountdown += deltaTime;
        if (produceCountdown >= ProduceTime) {
            produceCountdown = 0;
            if (Growable != null) {
                GrowableStructure g = (GrowableStructure)workingGrowables[0];
                currentlyHarvested++;
                ((GrowableStructure)g).Harvest();
            }
        }
        if (currentlyHarvested >= NeededHarvestForProduce) {
            Output[0].count++;
            cbOutputChange?.Invoke(this);
            currentlyHarvested -= NeededHarvestForProduce;
        }
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
    protected override void OnDestroy() {
        if (myWorker == null) {
            return;
        }
        foreach (Worker item in myWorker) {
            item.Destroy();
        }
    }
    public override object GetExtraBuildUIData() {
        return Efficiency;
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
            if (t.MyIsland == null) {
                return;
            }
            if (t.MyIsland.myFertilities.Contains(Growable.Fertility) == false) {
                percentage = 0;
            }
            else {
                //TODO calculate the perfect grow environment?

            }
        }

        parent.GetComponentInChildren<SpriteSlider>().ChangePercent(percentage);

    }

}
