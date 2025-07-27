using Andja.Controller;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn)]
    public class MarketStructure : OutputStructure {

        #region RuntimeOrOther

        public List<Structure> RegisteredSturctures;
        public List<OutputStructure> OutputMarkedStructures;
        #endregion RuntimeOrOther

        public MarketStructure(string id, OutputPrototypData marketData) : this(){
            ((IBaseThing)this).ID = id;
            _outputData = marketData;
            AddElement(new AddRangeTilesToCity(this));
            AddElement(new Capturable(this));
        }

        /// <summary>
        /// DO NOT USE
        /// </summary>
        public MarketStructure() {
            RegisteredSturctures = new List<Structure>();
            OutputMarkedStructures = new List<OutputStructure>();
        }

        protected MarketStructure(MarketStructure str) {
            BaseCopyData(str);
        }

        public override Structure Clone() {
            return new MarketStructure(this);
        }

        protected override void OnUpdate(float deltaTime) {
            base.UpdateWorker(deltaTime);
        }

        public override void OnBuild(bool loading = false) {
            RegisteredSturctures = new List<Structure>();
            OutputMarkedStructures = new List<OutputStructure>();
            WorkerJobsToDo = new Dictionary<OutputStructure, Item[]>();
            // add all the tiles to the city it was build in
            //dostuff thats happen when build
            foreach (var rangeTile in RangeTiles.Where(rangeTile => rangeTile.City == City)) {
                OnStructureAdded(rangeTile.Structure);
            }
            City.RegisterStructureAdded(OnStructureAdded);
        }

        public void OnOutputChangedStructure(Structure str) {
            if (!(str is OutputStructure outputStructure)) {
                return;
            }
            bool hasOutput = outputStructure.Output.Any(t => t.count > 0);
            if (hasOutput == false) {
                if (OutputMarkedStructures.Contains(outputStructure)) {
                    OutputMarkedStructures.Remove(outputStructure);
                }
                if (WorkerJobsToDo.ContainsKey(outputStructure)) {
                    WorkerJobsToDo.Remove(outputStructure);
                }
                return;
            }

            if (WorkerJobsToDo.ContainsKey(outputStructure)) {
                WorkerJobsToDo.Remove(outputStructure);
            }

            //get the roads around the structure
            if (outputStructure.GetRoutes().Any(item => Routes.Contains(item))) {
                if (outputStructure.OutputClaimed == false) {
                    WorkerJobsToDo.Add(outputStructure, null);
                }
                if (OutputMarkedStructures.Contains(outputStructure)) {
                    OutputMarkedStructures.Remove(outputStructure);
                }
                return;
            }
            //if were here there is noconnection between here and a the structure
            //so remember it for the case it gets connected to it.
            if (OutputMarkedStructures.Contains(outputStructure)) {
                return;
            }
            OutputMarkedStructures.Add(outputStructure);
        }

        public override void AddRoadStructure(RoadStructure roadStructure) {
            base.AddRoadStructure(roadStructure);
            roadStructure.Route.AddMarketStructure(this);
            foreach (var markedStructures in OutputMarkedStructures
                         .Where(markedStructures => markedStructures.GetRoutes()
                         .Any(item => Routes.Contains(item))).ToArray()) {
                OnOutputChangedStructure(markedStructures);
            }
        }
        public override void ClaimOutput() {
        }
        public override void ResetOutputClaimed() {
        }
        protected override void OnRouteChange(Route o, Route n) {
            base.OnRouteChange(o, n);
            o.RemoveMarketStructure(this);
            n.AddMarketStructure(this);
            OutputMarkedStructures.ToList().ForEach(OnOutputChangedStructure);
        }
        public override void RemoveRoute(Route route) {
            base.RemoveRoute(route);
            route.RemoveMarketStructure(this);
        }

        public void OnStructureAdded(Structure structure) {
            OutputStructure outputStructure = structure as OutputStructure;
            switch (outputStructure) {
                case null:
                case MarketStructure _:
                case GrowableStructure _:
                case { ForMarketplace: false }:
                    return;
            }
            if (structure.City != City) {
                return;
            }
            if (RangeTiles.Overlaps(outputStructure.Tiles) == false) return;
            outputStructure.RegisterOutputChanged(OnOutputChangedStructure);
            OnOutputChangedStructure(outputStructure);
        }

        public override Item[] GetRequiredItems(OutputStructure str, Item[] items) {
            items ??= str.Output;
            List<Item> all = new List<Item>();
            for (int i = items.Length - 1; i >= 0; i--) {
                int space = City.Inventory.GetRemainingSpaceForItem(items[i]);
                //WE need to know what every other marketstructure is getting atm 
                //so we do not get to much of this so look at every worker -> check if they have that item as getting -> else 0
                space -= City.MarketStructures.Where(x=> x.workers != null && x.workers.Count > 0)
                            .Sum(y => y.workers.Sum(z => Array.Find(z.ToGetItems, j => items[i].ID == j.ID)?.count ?? 0));
                if (space <= 0) continue;
                Item item = items[i].Clone();
                item.count = space;
                all.Add(item);
            }
            return all.ToArray();
        }

        public override bool InCityCheck(IEnumerable<Tile> tiles, int playerNumber) {
            return base.InCityCheck(tiles, playerNumber) 
                   || GetInRangeTiles(tiles.First()).Count(x => x?.City?.PlayerNumber == playerNumber) >= Data.structureRange / 5;
        }

        public override Item[] GetOutput(Item[] getItems, int[] maxAmounts) {
            Item[] temp = new Item[getItems.Length];
            for (int i = 0; i < getItems.Length; i++) {
                temp[i] = City.Inventory.GetItemWithMaxAmount(getItems[i], maxAmounts[i]);
            }
            return temp;
        }
    }
}