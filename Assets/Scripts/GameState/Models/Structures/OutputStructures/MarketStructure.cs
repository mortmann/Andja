using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Andja.Model {

    public class MarketPrototypData : OutputPrototypData {
        public float takeOverStartGoal = 100;

        //TODO: load this all in
        public float decreaseCaptureSpeed = 0.01f;

        public float maximumCaptureSpeed = 0.05f;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class MarketStructure : OutputStructure, ICapturable {

        #region Serialize

        [JsonPropertyAttribute] public int level = 1;
        [JsonPropertyAttribute] public float capturedProgress = 0;

        #endregion Serialize

        #region RuntimeOrOther

        public List<Structure> RegisteredSturctures;
        public List<Structure> OutputMarkedStructures;

        public float TakeOverStartGoal => CalculateRealValue(nameof(MarketData.takeOverStartGoal), MarketData.takeOverStartGoal);

        public float DecreaseCaptureSpeed => CalculateRealValue(nameof(MarketData.decreaseCaptureSpeed), MarketData.decreaseCaptureSpeed);
        public float MaximumCaptureSpeed => CalculateRealValue(nameof(MarketData.maximumCaptureSpeed), MarketData.maximumCaptureSpeed);

        protected MarketPrototypData _marketData;

        public MarketPrototypData MarketData {
            get {
                if (_marketData == null) {
                    _marketData = (MarketPrototypData)PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _marketData;
            }
        }

        #endregion RuntimeOrOther

        public MarketStructure(string id, MarketPrototypData MarketData) {
            this.ID = id;
            _marketData = MarketData;
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public MarketStructure() {
            RegisteredSturctures = new List<Structure>();
            OutputMarkedStructures = new List<Structure>();
        }

        protected MarketStructure(MarketStructure str) {
            BaseCopyData(str);
        }

        public override Structure Clone() {
            return new MarketStructure(this);
        }

        public override void OnUpdate(float deltaTime) {
            base.UpdateWorker(deltaTime);
            UpdateCaptureProgress(deltaTime);
        }

        public void UpdateCaptureProgress(float deltaTime) {
            if (currentCaptureSpeed > 0) {
                capturedProgress += currentCaptureSpeed * deltaTime;
                //reset the speed so that units can again add their speed
                currentCaptureSpeed = 0;
            }
            else if (capturedProgress > 0) {
                capturedProgress -= DecreaseCaptureSpeed * deltaTime;
            }
            capturedProgress = Mathf.Clamp01(capturedProgress);
        }

        public override void OnBuild() {
            RegisteredSturctures = new List<Structure>();
            OutputMarkedStructures = new List<Structure>();
            WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
            // add all the tiles to the city it was build in
            //dostuff thats happen when build
            City.AddTiles(RangeTiles.Concat(Tiles));
            foreach (Tile rangeTile in RangeTiles) {
                if (rangeTile.City != City) {
                    continue;
                }
                OnStructureAdded(rangeTile.Structure);
            }
            City.RegisterStructureAdded(OnStructureAdded);
        }

        public void OnOutputChangedStructure(Structure str) {
            if (!(str is OutputStructure outstr)) {
                return;
            }
            bool hasOutput = false;
            for (int i = 0; i < outstr.Output.Length; i++) {
                if (outstr.Output[i].count > 0) {
                    hasOutput = true;
                    break;
                }
            }
            if (hasOutput == false) {
                if (OutputMarkedStructures.Contains(str)) {
                    OutputMarkedStructures.Remove(str);
                }
                if (WorkerJobsToDo.ContainsKey(outstr)) {
                    WorkerJobsToDo.Remove(outstr);
                }
                return;
            }

            if (WorkerJobsToDo.ContainsKey(outstr)) {
                WorkerJobsToDo.Remove(outstr);
            }

            HashSet<Route> Routes = GetRoutes();
            //get the roads around the structure
            foreach (Route item in outstr.GetRoutes()) {
                //if one of them is in my roads
                if (Routes.Contains(item)) {
                    //if we are here we can get there through atleast 1 road
                    if (outstr.outputClaimed == false) {
                        WorkerJobsToDo.Add(outstr, null);
                    }
                    if (OutputMarkedStructures.Contains(str)) {
                        OutputMarkedStructures.Remove(str);
                    }
                    return;
                }
            }
            //if were here there is noconnection between here and a the structure
            //so remember it for the case it gets connected to it.
            if (OutputMarkedStructures.Contains(str)) {
                return;
            }
            OutputMarkedStructures.Add(str);
        }

        public override void AddRoadStructure(RoadStructure roadStructure) {
            base.AddRoadStructure(roadStructure);
            roadStructure.Route.AddMarketStructure(this);
            for (int i = 0; i < OutputMarkedStructures.Count; i++) {
                foreach (Route item in ((OutputStructure)OutputMarkedStructures[i]).GetRoutes()) {
                    if (Routes.Contains(item)) {
                        OnOutputChangedStructure(OutputMarkedStructures[i]);
                        break;//breaks only the innerloop eg the routes loop
                    }
                }
            }
        }

        protected override void OnRouteChange(Route o, Route n) {
            base.OnRouteChange(o, n);
            o.RemoveMarketStructure(this);
            n.AddMarketStructure(this);
        }
        public override void RemoveRoute(Route route) {
            base.RemoveRoute(route);
            route.RemoveMarketStructure(this);
        }

        public override void OnDestroy() {
            base.OnDestroy();
            City.RemoveTiles(Tiles);
            City.RemoveTiles(RangeTiles);
        }

        public void OnStructureAdded(Structure structure) {
            if (structure == null) {
                return;
            }
            if (structure is MarketStructure) {
                return;
            }
            if (structure is GrowableStructure) {
                return;
            }
            if (structure.City != City) {
                return;
            }
            if (structure is OutputStructure outstr) {
                if (outstr.ForMarketplace == false) {
                    return;
                }
                if (RangeTiles.Overlaps(structure.Tiles)) {
                    outstr.RegisterOutputChanged(OnOutputChangedStructure);
                    OnOutputChangedStructure(outstr);
                }
            }
        }

        public override Item[] GetRequiredItems(OutputStructure str, Item[] items) {
            if (items == null) {
                items = str.Output;
            }
            List<Item> all = new List<Item>();
            for (int i = items.Length - 1; i >= 0; i--) {
                int space = City.Inventory.GetRemainingSpaceForItem(items[i]);
                //WE need to know what every other marketstructure is getting atm 
                //so we do not get to much of this so look at every worker -> check if they have that item as getting -> else 0
                space -= City.marketStructures.Sum(y => y.Workers.Sum(z => Array.Find(z.ToGetItems, j => items[i].ID == j.ID)?.count ?? 0));
                if (space > 0) {
                    Item item = items[i].Clone();
                    item.count = space;//Mathf.Clamp (items [i].count, 0, space);
                    all.Add(item);
                }
            }
            return all.ToArray();
        }

        public override bool InCityCheck(IEnumerable<Tile> tiles, int playerNumber) {
            return base.InCityCheck(tiles, playerNumber) || GetInRangeTiles(tiles.First()).Count(x => x.City?.PlayerNumber == playerNumber) >= Data.structureRange / 5;
        }

        public override Item[] GetOutput(Item[] getItems, int[] maxAmounts) {
            Item[] temp = new Item[getItems.Length];
            for (int i = 0; i < getItems.Length; i++) {
                temp[i] = City.Inventory.GetItemWithMaxAmount(getItems[i], maxAmounts[i]);
            }
            return temp;
        }
        protected override void OnUpgrade() {
            base.OnUpgrade();
            _marketData = null;
        }
        #region ICapturableImplementation

        private float currentCaptureSpeed = 0f;

        public void Capture(IWarfare warfare, float progress) {
            if (Captured) {
                DoneCapturing(warfare);
                return;
            }
            currentCaptureSpeed = Mathf.Clamp(currentCaptureSpeed + progress, 0, MaximumCaptureSpeed);
        }

        private void DoneCapturing(IWarfare warfare) {
            //either capture it or destroy based on if is a city of that player on that island
            City c = BuildTile.Island.Cities.Find(x => x.PlayerNumber == warfare.PlayerNumber);
            if (c != null) {
                capturedProgress = 0;
                OnDestroy();
                City = c;
                OnBuild();
            }
            else {
                Destroy();
            }
        }

        public bool Captured => Mathf.Approximately(capturedProgress, 1);

        #endregion ICapturableImplementation
    }
}