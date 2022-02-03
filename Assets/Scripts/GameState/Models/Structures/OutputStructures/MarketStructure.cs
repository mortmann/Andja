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
        public List<Structure> OutputMarkedSturctures;

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
            OutputMarkedSturctures = new List<Structure>();
        }

        protected MarketStructure(MarketStructure str) {
            BaseCopyData(str);
        }

        public override Structure Clone() {
            return new MarketStructure(this);
        }

        public override void OnUpdate(float deltaTime) {
            base.UpdateWorker(deltaTime);
            if (currentCaptureSpeed > 0) {
                capturedProgress += currentCaptureSpeed * deltaTime;
            }
            else if (capturedProgress > 0) {
                capturedProgress -= DecreaseCaptureSpeed * deltaTime;
                capturedProgress = Mathf.Clamp01(capturedProgress);
            }
        }

        public override void OnBuild() {
            RegisteredSturctures = new List<Structure>();
            OutputMarkedSturctures = new List<Structure>();
            jobsToDo = new Dictionary<OutputStructure, Item[]>();
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
            OutputStructure outstr = str as OutputStructure;
            if (outstr == null) {
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
                if (OutputMarkedSturctures.Contains(str)) {
                    OutputMarkedSturctures.Remove(str);
                }
                if (jobsToDo.ContainsKey(outstr)) {
                    jobsToDo.Remove(outstr);
                }
                return;
            }

            if (jobsToDo.ContainsKey(outstr)) {
                jobsToDo.Remove(outstr);
            }

            HashSet<Route> Routes = GetRoutes();
            //get the roads around the structure
            foreach (Route item in outstr.GetRoutes()) {
                //if one of them is in my roads
                if (Routes.Contains(item)) {
                    //if we are here we can get there through atleast 1 road
                    if (outstr.outputClaimed == false) {
                        jobsToDo.Add(outstr, null);
                    }
                    if (OutputMarkedSturctures.Contains(str)) {
                        OutputMarkedSturctures.Remove(str);
                    }
                    return;
                }
            }
            //if were here there is noconnection between here and a the structure
            //so remember it for the case it gets connected to it.
            if (OutputMarkedSturctures.Contains(str)) {
                return;
            }
            OutputMarkedSturctures.Add(str);
        }

        public override void AddRoadStructure(RoadStructure roadStructure) {
            base.AddRoadStructure(roadStructure);
            roadStructure.Route.AddMarketStructure(this);
        }
        protected override void OnRouteChange(Route o, Route n) {
            base.OnRouteChange(o, n);
            o.RemoveMarketStructure(this);
            n.AddMarketStructure(this);
        }
        protected override void RemoveRoute(Route route) {
            base.RemoveRoute(route);
            route.RemoveMarketStructure(this);
        }

        protected override void OnDestroy() {
            base.OnDestroy();
            List<Tile> h = new List<Tile>(Tiles);
            h.AddRange(RangeTiles);
            City.RemoveTiles(h);
        }

        public void OnStructureAdded(Structure structure) {
            if (structure == null) {
                return;
            }
            if (this == structure) {
                return;
            }
            if (structure.City != City) {
                return;
            }
            if (structure is OutputStructure outstr) {
                if (outstr.ForMarketplace == false) {
                    return;
                }
                foreach (Tile item in structure.Tiles) {
                    if (RangeTiles.Contains(item)) {
                        outstr.RegisterOutputChanged(OnOutputChangedStructure);
                        OnOutputChangedStructure(outstr);
                        break;
                    }
                }
            }
            //IF THIS is a pathfinding structure check for new road
            //if true added that to the myroads

            if (structure.StructureTyp == StructureTyp.Pathfinding) {
                HashSet<Route> Routes = GetRoutes();
                if (Routes == null || Routes.Count == 0)
                    return;
                if (NeighbourTiles.Contains(structure.Tiles[0])) {
                    if (Routes.Contains(((RoadStructure)structure).Route) == false) {
                        Routes.Add(((RoadStructure)structure).Route);
                    }
                }
                for (int i = 0; i < OutputMarkedSturctures.Count; i++) {
                    foreach (Route item in ((OutputStructure)OutputMarkedSturctures[i]).GetRoutes()) {
                        if (Routes.Contains(item)) {
                            OnOutputChangedStructure(OutputMarkedSturctures[i]);
                            break;//breaks only the innerloop eg the routes loop
                        }
                    }
                }
            }
        }

        public override Item[] GetRequieredItems(OutputStructure str, Item[] items) {
            if (items == null) {
                items = str.Output;
            }
            List<Item> all = new List<Item>();
            for (int i = items.Length - 1; i >= 0; i--) {
                int space = City.Inventory.GetSpaceFor(items[i]);
                //WE need to know what every other marketstructure is getting atm 
                //so we do not get to much of this so look at every worker -> check if they have that item as getting -> else 0
                space -= City.marketStructures.Sum(y => y.Workers.Sum(z => Array.Find(z.toGetItems, j => items[i].ID == j.ID)?.count ?? 0));
                if (space <= 0) {
                
                }
                else {
                    Item item = items[i].Clone();
                    item.count = space;//Mathf.Clamp (items [i].count, 0, space);
                    all.Add(item);
                }
            }
            return all.ToArray();
        }

        public override Item[] GetOutputWithItemCountAsMax(Item[] getItems) {
            Item[] temp = new Item[getItems.Length];
            for (int i = 0; i < getItems.Length; i++) {
                //if(City.inventory.GetAmountForItem (getItems[i]) == 0){
                //	continue;
                //}
                temp[i] = City.Inventory.GetItemWithMaxAmount(getItems[i], getItems[i].count);
            }
            return temp;
        }

        public override bool InCityCheck(IEnumerable<Tile> tiles, int playerNumber) {
            return base.InCityCheck(tiles, playerNumber) || GetInRangeTiles(tiles.First()).Count(x => x.City?.PlayerNumber == playerNumber) >= Data.structureRange / 5;
        }

        public override Item[] GetOutput(Item[] getItems, int[] maxAmounts) {
            Item[] temp = new Item[getItems.Length];
            for (int i = 0; i < getItems.Length; i++) {
                //if(City.inventory.GetAmountForItem (getItems[i]) == 0){
                //	continue;
                //}
                if (getItems[i] == null || maxAmounts == null) {
                    Debug.Log("s");
                }
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
                OnDestroy();
                City = c;
                OnBuild();
            }
            else {
                Destroy();
            }
        }

        public bool Captured => capturedProgress == 1;

        #endregion ICapturableImplementation
    }
}