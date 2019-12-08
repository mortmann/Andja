using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class AIController : MonoBehaviour {
    public static AIController Instance { get; protected set; }
    public Dictionary<Island, List<TileValue>> islandToMapSpaceValuedTiles;
    public Dictionary<Island, List<TileValue>> islandToCurrentSpaceValuedTiles;

    // Use this for initialization
    void Start () {
        islandToMapSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        islandToCurrentSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        Debug.Log(Vector2.Max(new Vector2(8, 4), new Vector2(10, 1)));
        if (Instance != null) {
            Debug.LogError("There should never be two AIController.");
        }
        Instance = this;

        AIPlayer test = new AIPlayer(PlayerController.GetPlayer(1));
        test.CalculatePlayersCombatValue();
        World world = World.Current;
        foreach (Island island in World.Current.IslandList) {
            Vector2[,] seValue = new Vector2[island.Width, island.Height];
            Dictionary<TileType, Vector2[,]> typeToSEValue = new Dictionary<TileType, Vector2[,]>();
            Dictionary<TileType, Vector2[,]> typeToNWValue = new Dictionary<TileType, Vector2[,]>();
            foreach (TileType tt in typeof(TileType).GetEnumValues()) {
                typeToSEValue[tt] = new Vector2[island.Width, island.Height];
                typeToNWValue[tt] = new Vector2[island.Width, island.Height];
            }
            for (int y = 1; y < island.Height; y++) {
                for (int x = 1; x < island.Width; x++) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.CheckTile()) {
                        seValue[x, y].x = seValue[x - 1, y].x + 1;
                        seValue[x, y].y = seValue[x, y - 1].y + 1;
                    }
                    typeToSEValue[t.Type][x, y].x = typeToSEValue[t.Type][x - 1, y].x + 1;
                    typeToSEValue[t.Type][x, y].y = typeToSEValue[t.Type][x, y - 1].y + 1;
                }
            }
            Vector2[,] nwValue = new Vector2[island.Width, island.Height];
            for (int y = island.Height-2; y > 1; y--) {
                for (int x = island.Width-2; x > 1; x--) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.CheckTile()) {
                        nwValue[x, y].x = nwValue[x + 1, y].x + 1;
                        nwValue[x, y].y = nwValue[x, y + 1].y + 1;
                    }
                    typeToNWValue[t.Type][x, y].x = typeToNWValue[t.Type][x - 1, y].x + 1;
                    typeToNWValue[t.Type][x, y].y = typeToNWValue[t.Type][x, y - 1].y + 1;

                }
            }
            List<TileValue> values = new List<TileValue>();
            for (int y = 1; y < island.Height; y++) {
                for (int x = 1; x < island.Width; x++) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.CheckTile()) {
                        values.Add(new TileValue(t,
                                        seValue[x,y],
                                        nwValue[x,y]
                              ));
                    } else {
                        values.Add(new TileValue(t,
                                                typeToSEValue[t.Type][x, y],
                                                typeToNWValue[t.Type][x, y]
                                        ));
                    }
                }
            }
            values.RemoveAll(x => x.MaxValue == 0);
            islandToMapSpaceValuedTiles[island] = values;
            //Dictionary<int, List<TileValue>> maxToValue = new Dictionary<int, List<TileValue>>();
            //int i = 1;
            //while (values.Count > 0) {
            //    List<TileValue> selected = new List<TileValue>(from TileValue in values
            //                                                   where TileValue.MinValue == i
            //                                                   select new TileValue(TileValue));
            //    maxToValue[i] = selected;
            //    values.RemoveAll(x => x.MaxValue == i);
            //    i++;
            //}

            islandToCurrentSpaceValuedTiles[island] = new List<TileValue>(from TileValue in values
                                                                          select new TileValue(TileValue)); //copy them
        }
        BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
    }

    private void OnStructureCreated(Structure structure, bool load) {
        
    }

    // Update is called once per frame
    void Update () {
		
	}
    public bool PlaceStructure(AIPlayer player, Structure structure, List<Tile> tiles, Unit buildUnit = null) {
        BuildController.Instance.BuildOnTile(structure, tiles, player.PlayerNummer, false, false, buildUnit);
        return true;
    }

}
public class TileValue {
    public TileType Type => tile.Type;
    public int X => tile.X;
    public int Y => tile.Y;
    public int MaxValue => (int)Mathf.Max(seValue.x, seValue.y, nwValue.x, seValue.y);
    public int MinValue => (int)Mathf.Min(seValue.x, seValue.y, nwValue.x, seValue.y);
    public Vector2 MaxVector => Vector2.Max(seValue, nwValue);
    public Vector2 MinVector => Vector2.Min(seValue, nwValue);
    public Tile tile;
    public Vector2 seValue;
    public Vector2 nwValue;

    public TileValue(Tile tile, Vector2 seValue, Vector2 nwValue) {
        this.tile = tile;
        this.seValue = seValue;
        this.nwValue = nwValue;
    }

    public TileValue(TileValue tileValue) {
        this.tile = tileValue.tile;
        this.seValue = tileValue.seValue;
        this.nwValue = tileValue.nwValue;
    }

    public override bool Equals(object obj) {
        TileValue p = obj as TileValue;
        if ((object)p == null) {
            return false;
        }
        // Return true if the fields match:
        return p == this;
    }

    public override int GetHashCode() {
        var hashCode = 971533886;
        hashCode = hashCode * -1521134295 + seValue.GetHashCode();
        hashCode = hashCode * -1521134295 + nwValue.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(TileValue a, TileValue b) {
        // If both are null, or both are same instance, return true.
        if (System.Object.ReferenceEquals(a, b)) {
            return true;
        }

        // If one is null, but not both, return false.
        if (((object)a == null) || ((object)b == null)) {
            return false;
        }

        // Return true if the fields match:
        return a.seValue == b.seValue && a.nwValue == b.nwValue;
    }
    public static bool operator !=(TileValue a, TileValue b) {
        // If both are null, or both are same instance, return false.
        if (System.Object.ReferenceEquals(a, b)) {
            return false;
        }

        // If one is null, but not both, return true.
        if (((object)a == null) || ((object)b == null)) {
            return true;
        }

        // Return true if the fields not match:
        return a.seValue != b.seValue && a.nwValue != b.nwValue;
    }
}	
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
    public void DecideIsland() {
        //TODO:set here desires
        CalculateIslandScores();
        islandScores = islandScores.OrderByDescending(x => x.EndScore).ToList();
        //List<Tile> tiles = new List<Tile>(islandScores[0].Island.myTiles);
        //tiles.RemoveAll(x => x.Type != TileType.Shore);
        WarehouseStructure warehouse = PrototypController.Instance.FirstLevelWarehouse;
        //TODO: optimize
        List<TileValue> values = new List<TileValue>(AIController.Instance.islandToMapSpaceValuedTiles[islandScores[0].Island]);
        values.RemoveAll(x => x.Type != TileType.Shore);
        List<TileValue> selected = new List<TileValue>(from TileValue in values
                                                       where TileValue.MaxValue >= warehouse.Height
                                                       select new TileValue(TileValue));
        foreach (TileValue t in selected) {
            List <Tile> buildtiles = warehouse.GetBuildingTiles(t.X, t.Y, false, true);
            for(int i=0;i<4;i++) {
                if (warehouse.CanBuildOnSpot(buildtiles)) {
                    AIController.Instance.PlaceStructure(this, warehouse, buildtiles);
                    return;
                }
                warehouse.RotateStructure();
            }
            
        }
    }
    public void CalculateIslandScores() {
        List<Island> islands = World.Current.IslandList;
        islandScores = new List<IslandScore>();
        int maxSize = 0;
        int averageSize = 0;
        Dictionary<string, int> ressourceIDtoAverageAmount = new Dictionary<string, int>();
        Dictionary<string, int> ressourceIDtoExisting = new Dictionary<string, int>();
        Dictionary<Fertility, int> fertilitytoExisting = new Dictionary<Fertility, int>();
        foreach (Island island in islands) {
            if (island.myTiles.Count > maxSize)
                maxSize = island.myTiles.Count;
            averageSize += island.myTiles.Count;
            averageSize -= island.myTiles.FindAll(x => x.CheckTile()).Count;
            if (island.Ressources != null || island.Ressources.Count != 0) {
                foreach (string resid in island.Ressources.Keys) {
                    if (ressourceIDtoAverageAmount.ContainsKey(resid)) {
                        ressourceIDtoAverageAmount[resid] += island.Ressources[resid];
                        ressourceIDtoExisting[resid]++;
                    }
                    else {
                        ressourceIDtoAverageAmount[resid] = island.Ressources[resid];
                        ressourceIDtoExisting[resid]=1;
                    }
                }
                foreach (Fertility fer in island.myFertilities) {
                    if (fertilitytoExisting.ContainsKey(fer)) {
                        fertilitytoExisting[fer]++;
                    }
                    else {
                        fertilitytoExisting[fer]=1;
                    }
                }
            }
        }
        foreach (string resid in ressourceIDtoAverageAmount.Keys) {
            ressourceIDtoAverageAmount[resid] /= islands.Count;
            ressourceIDtoExisting[resid] = 1 - (ressourceIDtoExisting[resid]/islands.Count);
        }
        averageSize /= islands.Count;
        foreach (Island island in islands) {
            IslandScore score = new IslandScore {
                Island = island,
                ShapeScore = 1,
                SizeSimilarIslandScore = 1,
            };
            //Calculate Ressource Score
            if (island.Ressources == null || island.Ressources.Count == 0)
                score.RessourceScore = 0; // There is none
            else {
                //for each give it a score for each existing on the island
                foreach(string resid in island.Ressources.Keys) { 
                    // if it is needed RIGHT NOW score it after how much it exist on this in the average over ALL islands
                    if(neededRessources.Contains(resid)) {
                        score.RessourceScore += island.Ressources[resid] / ressourceIDtoAverageAmount[resid];
                    }
                    // its always nice to have -- add how rare it is in the world
                    score.RessourceScore += ressourceIDtoExisting[resid];
                }
            }

            score.SizeScore = (float)island.myTiles.Count;
            score.SizeScore -= island.myTiles.FindAll(x => x.CheckTile()).Count;
            score.SizeScore /= averageSize;

            //Calculate Fertility Score
            foreach (Fertility fertility in island.myFertilities) {
                // add how rare it is in the world -- multiple this??
                if (neededFertilities.Contains(fertility)) {
                    score.FertilityScore += fertilitytoExisting[fertility];
                }
                // its always nice to have -- add how rare it is in the world
                score.FertilityScore += fertilitytoExisting[fertility];
            }
            List<Island> myIslands = new List<Island>(player.GetIslandList());
            //Distance Score is either how far it is from other islands OR how far from center
            if (myIslands.Count > 0) {
                float distance = 0;
                foreach(Island isl in myIslands) {
                    distance += Vector2.Distance(island.Center , isl.Center);
                }
                distance /= myIslands.Count;
                score.DistanceScore = distance;
            } else {
                score.DistanceScore = Vector2.Distance(island.Center, World.Current.Center);
            }
            //Competition Score is the percentage of unclaimed Tiles multiplied through how many diffrent players
            if(island.myCities.Count>0) {
                float avaibleTiles = island.myTiles.Count;
                foreach (City c in island.myCities) {
                    if (c.IsWilderness())
                        continue;
                    avaibleTiles -= c.MyTiles.Count;
                }
                score.CompetitionScore = avaibleTiles / island.myTiles.Count;
                score.CompetitionScore *= island.myCities.Count;
            }
            islandScores.Add(score);
            Debug.Log("Calculated Island Score " + score.EndScore + " " + score.Island.StartTile.Vector2);
        }
        Debug.Log("Calculated Islands Scores");
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
            Debug.Log("Calculated PlayerCombat Score " + value.EndScore);
            combatValues.Add(value);
        }
    }

}
public class PlayerCombatValue {
    public Player Player;
    public float EndScore => UnitValue * 0.5f + ShipValue * 0.5f + MoneyValue * 0.25f + MilitaryStructureValue * 0.25f;
    public float UnitValue;
    public float ShipValue;
    public float MoneyValue; // are they supposed to know how much they have?
    public float MilitaryStructureValue;
    //public float TechnologyValue; // still not implemented

    public PlayerCombatValue(Player player, PlayerCombatValue isMe) {
        Player = player;
        UnitValue = 0;
        foreach (Unit u in player.GetLandUnits()) {
            UnitValue += u.Damage / 2 + u.MaxHealth / 2;
        }
        ShipValue = 0;
        foreach (Ship s in player.GetShipUnits()) {
            ShipValue += s.Damage / 2 + s.MaxHealth / 2;
        }
        List<MilitaryStructure> militaryStructures = new List<MilitaryStructure>(player.AllStructures.OfType<MilitaryStructure>());
        MilitaryStructureValue = 0;
        foreach (MilitaryStructure structure in militaryStructures) {
           MilitaryStructureValue++;
        }
        //imitates a guess how much the player makes 
        //also guess the money in the bank?
        if(isMe != null) {
            MoneyValue = UnityEngine.Random.Range(player.TreasuryChange - player.TreasuryChange / 4, player.TreasuryChange + player.TreasuryChange / 4);
            //compare the value to the calculating player
            UnitValue = Divide(UnitValue,isMe.UnitValue);
            ShipValue = Divide(ShipValue, isMe.ShipValue);
            MoneyValue = Divide(MoneyValue, isMe.MoneyValue);
            MilitaryStructureValue = Divide(MilitaryStructureValue, isMe.MilitaryStructureValue);
        }
        else {
            MoneyValue = player.TreasuryChange;
        }
    }
    float Divide(float one, float two) {
        if (two == 0)
            return one;
        return one / two;
    }
}
public struct IslandScore {
    public Island Island;
    public float EndScore => SizeScore * 0.5f + SizeSimilarIslandScore * 0.2f + 
                             RessourceScore * 0.1f + FertilityScore * 0.1f + 
                             DistanceScore * 0.3f;
    public float SizeScore;
    public float SizeSimilarIslandScore;
    public float RessourceScore;
    public float FertilityScore;
    public float CompetitionScore;
    public float DistanceScore;
    public float ShapeScore;//Not sure if this is feasible!
}