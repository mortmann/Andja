using Andja.Controller;
using Andja.Model.Data;
using Andja.Pathfinding;
using Andja.Utility;
using Priority_Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class AIPlayer {
        public bool isActive;
        public int PlayerNummer => player.Number;
        public Player player;
        public List<Fertility> neededFertilities;
        public List<string> neededResources;
        public List<Need> newNeeds;
        public List<Item> missingItems;
        public List<IslandScore> islandScores;
        public List<PlayerCombatValue> combatValues;
        private PlayerCombatValue CombatValue;
        public ConcurrentDictionary<string, int> structureToCount;
        
        /// <summary>
        /// 0 = either no Production
        /// Positive = has more Production than needed
        /// Negative = has less Production than needed
        /// </summary>
        public Dictionary<string, float> itemToProducePerMinuteChange;
        SimplePriorityQueue<ItemPriority> buildItemPriority = new SimplePriorityQueue<ItemPriority>();
        SimplePriorityQueue<ItemPriority> itemPriority = new SimplePriorityQueue<ItemPriority>();
        Queue<PlaceStructure> toBuildStructures = new Queue<PlaceStructure>();
        List<Operation> currentOperationPending = new List<Operation>();
        BuildPathAgent BuildPathAgent;

        Unlocks nextUnlocks;

        public AIPlayer(Player player) {
            this.player = player;
            neededFertilities = new List<Fertility>();
            neededResources = new List<string>();
            newNeeds = new List<Need>();
            missingItems = new List<Item>();
            CombatValue = new PlayerCombatValue(player, null);
            structureToCount = new ConcurrentDictionary<string, int>();
            itemToProducePerMinuteChange = new Dictionary<string, float>();
            foreach (string item in PrototypController.Instance.AllItems.Keys) {
                itemToProducePerMinuteChange[item] = 0;
            }
            foreach (Structure s in player.AllStructures) {
                OnPlaceStructure(s);
            }
            foreach (Item item in PrototypController.BuildItems) {
                ItemPriority ip = new ItemPriority(item);
                ip.CalculatePriority(this);
                buildItemPriority.Enqueue(ip, ip.Priority);
            }
            foreach (Item item in PrototypController.Instance.AllItems.Values.Except(PrototypController.BuildItems)) {
                ItemPriority ip = new ItemPriority(item);
                ip.CalculatePriority(this);
                itemPriority.Enqueue(ip, ip.Priority);
            }
            player.RegisterCityCreated(OnCityCreated);
            player.RegisterCityDestroy(OnCityDestroy);
            player.RegisterNewStructure(OnNewStructure);
            player.RegisterLostStructure(OnLostStructure);
            isActive = player.IsHuman == false;
            BuildPathAgent = new BuildPathAgent(player.Number);
            nextUnlocks = PrototypController.Instance.GetNextUnlocks(player.MaxPopulationLevel, player.MaxPopulationCounts[player.MaxPopulationLevel]);
        }

        public AIPlayer(Player player, bool dummy) {
            this.player = player;
            CalculateNeeded();
            if (dummy)
                return;
            isActive = player.IsHuman == false;
        }

        private void CalculateNeeded() {
            var data = AIController.PerPopulationLevelDatas[player.CurrentPopulationLevel];
            neededFertilities = new List<Fertility>(data.requiredFertilities);
            neededResources = new List<string>(data.newResources);
        }

        private void OnNewStructure(Structure structure) {
            if (structureToCount.ContainsKey(structure.ID))
                structureToCount[structure.ID]++;
            else
                structureToCount[structure.ID] = 1;

            if (structure is OutputStructure) {
                OutputStructure os = structure as OutputStructure;
                if (os.Output == null) {
                    return;
                }
                foreach (Item p in os.Output) {
                    itemToProducePerMinuteChange[p.ID] += os.OutputData.ProducePerMinute;
                }
                if (structure is ProductionStructure) {
                    ProductionStructure ps = structure as ProductionStructure;
                    if (ps.InputTyp == InputTyp.OR) {
                        foreach (Item p in ps.Intake) {
                            Item i = Array.Find(ps.ProductionData.intake, x => x.ID == p.ID);
                            itemToProducePerMinuteChange[p.ID] -= i.count * (60f / ps.ProduceTime);
                        }
                    }
                    else {
                        if(ps.ProductionData.intake != null) {
                            foreach (Item p in ps.ProductionData.intake) {
                                itemToProducePerMinuteChange[p.ID] -= p.count * (60f / ps.ProduceTime);
                            }
                        }
                    }
                }
            }
        }
        internal void Loop() {
            while(AIController.ShutdownAI == false && isActive) {
                if(AIController.ActiveAI == false || WorldController.Instance.IsPaused) {
                    continue;
                }
                if (AIController._cityToCurrentSpaceValueTiles == null||AIController._cityToCurrentSpaceValueTiles.Count == 0)
                    continue;
                foreach (Operation item in currentOperationPending.ToArray()) {
                    switch (item.Status) {
                        case OperationStatus.Pending:
                            break;
                        case OperationStatus.Success:
                            OperationSuccess(item);
                            break;
                        case OperationStatus.Failure:
                            OperationFailure(item);
                            break;
                    }
                }
                GameStartFunction();

                UnlocksFunction();
                BuildBuildings();
            }
        }

        private void UnlocksFunction() {
            if (nextUnlocks == null)
                return;
            if(nextUnlocks.populationLevel > player.MaxPopulationCount) 
                return;
            if(nextUnlocks.peopleCount > player.MaxPopulationCount)
                return;
            nextUnlocks = PrototypController.Instance.GetNextUnlocks(player.MaxPopulationCount, player.MaxPopulationCount);
        }

        private void BuildBuildings() {
            if (player.Cities.Count == 0) {
                return;
            }
            if(toBuildStructures.Count > 0) {
                PlaceStructure ps = toBuildStructures.Dequeue();
                //TODO:this could technically not be in the city so we would need to get it other way
                Structure s = PrototypController.Instance.GetStructure(ps.ID);
                if(ps.buildTile.City.HasEnoughOfItems(s.BuildingItems) == false && player.HasEnoughMoney(s.BuildCost)) {
                    toBuildStructures.Enqueue(ps);
                    return;
                }
                currentOperationPending.Add(AIController.Instance.AddOperation(new BuildStructureOperation(this, ps)));
                return;
            }
            //For now only plan a single round of buildings
            if (currentOperationPending.Count > 0 && currentOperationPending.Exists(x=>x is BuildStructureOperation)) return;
            foreach (var item in buildItemPriority) {
                item.CalculatePriority(this);
                buildItemPriority.UpdatePriority(item, item.Priority);
            }
            if (buildItemPriority.First.Priority < -0.2) {
                BuildItemStructure(buildItemPriority.First.item);
                return;
            }
            foreach (var item in itemPriority) {
                item.CalculatePriority(this);
                itemPriority.UpdatePriority(item, item.Priority);
            }
            if (itemPriority.First.Priority < -0) {
                BuildItemStructure(itemPriority.First.item);
                return;
            }
            foreach (string need in player.UnlockedStructureNeeds[player.CurrentPopulationLevel]) {
                //TODO: Is decide which of the structures is the best
                string structureID = PrototypController.Instance.NeedPrototypeDatas[need].structures[0].ID;
                if(structureToCount.ContainsKey(structureID)) {
                    //we have on already so we need to check if it is enough...
                    //later doing this
                } else {
                    BuildNeedStructure(structureID);
                    return;
                }
            }
            //First check if need structure is required
            //Then we need to build more homes

            int alreadyBuild = 0;
            if(structureToCount.ContainsKey(PrototypController.Instance.BuildableHomeStructure.ID))
                alreadyBuild = structureToCount[PrototypController.Instance.BuildableHomeStructure.ID];
            if (nextUnlocks.requiredFullHomes - alreadyBuild > 0) {
                BuildHomeStructure();
            }
        }

        private void BuildHomeStructure() {
            //TODO: rewrite this all 
            //TODO: choose the better one somehow...
            //TODO: road placement for this
            City city = player.Cities.MaxBy(x => x.PopulationCount);
            var cityValues = AIController._cityToCurrentSpaceValueTiles[city];
            var tempStructures = city.Structures.ToList();
            var poplevel = PrototypController.Instance.GetPopulationLevelPrototypDataForLevel(0);
            var structureNeeds = AIController.PerPopulationLevelDatas[0].structureNeeds;
            var ns = tempStructures.FindAll(x => x is NeedStructure && structureNeeds.Any(y=>y.Data.structures.Any(z=>z.ID==x.ID)));
            if (ns.Count == 0)
                return;
            var tiles = ns.SelectMany(x => x.RangeTiles)
                .Where(x=> PrototypController.Instance.BuildableHomeStructure.CanBuildOnSpot(
                        PrototypController.Instance.BuildableHomeStructure.GetBuildingTiles(x)
                    ));
            tiles = tiles.Where(x => x.City.PlayerNumber == player.Number);
            var temp = tiles.GroupBy(i => i);
            var t = temp.OrderByDescending(grp => grp.Count()).Select(x=>x.Key);
            var tempt = t.First();
            if (ns.Count >= 1) {
                var distanceOrdered = tiles.OrderBy(x => Vector2.Distance(x.Vector2, ns[0].Center));
                foreach(Tile nt in distanceOrdered) {
                    if (PrototypController.Instance.BuildableHomeStructure.GetBuildingTiles(nt)
                        .Exists(x => x.City.PlayerNumber != player.Number) == false) {
                        tempt = nt;
                        break;
                    }
                }
            }
            toBuildStructures.Enqueue(new PlaceStructure {
                buildTile = tempt,
                ID = PrototypController.Instance.BuildableHomeStructure.ID,
                rotation = 0,
            });
            
        }

        private void BuildNeedStructure(string structureID) {
            var place = FindStructurePlace(structureID);
            if (place != null)
                toBuildStructures.Enqueue(place.Value);
        }

        private void BuildItemStructure(Item item) {
            var produces = PrototypController.Instance.ItemIDToProduce[item.ID].Where(x => player.HasStructureUnlocked(x.ProducerStructure.ID));
            var sorted = produces.SelectMany(x => x.SupplyChains.Where(y => y.IsValid && y.IsUnlocked(player)));
            if (sorted.Count() == 0) {
                Debug.LogWarning("AI cannot find a valid SupplyChain for " + item.ID + "! Wanted behaviour?");
                return;
            }
            sorted.OrderBy(z => z.cost);
            SupplyChain currentlySelected = sorted.First();
            if (currentlySelected.cost.requiredFertilites != null && currentlySelected.cost.requiredFertilites.Count > 0) {
                IEnumerable<Fertility> missing = null;
                foreach (SupplyChain p in sorted) {
                    var missingFertilities = p.cost.requiredFertilites.Except(player.GetIslandList().SelectMany(x => x.Fertilities).ToList());
                    if(missingFertilities.Count() == 0) {
                        currentlySelected = p;
                        break;
                    }
                    //For now it is preferring the SupplyChain that is not requiring any new fertility.
                    //But it also should take in a count how difficult each new fertility is to get or how much space 
                    //is left on each island with the corresponding fertility -- 
                    if(missing == null || missing.Count() > missingFertilities.Count()) {
                        missing = missingFertilities;
                        currentlySelected = p;
                    }
                }
            }
            //We need to queue these structures -- maybe we can remove already exisiting structures
            //because not all of the produced is needed for another supplychain?
            var strucuturesToCount = currentlySelected.StructureToBuildForOneRatio();
            if(item.Type == ItemType.Intermediate) {
                
            }
            foreach (var structureToCount in strucuturesToCount) {
                for (int i = 0; i < structureToCount.Value; i++) {
                    var place = FindStructurePlace(structureToCount.Key);
                    if(place != null)
                        toBuildStructures.Enqueue(place.Value);
                }
            }
        }

        internal void OperationSuccess(Operation op) {
            currentOperationPending.Remove(op);
            if (op is BuildStructureOperation bs) {
                if(bs.Structure is OutputStructure os) {
                    if (os is MarketStructure) {
                        if(os is WarehouseStructure ws && bs.BuildUnit != null) {
                            currentOperationPending.Add(AIController.Instance.AddOperation(
                                new UnitCityMoveItemOperation(this, bs.BuildUnit, ws.City, bs.BuildUnit.inventory.Items.Values.ToArray(), false)
                                ));
                            currentOperationPending.Add(AIController.Instance.AddOperation(
                                new TradeItemOperation(this, ws.City, new List<TradeItem> { new TradeItem("tools", 25, 50, Trade.Buy)}, true)
                                ));
                        }
                        return;
                    }
                    if (os.ForMarketplace == false) {
                        //TODO: only build roads IF it is needed when the os is not in range of intake 
                    }
                    City c = bs.BuildTile.Island.FindCityByPlayer(player.Number);
                    var marketStructures = c.Structures.Where(x => x is MarketStructure && x.RangeTiles.Intersect(os.Tiles).Any());
                    var routes = marketStructures.SelectMany(x => x.GetRoutes()).Distinct();
                    List<Tile> tiles = marketStructures.SelectMany(y=>y.Tiles.Where(x=>x.IsGenericBuildType())).ToList();
                    tiles.AddRange(routes.SelectMany(x => x.Tiles));
                    var roadTargets = os.Tiles.Where(x => x.IsGenericBuildType()).Select(x => x.Vector2).ToList();
                    Tile start = tiles.MinBy(x => Mathf.Abs(x.X - os.Center.x) + Mathf.Abs(x.Y - os.Center.y));
                    PathJob job = new PathJob(BuildPathAgent, bs.BuildTile.Island.Grid, 
                            start.Vector2,
                            roadTargets.MinBy(x => Mathf.Abs(x.x - start.Vector2.x) + Mathf.Abs(x.y - start.Vector2.y)),
                            roadTargets, 
                            tiles.Select(x=>x.Vector2).ToList()
                    );
                    PathfindingThreadHandler.EnqueueJob(job, () => { });
                    while(job.Status == JobStatus.InQueue || job.Status == JobStatus.Calculating) { }
                    if(job.Status == JobStatus.Done) {
                        currentOperationPending.Add(
                        AIController.Instance.AddOperation(
                            new BuildSingleStructureOperation(this, job.Path.ToList(), PrototypController.Instance.GetRoadForLevel(0))
                            )
                        );
                    } else {
                        Debug.LogWarning("Path not found for roads to structure: " + os);
                    }
                    if(os is FarmStructure fs) {
                        if(fs.Growable != null) {
                            AIController.Instance.AddOperation(
                                new BuildSingleStructureOperation(this, fs.RangeTiles.Select(x=>x.Vector2).ToList(), fs.Growable)
                            );
                        }
                    }
                }
                if(bs.Structure is NeedStructure ns) {
                    if (ns.NeedStructureData.SatisfiesNeeds.Any(x => x.HasToReachPerRoad)) {
                        AIController.Instance.AddOperation(
                                new BuildSingleStructureOperation(this, ns.NeighbourTiles.Select(x => x.Vector2).ToList(), PrototypController.Instance.GetRoadForLevel(0))
                            );
                    }
                }
            }
        }

        internal void OperationFailure(Operation op) {
            Debug.LogWarning("Operation Failure: " + op.GetType());
            if (op is BuildStructureOperation bso)
                Debug.LogWarning(bso.Structure + " " + bso.BuildTile);
            currentOperationPending.Remove(op);
        }

        private PlaceStructure? FindStructurePlace(string key) {
            Structure s = PrototypController.Instance.GetStructureCopy(key);
            IEnumerable<Island> isls = player.GetIslandList();
            if (s is FarmStructure fs && fs.Growable.Fertility != null) {
                isls = isls.Where(x => x.Fertilities.Contains(fs.Growable.Fertility));
            } else 
            if(s is MineStructure ms) {
                isls = isls.Where(x => x.Resources.ContainsKey(ms.Resource));
            }
            isls.OrderBy(x => islandScores.Find(y => y.Island == x).SizeScore);
            foreach (var island in isls) {
                var cityValues = AIController._cityToCurrentSpaceValueTiles[island.FindCityByPlayer(player.Number)];
                List<TileValue> tiles;
                lock (cityValues) {
                    tiles = cityValues.Values.ToList();
                }
                if(s.BuildTileTypes != null) {
                    HashSet<TileType> typesRequired = new HashSet<TileType>();
                    for (int x = 0; x < s.BuildTileTypes.GetLength(0); x++) {
                        for (int y = 0; y < s.BuildTileTypes.GetLength(1); y++) {
                            if (s.BuildTileTypes[x, y].HasValue)
                                typesRequired.Add(s.BuildTileTypes[x, y].Value);
                        }
                    }
                    tiles.RemoveAll(x => typesRequired.Contains(x.tile.Type) == false);
                    var minTileValues = s.Data.BuildTileTypesToMinLength;
                    tiles.RemoveAll(x => minTileValues[x.tile.Type] > x.MaxValue);
                    if (tiles.Count == 0) {
                        //AI has to expand or choose another island
                        return null;
                    }
                    foreach(var t in tiles.OrderBy(x => x.MaxValue)) {
                        for (int i = 0; i < 4; i++) {
                            List<Tile> buildtiles = s.GetBuildingTiles(t.tile, false, false);
                            if (buildtiles.Exists(x => x.City?.PlayerNumber != player.Number))
                                continue;
                            if (s.CanBuildOnSpot(buildtiles)) {
                                return new PlaceStructure {
                                    ID = s.ID,
                                    buildTile = t.tile,
                                    rotation = s.rotation
                                };
                            }
                            s.RotateStructure();
                        }
                    }
                } else {
                    tiles = tiles.Where(x => x.tile.CheckTile() && x.MinVector.x >= s.TileWidth && x.MinVector.y >= s.TileHeight).ToList();
                }
                if (s.StructureRange > 0) {
                    tiles.OrderByDescending(x => x.MinVector.x == s.StructureRange && x.MinVector.y == s.StructureRange);
                    var tooSmall = tiles.Where(x => x.MinVector.x < s.StructureRange || x.MinVector.y < s.StructureRange);
                    var bigger = tiles.Except(tooSmall);
                    Tile tile = null;
                    if(bigger.Count() > 0) {
                        bigger.OrderBy(x => x.MinVector.x - s.StructureRange).ThenBy(x => x.MinVector.y - s.StructureRange);
                        tile = bigger.First().tile;
                    }
                    if(tile == null) {
                        tooSmall.OrderBy(x => x.MinVector.x - s.StructureRange).ThenBy(x => x.MinVector.y - s.StructureRange);
                        tile = tooSmall.Last().tile;
                    }
                    return new PlaceStructure {
                        ID = s.ID,
                        buildTile = tile,
                        rotation = 0
                    };
                }
            }
            Debug.LogWarning("AI did not FindStructurePlace " + key + "!");
            return null;
        }
        bool startFunction = false;
        private void GameStartFunction() {
            if (player.Cities.Count == 0) {
                Ship ship = player.Ships
                                .First(s => s.inventory.HasEnoughOfItems(PrototypController.Instance.FirstLevelWarehouse.BuildingItems));
                if (ship == null) {
                    Debug.LogError("AI does not have enough resources to start a city! -- Stopping AI Player " + player.Name);
                    isActive = false;
                    return;
                }
                if (startFunction)
                    return;
                startFunction = true;
                CalculateNeeded();
                var t = DecideIsland(false, true);
                currentOperationPending.Add(AIController.Instance.AddOperation(new MoveUnitOperation(this, ship, t.Item1, true)));

                void ShipWarehouse(Unit u, bool atdest) {
                    if (atdest == false)
                        return;
                    var warehouse = PrototypController.Instance.FirstLevelWarehouse.Clone();
                    warehouse.ChangeRotation(t.Item2);
                    currentOperationPending.Add(
                        AIController.Instance.AddOperation(new BuildStructureOperation(this, t.Item1, warehouse, ship))
                        );
                    ship.UnregisterOnArrivedAtDestinationCallback(ShipWarehouse);
                }

                ship.RegisterOnArrivedAtDestinationCallback(ShipWarehouse);
            }
        }

        private void OnLostStructure(Structure structure) {
            structureToCount[structure.ID]--;
            if (structure is OutputStructure os) {
                if(os.Output != null)
                    foreach (Item p in os.Output) {
                        itemToProducePerMinuteChange[p.ID] -= os.OutputData.ProducePerMinute;
                    }
                if (os is ProductionStructure ps) {
                    if (ps.InputTyp == InputTyp.OR) {
                        Debug.LogWarning("AI CAN'T HANDLE OR INTAKE YET!");
                        return;
                    }
                    foreach (Item p in ps.Intake) {
                        itemToProducePerMinuteChange[p.ID] += p.count * (60f / ps.ProduceTime);
                    }
                }
            }
        }

        private void OnCityDestroy(City city) {
            //AI BE MAD
        }

        private void OnCityCreated(City city) {
            //AI BE SMART
        }

        private void OnPlaceStructure(Structure s) {
            OnNewStructure(s);
        }

        private void OnOwnerChange(Structure str, City oldCity, City newCity) {
        }

        private void OnStructureDestroy(Structure structure, IWarfare destroyer) {
        }

        public Tuple<Tile, int> DecideIsland(bool buildWarehouseDirectly = false, bool startIslands = false) {
            //TODO:set here desires
            CalculateIslandScores();
            islandScores = islandScores.OrderByDescending(x => x.EndScore).ToList();
            if(startIslands && neededFertilities.Count > 0) {
                islandScores.RemoveAll(x => x.Island.Fertilities.Any(y => neededFertilities.Contains(y)) == false);
                if (islandScores.Count == 0) {
                    Debug.Log("No island found that would be a possible start for ai (anyone).");
                    return null;
                }
                if(islandScores.Count < PlayerController.PlayerCount) {
                    startIslands = false;
                }
            }
            WarehouseStructure warehouse = PrototypController.Instance.FirstLevelWarehouse.Clone() as WarehouseStructure;
            int index = 0;
            while(warehouse.BuildTile == null) {
                List<TileValue> values = new List<TileValue>(AIController.IslandsTileToValue[islandScores[index].Island].Values);
                values.RemoveAll(x => x.Type != TileType.Shore);
                List<TileValue> selected = new List<TileValue>(from TileValue in values
                                                               where TileValue.MaxValue >= warehouse.Height 
                                                                     && TileValue.tile.City.IsWilderness()
                                                               select new TileValue(TileValue));
                lock (islandScores[index].Island) {
                    if(startIslands && islandScores[index].Island.startClaimed) {

                    } else {
                        foreach (TileValue t in selected) {
                            for (int i = 0; i < 4; i++) {
                                List<Tile> buildtiles = warehouse.GetBuildingTiles(t.tile, false, false);
                                if (buildtiles.Exists(x => x.Type == TileType.Ocean || x.City.IsWilderness() == false))
                                    continue;
                                if (warehouse.CanBuildOnSpot(buildtiles)) {
                                    if (buildWarehouseDirectly) {
                                        AIController.BuildStructure(this, warehouse, buildtiles, null, buildWarehouseDirectly);
                                    }
                                    else {
                                        islandScores[index].Island.startClaimed = startIslands;
                                    }
                                    return new Tuple<Tile, int>(t.tile, warehouse.rotation);
                                }
                                warehouse.RotateStructure();
                            }
                        }
                    }
                }
                index++;
            }
            Debug.LogError("AI Player " + player.Name + " did not find any possible build location for warehouse. Please report seed/save.");
            return null;
        }

        public void CalculateIslandScores() {
            List<Island> islands = World.Current.Islands;
            islandScores = new List<IslandScore>();
            int maxSize = 0;
            int averageSize = 0;
            Dictionary<string, int> resourceIDtoAverageAmount = new Dictionary<string, int>();
            Dictionary<string, int> resourceIDtoExisting = new Dictionary<string, int>();
            Dictionary<Fertility, int> fertilitytoExisting = new Dictionary<Fertility, int>();
            string debugCalcValues = "IslandValues\n";
            foreach (Island island in islands) {
                if (island.Tiles.Count > maxSize)
                    maxSize = island.Tiles.Count;
                averageSize += island.Tiles.FindAll(x => x.CheckTile()).Count;
                if (island.Resources != null || island.Resources.Count != 0) {
                    foreach (string resid in island.Resources.Keys) {
                        if (resourceIDtoAverageAmount.ContainsKey(resid)) {
                            resourceIDtoAverageAmount[resid] += island.Resources[resid];
                            resourceIDtoExisting[resid]++;
                        }
                        else {
                            resourceIDtoAverageAmount[resid] = island.Resources[resid];
                            resourceIDtoExisting[resid] = 1;
                        }
                    }
                    foreach (Fertility fer in island.Fertilities) {
                        if (fertilitytoExisting.ContainsKey(fer)) {
                            fertilitytoExisting[fer]++;
                        }
                        else {
                            fertilitytoExisting[fer] = 1;
                        }
                    }
                }
            }
            foreach (string resid in resourceIDtoAverageAmount.Keys.ToArray()) {
                resourceIDtoAverageAmount[resid] /= islands.Count;
                resourceIDtoExisting[resid] = 1 - (resourceIDtoExisting[resid] / islands.Count);
            }
            averageSize /= islands.Count;
            foreach (Island island in islands) {
                IslandScore score = new IslandScore {
                    Island = island,
                    ShapeScore = 1,
                    SizeSimilarIslandScore = 1,
                };
                //Calculate Resource Score
                if (island.Resources == null || island.Resources.Count == 0)
                    score.ResourceScore = 0; // There is none
                else {
                    //for each give it a score for each existing on the island
                    foreach (string resid in island.Resources.Keys) {
                        // if it is needed RIGHT NOW score it after how much it exist on this in the average over ALL islands
                        if (neededResources.Contains(resid) && resourceIDtoAverageAmount[resid] > 0) {
                            score.ResourceScore += island.Resources[resid] / resourceIDtoAverageAmount[resid];
                        }
                        // its always nice to have -- add how rare it is in the world
                        score.ResourceScore += resourceIDtoExisting[resid];
                    }
                }

                score.SizeScore = (float)island.Tiles.FindAll(x => x.CheckTile()).Count;
                score.SizeScore /= averageSize;

                List<Fertility> ordered = PrototypController.Instance.orderUnlockFertilities;
                int indexLastUnlocked = ordered.FindLastIndex(x => x.IsUnlocked(player));
                if (indexLastUnlocked < 0)
                    indexLastUnlocked = 0;
                //Calculate Fertility Score
                foreach (Fertility fertility in island.Fertilities) {
                    // add how rare it is in the world -- multiple this??
                    if (neededFertilities.Contains(fertility)) {
                        score.FertilityScore += fertilitytoExisting[fertility];
                    }
                    if (fertility.IsUnlocked(player) == false)
                        score.FertilityScore += 1 - (ordered.IndexOf(fertility) - indexLastUnlocked) / ordered.Count;
                    // its always nice to have -- add how rare it is in the world
                    score.FertilityScore += fertilitytoExisting[fertility];
                }
                List<Island> Islands = new List<Island>(player.GetIslandList());
                //Distance Score is either how far it is from other islands OR how far from center
                if (Islands.Count > 0) {
                    float distance = 0;
                    foreach (Island isl in Islands) {
                        distance += Vector2.Distance(island.Center, isl.Center);
                    }
                    distance /= Islands.Count;
                    score.DistanceScore = distance;
                }
                else {
                    score.DistanceScore = (Vector2.Distance(island.Center, World.Current.Center) / World.Current.Center.magnitude);
                }
                Dictionary<Tile, TileValue> values = AIController.IslandsTileToValue[island];
                int averageTileScore = 0;
                foreach (Tile t in values.Keys) {
                    if (t.CheckTile()) {
                        averageTileScore += values[t].MinValue;
                    }
                }
                averageTileScore /= island.Tiles.Count;
                score.ShapeScore = averageTileScore;
                //Competition Score is the percentage of unclaimed Tiles multiplied through how many diffrent players
                if (island.Cities.Count > 1) {
                    float avaibleTiles = island.Tiles.Count;
                    foreach (City c in island.Cities) {
                        if (c.IsWilderness())
                            continue;
                        avaibleTiles -= c.Tiles.Count;
                    }
                    score.CompetitionScore = 1 - (avaibleTiles / island.Tiles.Count);
                    score.CompetitionScore *= island.Cities.Count - 1;
                }
                islandScores.Add(score);
                debugCalcValues += (score.Island.StartTile.Vector2 + " " + score.EndScore + "=" + score + "\n");
            }
            Debug.Log(debugCalcValues);
        }

        public void CalculatePlayersCombatValue() {
            combatValues = new List<PlayerCombatValue>();
            List<Player> players = PlayerController.Instance.GetPlayers();
            foreach (Player p in players) {
                PlayerCombatValue value = new PlayerCombatValue(p, CombatValue);
                combatValues.Add(value);
            }
        }
    }
    public struct PlaceStructure {
        public Tile buildTile;
        public int rotation;
        public string ID;
    }

    internal abstract class AIPriority {
        /// <summary>
        /// this has to invert the priority because the queue system only allows lower priority as first
        /// so -1 comes bevor 1
        /// </summary>
        public float Priority => -priority;

        protected float priority { get; set; }
        public abstract void CalculatePriority(AIPlayer player);
    }

    internal class ItemPriority : AIPriority {
        public Item item;

        public ItemPriority(Item item) {
            this.item = item;
        }

        public override void CalculatePriority(AIPlayer player) {
            if (player.player.MaxPopulationLevel < item.Data.UnlockLevel) {
                //Coming up when level up. Not really important so it will max -1
                priority = player.player.MaxPopulationLevel - item.Data.UnlockLevel;
                return;
            }
            if (player.player.MaxPopulationLevel == item.Data.UnlockLevel) {
                //Coming up SOON but CANT build it so it will range between -1 and 0
                if (player.player.MaxPopulationCounts[item.Data.UnlockLevel] < item.Data.UnlockPopulationCount) {
                    priority = (player.player.MaxPopulationCounts[item.Data.UnlockLevel] - item.Data.UnlockPopulationCount)
                                        / AIController.PerPopulationLevelDatas[item.Data.UnlockLevel].atleastRequiredPeople;
                    return;
                }
            }
            if (item.Type == ItemType.Build) {
                if (PrototypController.Instance.recommandedBuildSupplyChains.ContainsKey(item.ID) == false) {
                    priority = int.MinValue;
                    return;
                }
                priority = PrototypController.Instance.recommandedBuildSupplyChains[item.ID][player.player.CurrentPopulationLevel];
                priority -= player.itemToProducePerMinuteChange[item.ID];
                return;
            }
            if (item.Type == ItemType.Luxury) {
                priority = player.player.Cities.Sum(x => x.GetPopulationItemUsage(item));
                priority -= player.itemToProducePerMinuteChange[item.ID];
                return;
            }
            //int currentPopulation = player.player.GetCurrentPopulation(item.)
        }
    }
}