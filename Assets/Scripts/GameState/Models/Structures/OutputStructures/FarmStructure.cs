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
        public float fulfillmentPercentage = 0.9f; 
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class FarmStructure : OutputStructure {

        #region Serialize

        [SerializeField] public int currentlyHarvested { get; protected set; } = 0;

        #endregion Serialize

        #region RuntimeOrOther

        public GrowableStructure Growable => FarmData.growable;

        public int NeededHarvestForProduce => CalculateRealValue(nameof(FarmData.neededHarvestToProduce), FarmData.neededHarvestToProduce);

        //TODO: this has to be checked against other working this? (especially for no growables)
        public int WorkingTilesCount;
        protected List<GrowableStructure> readyToHarvestGrowable;
        public override float Progress => CalculateProgress();
        public float FulfillmentPercentage => FarmData.fulfillmentPercentage;
        public override float TotalProgress => ProduceTime * NeededHarvestForProduce;

        private FarmPrototypeData _farmData;

        public FarmPrototypeData FarmData =>
            _farmData ??= (FarmPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        #endregion RuntimeOrOther

        public override float EfficiencyPercent => Mathf.Round((GetFullWorkedTiles() / (float)RangeTiles.Count) * 1000) / 10f;

        private float GetFullWorkedTiles() {
            if (Growable == null) return WorkingTilesCount;
            return RangeTiles.Where(t => Growable.ID.Equals(t.Structure?.ID))
                .Select(t => t.Structure as GrowableStructure)
                .Select(g => 1f / g.BeingWorkedBy).Sum();
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
            readyToHarvestGrowable = new List<GrowableStructure>();
        }

        public override Structure Clone() {
            return new FarmStructure(this);
        }

        public override void OnBuild(bool loading = false) {
            readyToHarvestGrowable = new List<GrowableStructure>();
            foreach (var rangeTile in RangeTiles.Where(rangeTile => Growable != null || rangeTile.Structure == null
                                            || PrototypController.Instance.AllNaturalSpawningStructureIDs.Contains(rangeTile.Structure.ID))) {
                OnTileStructureChange(rangeTile.Structure, null);
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
                DoWorkWithGrowableNoWorker(deltaTime);
            }
            else {
                DoWorkNoGrowable(deltaTime);
            }
            CheckForOutputProduced();
        }

        public void CheckForOutputProduced() {
            if (currentlyHarvested < NeededHarvestForProduce) return;
            Output[0].count++;
            cbOutputChange?.Invoke(this);
            currentlyHarvested -= NeededHarvestForProduce;
        }

        public void DoWorkNoGrowable(float deltaTime) {
            ProduceTimer += deltaTime * Efficiency
                                * Mathf.Clamp01((float)WorkingTilesCount / (RangeTiles.Count * FulfillmentPercentage));
            if (!(ProduceTimer >= ProduceTime)) return;
            ProduceTimer = 0;
            AddHarvastable();
        }

        public void DoWorkWithGrowableNoWorker(float deltaTime) {
            if (MaxNumberOfWorker != 0) return;
            if (readyToHarvestGrowable.Count == 0)
                return;
            ProduceTimer += deltaTime * Efficiency;
            if ((ProduceTimer >= ProduceTime) == false) return;
            ProduceTimer = 0;
            AddHarvastable();
            readyToHarvestGrowable[0].Harvest();
            readyToHarvestGrowable.RemoveAt(0);
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
                    if (readyToHarvestGrowable.Contains(grow)) {
                        readyToHarvestGrowable.Remove(grow);
                    }
                    return;
                }
                readyToHarvestGrowable.Add(grow);
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
            if (Growable == null) {
                if (now == null || PrototypController.Instance.AllNaturalSpawningStructureIDs.Contains(now.ID)) {
                    WorkingTilesCount++;
                }
                else
                if (old == null && PrototypController.Instance.AllNaturalSpawningStructureIDs.Contains(now.ID) == false) {
                    WorkingTilesCount--;
                }
                return;
            }
            if (old != null && old.ID == Growable.ID) {
                WorkingTilesCount--;
            }
            if (now == null) {
                return;
            }
            if (now.ID != Growable.ID) return;
            WorkingTilesCount++;
            now.RegisterOnChangedCallback(OnGrowableChanged);
            GrowableStructure g = now as GrowableStructure;
            g.SetBeingWorked(true);
            if (g.hasProduced) {
                //we need to check if its done
                //if so we need to get it queued for work!
                OnGrowableChanged(g);
            }
        }

        protected override void SendOutWorkerIfCan(float workTime = 1) {
            if (workers.Count >= MaxNumberOfWorker) {
                return;
            }
            if (readyToHarvestGrowable.Count == 0) {
                return;
            }
            GrowableStructure workStructure = readyToHarvestGrowable.FirstOrDefault(g => g.OutputClaimed == false);
            if (workStructure == null) {
                return;
            }
            if (Output[0].count == MaxOutputStorage) {
                return;
            }
            if(NeededHarvestForProduce == currentlyHarvested + workers.Count) {
                return;
            }
            readyToHarvestGrowable.Remove(workStructure);
            Worker ws = new Worker(this, workStructure, ProduceTime,
                                    OutputData.workerID ?? "placeholder", workStructure.Output,
                                    true, ProduceTime * 0.05f);
            World.Current.CreateWorkerGameObject(ws);
            workers.Add(ws);
        }

        private float CalculateProgress() {
            if (IsActiveAndWorking == false)
                return 0;
            if (MaxNumberOfWorker == 0 || Growable == null) {
                return ProduceTimer + currentlyHarvested * ProduceTime;
            }
            if (MaxNumberOfWorker > NeededHarvestForProduce - currentlyHarvested) {
                return currentlyHarvested * ProduceTime
                     + workers.Where(x => x.IsWorking())
                              .OrderBy(x => x.WorkTimer)
                              .Take(NeededHarvestForProduce - currentlyHarvested)
                              .Sum(x => ProduceTime - x.WorkTimer);
            }
            return currentlyHarvested * ProduceTime
                     + (workers.FindAll(x => x.IsWorking())).Sum(x => ProduceTime - x.WorkTimer);
        }

        public override void OnDestroy() {
            if (workers == null) {
                return;
            }
            foreach (Worker item in workers) {
                item.Destroy();
            }
            foreach (Tile tile in RangeTiles) {
                if (tile.Structure is GrowableStructure g && g.ID == Growable.ID) {
                    g.SetBeingWorked(false);
                }
            }
        }

        public override object GetExtraBuildUIData() {
            return EfficiencyPercent;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _farmData = null;
        }
        public override void UpdateExtraBuildUI(GameObject parent, Tile t) {
            //FIXME
            //TODO
            HashSet<Tile> hs = this.GetInRangeTiles(t);
            if (hs == null) {
                return;
            }
            int count = 0;
            foreach (var item in hs.Where(item => item != null)) {
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