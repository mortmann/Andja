using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Tilemaps;

public class AIController : MonoBehaviour {
    public static AIController Instance { get; protected set; }
    public Dictionary<Island, List<TileValue>> islandToMapSpaceValuedTiles;
    public Dictionary<Island, List<TileValue>> islandToCurrentSpaceValuedTiles;
    public Dictionary<Island, Dictionary<Tile,TileValue>> islandsTileToValue;

    // Use this for initialization
    void Start () {
        islandToMapSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        islandToCurrentSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        islandsTileToValue = new Dictionary<Island, Dictionary<Tile, TileValue>>();
        if (Instance != null) {
            Debug.LogError("There should never be two AIController.");
        }
        Instance = this;

        AIPlayer test = new AIPlayer(PlayerController.GetPlayer(1));
        test.CalculatePlayersCombatValue();
        World world = World.Current;
        Dictionary<string, TileBase> stringToBase = new Dictionary<string, TileBase>();
        foreach (Island island in World.Current.Islands) {
            Vector2[,] swValue = new Vector2[island.Width, island.Height];
            Vector2[,] neValue = new Vector2[island.Width, island.Height];
            Dictionary<TileType, Vector2[,]> typeToSWValue = new Dictionary<TileType, Vector2[,]>();
            Dictionary<TileType, Vector2[,]> typeToNEValue = new Dictionary<TileType, Vector2[,]>();
            foreach (TileType tt in typeof(TileType).GetEnumValues()) {
                typeToSWValue[tt] = new Vector2[island.Width, island.Height];
                typeToNEValue[tt] = new Vector2[island.Width, island.Height];
            }
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x > 0)
                            startX = swValue[x - 1, y].x;
                        if (y > 0)
                            startY = swValue[x, y - 1].y;
                        swValue[x, y].x = startX + 1;
                        swValue[x, y].y = startY + 1;
                    }
                    if (x > 0)
                        startX = typeToSWValue[t.Type][x - 1, y].x;
                    if (y > 0)
                        startY = typeToSWValue[t.Type][x, y - 1].y;
                    typeToSWValue[t.Type][x, y].x = startX + 1;
                    typeToSWValue[t.Type][x, y].y = startY + 1;
                }
            }
            for (int y = island.Height-1; y > 0; y--) {
                for (int x = island.Width-1; x > 0; x--) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x < island.Width - 1)
                            startX = neValue[x + 1, y].x;
                        if (y < island.Height - 1)
                            startY = neValue[x, y + 1].y;
                        neValue[x, y].x = startX + 1;
                        neValue[x, y].y = startY + 1;
                    }
                    if (x < island.Width - 1)
                        startX = typeToNEValue[t.Type][x + 1, y].x;
                    if (y < island.Height - 1)
                        startY = typeToNEValue[t.Type][x, y + 1].y;
                    typeToNEValue[t.Type][x, y].x = startX + 1;
                    typeToNEValue[t.Type][x, y].y = startY + 1;
                }
            }
            List<TileValue> values = new List<TileValue>();
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    Tile t = world.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.CheckTile()) {
                        values.Add(new TileValue(t,
                                        swValue[x,y],
                                        neValue[x,y]
                              ));
                    } else {
                        values.Add(new TileValue(t,
                                                typeToSWValue[t.Type][x, y],
                                                typeToNEValue[t.Type][x, y]
                                        ));
                    }
                }
            }
            //values.RemoveAll(x => x.MaxValue == 0);
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

            islandToMapSpaceValuedTiles[island] = values;
            islandToCurrentSpaceValuedTiles[island] = new List<TileValue>(from TileValue in values
                                                                          select new TileValue(TileValue)); //copy them
            islandsTileToValue[island] = new Dictionary<Tile, TileValue>();
            foreach (TileValue tv in islandToCurrentSpaceValuedTiles[island]) {
                islandsTileToValue[island][tv.tile] = tv;
            }
        }
        BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
        BuildController.Instance.RegisterStructureDestroyed(OnStructureDestroyed);

        //TextToTexture = new TextToTexture(font, 32, 32, false);
        //foreach (TileValue tv in values) {
        //    string n = tv.ToString();
        //    if (stringToBase.ContainsKey(n) == false) {
        //        UnityEngine.Tilemaps.Tile tileBase = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
        //        tileBase.sprite = Sprite.Create(TextToTexture.CreateTextToTexture(n, 1, 1, 256, 1, 0.2f), new Rect(0, 0, 256, 256), Vector2.zero);
        //        tileBase.colliderType = UnityEngine.Tilemaps.Tile.ColliderType.None;
        //        stringToBase[n] = tileBase;
        //    }
        //}
        //islandToMap = new Dictionary<Island, Tilemap>();
        //foreach(Island i in world.IslandList) {
        //    GameObject island_tilemap = new GameObject();
        //    island_tilemap.transform.position = i.Placement;
        //    Tilemap tilemap = island_tilemap.AddComponent<Tilemap>();
        //    Grid g = island_tilemap.AddComponent<Grid>();
        //    g.cellSize = new Vector3(1, 1, 0);
        //    g.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        //    g.cellLayout = GridLayout.CellLayout.Rectangle;
        //    TilemapRenderer trr = island_tilemap.AddComponent<TilemapRenderer>();
        //    trr.sortingLayerName = "Tile";
        //    tilemap.size = new Vector3Int(i.Width, i.Height, 0);
        //    islandToMap.Add(i, tilemap);
        //    foreach (Tile t in islandsTileToValue[i].Keys) {
        //        TileValue tv = islandsTileToValue[i][t];
        //        tilemap.SetTile(new Vector3Int((int)(t.X - i.Placement.x), (int)(t.Y - i.Placement.y), 0), stringToBase[tv.ToString()]);
        //    }

        //}

    }
    private void OnDestroy() {
        Instance = null;
    }
    internal string GetTileValue(Tile tile) {
        if (tile.Type == TileType.Ocean)
            return "";
        if(islandsTileToValue[tile.Island].ContainsKey(tile)==false) {
            return "ERROR";
        }
        return islandsTileToValue[tile.Island][tile].ToString();
    }

    private void OnStructureDestroyed(Structure obj) {
    }

    private void OnStructureCreated(Structure structure, bool load) {
        if (structure.CanBeBuildOver)
            return;
        Island island = structure.City.island;
        List<Tile> toChange = new List<Tile>();
        Dictionary<Tile, TileValue> tileValue = islandsTileToValue[island];
        
        foreach (Tile t in structure.StructureTiles) {
            tileValue[t].neValue = Vector2.zero;
            tileValue[t].swValue = Vector2.zero;
        }
        foreach (Tile t in structure.StructureTiles) {
            ChangeTileValue(t.North(), 1, Direction.N);
            ChangeTileValue(t.East(), 1, Direction.E);
            ChangeTileValue(t.South(), 1, Direction.S);
            ChangeTileValue(t.West(), 1, Direction.W);
        }
        
    }

    private void ChangeTileValue(Tile t,int value, Direction direction) {
        if(t.Type == TileType.Ocean) {
            return;
        }
        Dictionary<Tile, TileValue> tileValue = islandsTileToValue[t.Island];
        if (tileValue.ContainsKey(t) ==false) {
            return;
        }
        if (tileValue[t].MaxValue == 0)
            return;
        switch (direction) {
            case Direction.N:
                tileValue[t].swValue.y = value;
                ChangeTileValue(t.North(), ++value, Direction.N);
                break;
            case Direction.W:
                tileValue[t].neValue.x = value;
                ChangeTileValue(t.East(), ++value, Direction.E);
                break;
            case Direction.S:
                tileValue[t].neValue.y = value;
                ChangeTileValue(t.South(), ++value, Direction.S);
                break;
            case Direction.E:
                tileValue[t].swValue.x = value;
                ChangeTileValue(t.West(), ++value, Direction.W);
                break;
        }

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
    public int MaxValue => (int)Mathf.Max(swValue.x, swValue.y, neValue.x, swValue.y);
    public int MinValue => (int)Mathf.Min(swValue.x, swValue.y, neValue.x, swValue.y);
    public Vector2 MaxVector => Vector2.Max(swValue, neValue);
    public Vector2 MinVector => Vector2.Min(swValue, neValue);
    public Tile tile;
    public Vector2 swValue;
    public Vector2 neValue;

    public TileValue(Tile tile, Vector2 seValue, Vector2 nwValue) {
        this.tile = tile;
        this.swValue = seValue;
        this.neValue = nwValue;
    }

    public TileValue(TileValue tileValue) {
        this.tile = tileValue.tile;
        this.swValue = tileValue.swValue;
        this.neValue = tileValue.neValue;
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
        hashCode = hashCode * -1521134295 + swValue.GetHashCode();
        hashCode = hashCode * -1521134295 + neValue.GetHashCode();
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
        return a.swValue == b.swValue && a.neValue == b.neValue;
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
        return a.swValue != b.swValue && a.neValue != b.neValue;
    }

    public override string ToString() {
        return "N" + neValue.y + "\nW" + swValue.x +"  E" + neValue.x + "\nS" + swValue.y ;
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
        List<Island> islands = World.Current.Islands;
        islandScores = new List<IslandScore>();
        int maxSize = 0;
        int averageSize = 0;
        Dictionary<string, int> ressourceIDtoAverageAmount = new Dictionary<string, int>();
        Dictionary<string, int> ressourceIDtoExisting = new Dictionary<string, int>();
        Dictionary<Fertility, int> fertilitytoExisting = new Dictionary<Fertility, int>();
        foreach (Island island in islands) {
            if (island.Tiles.Count > maxSize)
                maxSize = island.Tiles.Count;
            averageSize += island.Tiles.Count;
            averageSize -= island.Tiles.FindAll(x => x.CheckTile()).Count;
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

            score.SizeScore = (float)island.Tiles.Count;
            score.SizeScore -= island.Tiles.FindAll(x => x.CheckTile()).Count;
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
                score.DistanceScore = Vector2.Distance(island.Center, World.Current.Center);
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
            //Debug.Log("Calculated PlayerCombat Score " + value.EndScore);
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