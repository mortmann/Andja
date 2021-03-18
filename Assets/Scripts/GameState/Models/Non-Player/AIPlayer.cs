using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AIPlayer {
    public int PlayerNummer => player.Number;
    public Player player;
    public List<Fertility> neededFertilities;
    public List<string> neededRessources;
    public List<Need> newNeeds;
    public List<Item> missingItems;
    public List<IslandScore> islandScores;
    public List<PlayerCombatValue> combatValues;
    PlayerCombatValue CombatValue;

    public AIPlayer(Player player) {
        this.player = player;
        neededFertilities = new List<Fertility>();
        neededRessources = new List<string>();
        newNeeds = new List<Need>();
        missingItems = new List<Item>();
        CombatValue = new PlayerCombatValue(player, null);
    }
    public void DecideIsland(bool onStart = false) {
        //TODO:set here desires
        CalculateIslandScores();
        islandScores = islandScores.OrderByDescending(x => x.EndScore).ToList();
        WarehouseStructure warehouse = PrototypController.Instance.FirstLevelWarehouse;
        //TODO: optimize
        List<TileValue> values = new List<TileValue>(AIController.IslandToMapSpaceValuedTiles[islandScores[0].Island]);
        values.RemoveAll(x => x.Type != TileType.Shore);
        List<TileValue> selected = new List<TileValue>(from TileValue in values
                                                       where TileValue.MaxValue >= warehouse.Height
                                                       select new TileValue(TileValue));
        foreach (TileValue t in selected) {
            for(int i=0;i<4;i++) {
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
    }
    public void CalculateIslandScores() {
        List<Island> islands = World.Current.Islands;
        islandScores = new List<IslandScore>();
        int maxSize = 0;
        int averageSize = 0;
        Dictionary<string, int> ressourceIDtoAverageAmount = new Dictionary<string, int>();
        Dictionary<string, int> ressourceIDtoExisting = new Dictionary<string, int>();
        Dictionary<Fertility, int> fertilitytoExisting = new Dictionary<Fertility, int>();
        string debugCalcValues = "IslandValues\n";
        foreach (Island island in islands) {
            if (island.Tiles.Count > maxSize)
                maxSize = island.Tiles.Count;
            averageSize += island.Tiles.FindAll(x => x.CheckTile()).Count;
            if (island.Resources != null || island.Resources.Count != 0) {
                foreach (string resid in island.Resources.Keys) {
                    if (ressourceIDtoAverageAmount.ContainsKey(resid)) {
                        ressourceIDtoAverageAmount[resid] += island.Resources[resid];
                        ressourceIDtoExisting[resid]++;
                    }
                    else {
                        ressourceIDtoAverageAmount[resid] = island.Resources[resid];
                        ressourceIDtoExisting[resid]=1;
                    }
                }
                foreach (Fertility fer in island.Fertilities) {
                    if (fertilitytoExisting.ContainsKey(fer)) {
                        fertilitytoExisting[fer]++;
                    }
                    else {
                        fertilitytoExisting[fer]=1;
                    }
                }
            }
        }
        foreach (string resid in ressourceIDtoAverageAmount.Keys.ToArray()) {
            ressourceIDtoAverageAmount[resid] /= islands.Count;
            ressourceIDtoExisting[resid] = 1 - (ressourceIDtoExisting[resid] / islands.Count);
        }
        averageSize /= islands.Count;
        foreach (Island island in islands) {
            IslandScore score = new IslandScore {
                Island = island,
                ShapeScore = 1,
                SizeSimilarIslandScore = 1,
            };
            //Calculate Ressource Score
            if (island.Resources == null || island.Resources.Count == 0)
                score.RessourceScore = 0; // There is none
            else {
                //for each give it a score for each existing on the island
                foreach(string resid in island.Resources.Keys) { 
                    // if it is needed RIGHT NOW score it after how much it exist on this in the average over ALL islands
                    if(neededRessources.Contains(resid)) {
                        score.RessourceScore += island.Resources[resid] / ressourceIDtoAverageAmount[resid];
                    }
                    // its always nice to have -- add how rare it is in the world
                    score.RessourceScore += ressourceIDtoExisting[resid];
                }
            }

            score.SizeScore = (float)island.Tiles.FindAll(x => x.CheckTile()).Count;
            score.SizeScore /= averageSize;

            //Calculate Fertility Score
            foreach (Fertility fertility in island.Fertilities) {
                // add how rare it is in the world -- multiple this??
                if (neededFertilities.Contains(fertility)) {
                    score.FertilityScore += fertilitytoExisting[fertility];
                }
                // its always nice to have -- add how rare it is in the world
                score.FertilityScore += fertilitytoExisting[fertility];
            }
            List<Island> Islands = new List<Island>(player.GetIslandList());
            //Distance Score is either how far it is from other islands OR how far from center
            if (Islands.Count > 0) {
                float distance = 0;
                foreach(Island isl in Islands) {
                    distance += Vector2.Distance(island.Center , isl.Center);
                }
                distance /= Islands.Count;
                score.DistanceScore = distance;
            } else {
                score.DistanceScore = 1 / (Vector2.Distance(island.Center, World.Current.Center) / ((World.Current.Width + World.Current.Height)/2));
            }
            //Competition Score is the percentage of unclaimed Tiles multiplied through how many diffrent players
            if(island.Cities.Count>0) {
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
            debugCalcValues += ("| " + score.Island.StartTile.Vector2+ " " + score.EndScore);
        }
        Debug.Log(debugCalcValues);
    }
    public void CalculatePlayersCombatValue() {
        combatValues = new List<PlayerCombatValue>();
        List<Player> players = PlayerController.Instance.GetPlayers();
        foreach(Player p in players) {
            PlayerCombatValue value = new PlayerCombatValue(p,CombatValue);
            //foreach(Unit u in p.GetLandUnits()) {
            //    value.UnitValue += u.Damage/2 + u.MaxHealth/2; 
            //}
            //foreach (Ship s in p.GetShipUnits()) {
            //    value.ShipValue += s.Damage / 2 + s.MaxHealth / 2;
            //}
            //List<MilitaryStructure> militaryStructures = new List<MilitaryStructure>(p.AllStructures.OfType<MilitaryStructure>());
            //foreach(MilitaryStructure structure in militaryStructures) {
            //    value.MilitaryStructureValue++;
            //}
            ////imitates a guess how much the player makes 
            ////also guess the money in the bank?
            //value.MoneyValue = Random.Range(p.TreasuryChange - p.TreasuryChange / 4, p.TreasuryChange + p.TreasuryChange / 4);
            //Debug.Log("Calculated PlayerCombat Score " + value.EndScore);
            combatValues.Add(value);
        }
    }
    
}
