using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using Andja.Pathfinding;
using UnityEngine;
using Andja.UI.Model;

namespace Andja.Model {

    public enum InputTyp { AND, OR }

    public class ProductionPrototypeData : OutputPrototypData {
        public Item[] intake;
        public InputTyp inputTyp;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ProductionStructure : OutputStructure {
        public const int INTAKE_MULTIPLIER = 5;

        #region Serialize

        private Item[] _intake;

        [JsonPropertyAttribute]
        public Item[] Intake {
            get {
                if (_intake != null) return _intake;
                if (ProductionData.intake == null) {
                    return null;
                }
                if (ProductionData.inputTyp == InputTyp.AND) {
                    _intake = new Item[ProductionData.intake.Length];
                    for (int i = 0; i < ProductionData.intake.Length; i++) {
                        _intake[i] = ProductionData.intake[i].Clone();
                    }
                }
                if (ProductionData.inputTyp != InputTyp.OR) return _intake;
                _intake = new Item[1];
                _intake[0] = ProductionData.intake[0].Clone();
                return _intake;
            }
            set => _intake = value;
        }

        #endregion Serialize

        #region RuntimeOrOther

        private int _orItemIndex = int.MinValue; //TODO think about to switch to short if it needs to save space

        public int OrItemIndex {
            get {
                if (_orItemIndex == int.MinValue) {
                    _orItemIndex = Array.FindIndex(ProductionData.intake, x => x.ID == Intake[0].ID);
                }
                return _orItemIndex;
            }
        }

        public Dictionary<OutputStructure, Item[]> RegisteredStructures;
        protected MarketStructure nearestMarketStructure;
        public InputTyp InputTyp => ProductionData.inputTyp;

        #endregion RuntimeOrOther

        private ProductionPrototypeData _productionData;

        public ProductionPrototypeData ProductionData =>
            _productionData ??= (ProductionPrototypeData)PrototypController.Instance.GetStructurePrototypDataForID(ID);

        public ProductionStructure(string id, ProductionPrototypeData productionData) {
            this.ID = id;
            this._productionData = productionData;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        protected ProductionStructure() {
            RegisteredStructures = new Dictionary<OutputStructure, Item[]>();
        }

        protected ProductionStructure(ProductionStructure str) {
            OutputCopyData(str);
        }

        public override Structure Clone() {
            return new ProductionStructure(this);
        }

        public override void OnUpdate(float deltaTime) {
            if (Output == null) {
                return;
            }
            UpdateWorker(deltaTime);
            if (IsActiveAndWorking == false) {
                return;
            }

            if (Output.Any(item => item.count == MaxOutputStorage)) {
                return;
            }
            if (HasRequiredInput() == false) {
                return;
            }
            ProduceTimer += deltaTime;
            if ((ProduceTimer >= ProduceTime) == false) {
                return;
            }
            ProduceTimer = 0;
            if (Intake != null) {
                for (int i = 0; i < Intake.Length; i++) {
                    Intake[i].count -= Mathf.Clamp(ProductionData.intake[i].count,1, int.MaxValue);
                }
            }
            for (int i = 0; i < Output.Length; i++) {
                Output[i].count += OutputData.output[i].count;
                cbOutputChange?.Invoke(this);
            }
        }

        public bool HasRequiredInput() {
            if (ProductionData.intake == null) {
                return true;
            }
            return InputTyp switch {
                InputTyp.AND => Intake.Where((t, i) => ProductionData.intake[i].count > t.count).Any() == false,
                InputTyp.OR when ProductionData.intake[OrItemIndex].count > Intake[0].count => false,
                _ => true
            };
        }

        protected override void SendOutWorkerIfCan(float workTime = 1) {
            if (workers.Count >= MaxNumberOfWorker || WorkerJobsToDo.Count == 0 && nearestMarketStructure == null) {
                return;
            }
            Dictionary<Item, int> needItems = new Dictionary<Item, int>();
            for (int i = 0; i < Intake.Length; i++) {
                if (GetMaxIntakeForIndex(i) > Intake[i].count) {
                    needItems.Add(Intake[i].Clone(), GetMaxIntakeForIndex(i) - Intake[i].count);
                }
            }
            if (needItems.Count == 0) {
                return;
            }
            if (WorkerJobsToDo.Count == 0 && nearestMarketStructure != null) {
                List<Item> getItems = new List<Item>();
                for (int i = Intake.Length - 1; i >= 0; i--) {
                    if (City.HasAnythingOfItem(Intake[i]) == false) continue;
                    Item item = Intake[i].Clone();
                    item.count = GetMaxIntakeForIndex(i) - Intake[i].count;
                    getItems.Add(item);
                }
                if (getItems.Count <= 0) {
                    return;
                }
                workers.Add(new Worker(this, nearestMarketStructure, workTime, OutputData.workerID, getItems.ToArray(), false));
                World.Current.CreateWorkerGameObject(workers[0]);
            }
            else {
                base.SendOutWorkerIfCan();
            }
        }

        public void OnOutputChangedStructure(Structure str) {
            //it is == false but unity thinks this is the only correct way :)
            if (!(str is OutputStructure os)) {
                return;
            }
            if (WorkerJobsToDo.ContainsKey(os)) {
                WorkerJobsToDo.Remove(os);
            }
            if (os.outputClaimed) return;
            Item[] items = os.Output.Where(x => Array.Exists(Intake, y => x.ID == y.ID && x.count > 0)).ToArray();
            if (items.Length == 0) return;
            WorkerJobsToDo.Add(os, items);
        }

        public bool AddToIntake(Inventory toAdd) {
            if (Intake == null) {
                return false;
            }
            for (int i = 0; i < Intake.Length; i++) {
                Intake[i].count += toAdd.GetItemWithMaxAmount(Intake[i], GetRemainingIntakeSpaceForIndex(i)).count;
                CallbackChangeIfNotNull();
            }
            return true;
        }

        public override Item[] GetRequiredItems(OutputStructure str, Item[] items) {
            List<Item> all = new List<Item>();
            for (int i = 0; i < Intake.Length; i++) {
                Item output = Array.Find(items, x => x.ID == Intake[i].ID);
                if (output == null) continue;
                Item item = output.Clone();
                item.count = GetMaxIntakeForIndex(i) - Intake[i].count;
                item.count -= workers?.Where(z => z.ToGetItems != null)
                    .Sum(x => Array.Find(x.ToGetItems, y => items[i].ID == y.ID)?.count ?? 0) ?? 0;
                item.count = Mathf.Min(item.count, output.count);
                all.Add(item);
            }
            return all.Where(x=>x.count > 0).ToArray();
        }

        protected override void OnUpgrade() {
            base.OnUpgrade();
            _productionData = null;
        }
        public override void OnBuild(bool loading = false) {
            WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
            RegisteredStructures = new Dictionary<OutputStructure, Item[]>();

            if (RangeTiles == null) return;
            foreach (Tile rangeTile in RangeTiles) {
                if (rangeTile.Structure == null) {
                    continue;
                }
                OnStructureBuild(rangeTile.Structure);
            }
            City.RegisterStructureAdded(OnStructureBuild);
        }

        public void ChangeInput(Item change) {
            if (change == null) {
                return;
            }
            if (InputTyp == InputTyp.AND) {
                return;
            }
            for (int i = 0; i < ProductionData.intake.Length; i++) {
                Item item = ProductionData.intake[i];
                if (item.ID == change.ID) {
                    Intake[0] = item.Clone();
                    break;
                }
            }
            ProduceTimer = 0;
        }

        public int GetRemainingIntakeSpaceForIndex(int itemIndex) {
            return GetMaxIntakeForIndex(itemIndex) - Intake[itemIndex].count; //TODO THINK ABOUT THIS
        }

        /// <summary>
        /// Give an index for the needed Item, so only use in for loops
        /// OR
        /// for OR Intake use with orItemIndex
        /// </summary>
        public int GetMaxIntakeForIndex(int itemIndex) {
            return ProductionData.intake[itemIndex].count * INTAKE_MULTIPLIER; //TODO THINK ABOUT THIS
        }

        public void OnStructureBuild(Structure str) {
            OutputStructure outputStructure = str as OutputStructure;
            if (outputStructure == null || outputStructure is GrowableStructure) {
                return;
            }
            if (RangeTiles.Overlaps(str.Tiles) == false) {
                return;
            }
            if (RegisteredStructures.ContainsKey(outputStructure)) {
                return;
            }
            if (str is MarketStructure) {
                FindNearestMarketStructure(str.BuildTile);
                return;
            }
            Item[] items = outputStructure.Output.Where(x => Array.Exists(Intake, y => x.ID == y.ID)).ToArray();
            if (items.Length == 0) return;
            outputStructure.RegisterOutputChanged(OnOutputChangedStructure);
            OnOutputChangedStructure(outputStructure);
            RegisteredStructures.Add(outputStructure, items);
        }

        public void FindNearestMarketStructure(Tile tile) {
            if (!(tile.Structure is MarketStructure market)) return;
            if (WorkerPrototypeData.hasToFollowRoads) {
                if (market.GetRoutes().Overlaps(Routes) == false) {
                    return;
                }
            }
            if (nearestMarketStructure == null) {
                nearestMarketStructure = market;
            }
            else {
                if(WorkerPrototypeData.hasToFollowRoads) {
                    CheckMarketStructureForRoutes(market);
                } else {
                    float firstDistance = nearestMarketStructure.Center.magnitude - Center.magnitude;
                    float distanceToNewMarket = market.Center.magnitude - Center.magnitude;
                    if (Mathf.Abs(distanceToNewMarket) < Mathf.Abs(firstDistance)) {
                        nearestMarketStructure = market;
                    }
                }
            }
            if (WorkerPrototypeData.hasToFollowRoads) {
                nearestMarketStructure.RegisterOnRoutesChangedCallback(CheckMarketStructureForRoutes);
            }
        }

        protected void CheckMarketStructureForRoutes(Structure structure) {
            structure.UnregisterOnRoutesChangedCallback(CheckMarketStructureForRoutes);
            IEnumerable<MarketStructure> toCheck = Routes.SelectMany(r  => r.MarketStructures)
                                                         .Where(m => RangeTiles.Overlaps(m.Tiles));
            if (toCheck.Any() == false) {
                nearestMarketStructure = null;                
                return;
            }
            if (toCheck.Count() == 1) {
                nearestMarketStructure = toCheck.First();
                return;
            }

            PathJob job = new PathJob(new Worker(OutputData.workerID), toCheck.Count()) {
                Grid = Routes.Where(r => r.MarketStructures.Overlaps(toCheck)).Select(r => r.Grid).ToArray()
            };
            job.EndTiles = new List<Vector2>[job.Grid.Length];
            var tiles = toCheck.SelectMany(m => m.Tiles.Select(t=>t.Vector2)).ToList();
            for (int i = 0; i < job.Grid.Length; i++) {
                job.EndTiles[i] = tiles;
            }
            nearestMarketStructure = null;
            PathfindingThreadHandler.EnqueueJob(job, ()=> {
                nearestMarketStructure = World.Current.GetTileAt(job.End).Structure as MarketStructure;
            });
        }

        public override void Load() {
            base.Load();
            if (_intake == null) return;
            if(InputTyp == InputTyp.AND) {
                _intake = _intake.ReplaceKeepCounts(ProductionData.intake);
            } else {
                if (Array.Exists(ProductionData.intake, x => x.ID == _intake[0].ID) != false) return;
                Debug.LogWarning("Prototype Intake Data changed for " + ID + " 'OR' does not contain last produced. " +
                                 "Updated to first in array.");
                _intake[0] = ProductionData.intake[0].Clone();
            }
        }

    }
}