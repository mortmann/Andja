using Andja.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public class AIPlayer {
        public int PlayerNummer => player.Number;
        public Player player;
        public List<Fertility> neededFertilities;
        public List<string> neededResources;
        public List<Need> newNeeds;
        public List<Item> missingItems;
        public List<IslandScore> islandScores;
        public List<PlayerCombatValue> combatValues;
        private PlayerCombatValue CombatValue;
        public Dictionary<string, int> structureToCount;

        /// <summary>
        /// 0 = either no Production
        /// Positive = has more Production than needed
        /// Negative = has less Production than needed
        /// </summary>
        public Dictionary<string, float> itemToProducePerMinuteChange;

        public AIPlayer(Player player) {
            this.player = player;
            neededFertilities = new List<Fertility>();
            neededResources = new List<string>();
            newNeeds = new List<Need>();
            missingItems = new List<Item>();
            CombatValue = new PlayerCombatValue(player, null);
            structureToCount = new Dictionary<string, int>();
            itemToProducePerMinuteChange = new Dictionary<string, float>();
            foreach (string item in PrototypController.Instance.AllItems.Keys) {
                itemToProducePerMinuteChange[item] = 0;
            }
            foreach (Structure s in player.AllStructures) {
                OnPlaceStructure(s);
            }
            player.RegisterCityCreated(OnCityCreated);
            player.RegisterCityDestroy(OnCityDestroy);
            player.RegisterNewStructure(OnNewStructure);
            player.RegisterLostStructure(OnLostStructure);
            
        }

        public AIPlayer(Player player, bool dummy) {
            this.player = player;
            CalculateNeeded();
        }

        private void CalculateNeeded() {
            var data = AIController.PerPopulationLevelDatas[player.CurrentPopulationLevel];
            neededFertilities = new List<Fertility>(data.possibleFertilities);
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
                        foreach (Item p in ps.ProductionData.intake) {
                            itemToProducePerMinuteChange[p.ID] -= p.count * (60f / ps.ProduceTime);
                        }
                    }
                }
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

        public void DecideIsland(bool onStart = false) {
            //TODO:set here desires
            CalculateIslandScores();
            islandScores = islandScores.OrderByDescending(x => x.EndScore).ToList();
            WarehouseStructure warehouse = PrototypController.Instance.FirstLevelWarehouse;
            int index = 0;
            while(warehouse.BuildTile == null) {
                List<TileValue> values = new List<TileValue>(AIController.IslandToMapSpaceValuedTiles[islandScores[index].Island]);
                values.RemoveAll(x => x.Type != TileType.Shore);
                List<TileValue> selected = new List<TileValue>(from TileValue in values
                                                               where TileValue.MaxValue >= warehouse.Height
                                                               select new TileValue(TileValue));
                foreach (TileValue t in selected) {
                    for (int i = 0; i < 4; i++) {
                        //bool left = warehouse.Rotation == 90 || warehouse.Rotation == 180;
                        List<Tile> buildtiles = warehouse.GetBuildingTiles(t.tile, false, false);
                        if (buildtiles.Exists(x => x.Type == TileType.Ocean))
                            continue;
                        if (warehouse.CanBuildOnSpot(buildtiles)) {
                            AIController.PlaceStructure(this, warehouse, buildtiles, null, onStart);
                            return;
                        }
                        warehouse.RotateStructure();
                    }
                }
                index++;
            }
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
                if (island.Cities.Count > 0) {
                    float avaibleTiles = island.Tiles.Count;
                    foreach (City c in island.Cities) {
                        if (c.IsWilderness())
                            continue;
                        avaibleTiles -= c.Tiles.Count;
                    }
                    score.CompetitionScore = avaibleTiles / island.Tiles.Count;
                    score.CompetitionScore *= island.Cities.Count;
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

    internal abstract class AIPriority {
        public float Priority { get; protected set; }

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
                Priority = player.player.MaxPopulationLevel - item.Data.UnlockLevel;
                return;
            }
            if (player.player.MaxPopulationLevel == item.Data.UnlockLevel) {
                //Coming up SOON but CANT build it so it will range between -1 and 0
                if (player.player.MaxPopulationCounts[item.Data.UnlockLevel] < item.Data.UnlockPopulationCount) {
                    Priority = (player.player.MaxPopulationCounts[item.Data.UnlockLevel] - item.Data.UnlockPopulationCount)
                                        / AIController.PerPopulationLevelDatas[item.Data.UnlockLevel].atleastRequiredPeople;
                    return;
                }
            }
            if (item.Type == ItemType.Build) {
                Priority = PrototypController.Instance.recommandedBuildSupplyChains[item.ID][player.player.CurrentPopulationLevel];
                Priority -= player.itemToProducePerMinuteChange[item.ID];
                return;
            }
            //int currentPopulation = player.player.GetCurrentPopulation(item.)
        }
    }
}