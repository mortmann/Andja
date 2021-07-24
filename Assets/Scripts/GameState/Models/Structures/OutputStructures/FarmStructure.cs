using Andja.Controller;
using Andja.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class FarmPrototypeData : OutputPrototypData {
        public GrowableStructure growable;
        public int neededHarvestToProduce;
        //this is either/or/none with growable -- if it doesnt have any specific growable 
        //this can specify which fertility is required to be active
        public Fertility fertility;
        //how many tile have to be empty when no growable is present
        public float fullfillmentPercantage = 0.9f; 
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class FarmStructure : OutputStructure {

        #region Serialize

        [SerializeField] private int currentlyHarvested = 0;

        #endregion Serialize

        #region RuntimeOrOther

        public GrowableStructure Growable { get { return FarmData.growable; } }

        public int NeededHarvestForProduce { get { return CalculateRealValue(nameof(FarmData.neededHarvestToProduce), FarmData.neededHarvestToProduce); } }

        public int OnRegisterCallbacks;
        private List<GrowableStructure> workingGrowables;
        public override float Progress => CalculateProgress();
        public float FullfillmentPercantage => FarmData.fullfillmentPercantage;
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

        #endregion RuntimeOrOther

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
            //farm has it needs plant if it can
            if (Growable != null) {
                foreach (Tile rangeTile in RangeTiles) {
                    if (rangeTile.Structure != null && rangeTile.Structure.ID == Growable.ID) {
                        rangeTile.Structure.RegisterOnChangedCallback(OnGrowableChanged);
                        OnRegisterCallbacks++;
                        if (((GrowableStructure)rangeTile.Structure).hasProduced == true) {
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
            UpdateWorker(deltaTime);
            if (IsActiveAndWorking == false || Output[0].count >= MaxOutputStorage) {
                return;
            }
            if (Growable != null) {
                if (MaxNumberOfWorker == 0) {
                    if (workingGrowables.Count == 0)
                        return;
                    produceTimer += deltaTime * Efficiency;
                    //Display Warning?
                    //Debug.LogWarning("FARM " + Name + " can not send worker -- ProduceTime to fast.");
                    if (produceTimer >= ProduceTime) {
                        if (workingGrowables.Count == 0) {
                            return;
                        }
                        produceTimer = 0;
                        AddHarvastable();
                        workingGrowables[0].Harvest();
                        workingGrowables.RemoveAt(0);
                    }
                }
            } else {
                produceTimer += deltaTime * Efficiency * Mathf.Clamp01(OnRegisterCallbacks/(RangeTiles.Count * FullfillmentPercantage));
                if (produceTimer >= ProduceTime) {
                    produceTimer = 0;
                    AddHarvastable();
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
            if (str is GrowableStructure grow) {
                if (grow.ID != Growable.ID) {
                    grow.UnregisterOnChangedCallback(OnGrowableChanged);
                    return;
                }
                if (grow.hasProduced == false) {
                    if (workingGrowables.Contains(grow)) {
                        workingGrowables.Remove(grow);
                    }
                    return;
                }
                workingGrowables.Add(grow);
            }
            else {
                str.UnregisterOnChangedCallback(OnGrowableChanged);
            }
        }
        /// <summary>
        /// When growable is null -- empty space is counted as workables
        /// </summary>
        /// <param name="obj"></param>
        public void OnTileStructureChange(Structure now, Structure old) {
            if(Growable == null) {
                if (now == null) {
                    OnRegisterCallbacks++;
                }
                if (old == null) {
                    OnRegisterCallbacks--;
                }
                return;
            }
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
            if (workingGrowables.Count == 0) {
                return;
            }
            if (Workers.Count >= MaxNumberOfWorker) {
                return;
            }
            Worker ws = new Worker(this, workingGrowables[0], ProduceTime, 
                                    OutputData.workerID ?? "placeholder", Output, 
                                    true, ProduceTime * 0.05f);
            workingGrowables.RemoveAt(0);
            World.Current.CreateWorkerGameObject(ws);
            Workers.Add(ws);
        }

        private float CalculateProgress() {
            if (IsActiveAndWorking == false)
                return 0;
            if (MaxNumberOfWorker == 0) {
                return produceTimer;
            }
            if (MaxNumberOfWorker > NeededHarvestForProduce) {
                float sum = 0;
                for (int x = 0; x < MaxNumberOfWorker; x++) {
                    if(Workers[x].IsWorking())
                        sum = ProduceTime - Workers[x].WorkTimer;
                }
                sum /= MaxNumberOfWorker;
                return (sum);
            }
            return (Workers.FindAll(x => x.IsWorking())).Sum(x => ProduceTime - x.WorkTimer);
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
            float percentage = Mathf.RoundToInt(((float)count / (float)hs.Count) * 100);

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
}