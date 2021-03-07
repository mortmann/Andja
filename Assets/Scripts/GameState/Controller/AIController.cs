using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Tilemaps;
using static PrototypController;
public class AIController : MonoBehaviour {
    public static AIController Instance { get; protected set; }
    public static Dictionary<Island, Dictionary<Tile, TileValue>> IslandsTileToValue {
        get {
            if (_islandsTileToValue == null)
                CalculateIslandTileValues();
            return _islandsTileToValue;
        }
        set => _islandsTileToValue = value; 
    }

    public static Dictionary<Island, List<TileValue>> IslandToMapSpaceValuedTiles {
        get {
            if (_islandToMapSpaceValuedTiles == null)
                CalculateIslandTileValues();
            return _islandToMapSpaceValuedTiles;
        }
        set => _islandToMapSpaceValuedTiles = value; }
    public static Dictionary<Island, List<TileValue>> IslandToCurrentSpaceValuedTiles {
        get {
            if (_islandToCurrentSpaceValuedTiles == null)
                CalculateIslandTileValues();
            return _islandToCurrentSpaceValuedTiles;
        }
        set => _islandToCurrentSpaceValuedTiles = value; }

    private static Dictionary<Island, List<TileValue>> _islandToMapSpaceValuedTiles;
    private static Dictionary<Island, List<TileValue>> _islandToCurrentSpaceValuedTiles;
    private static Dictionary<Island, Dictionary<Tile, TileValue>> _islandsTileToValue;
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two AIController.");
        }
        Instance = this;
    }
    // Use this for initialization
    void Start () {
        //foreach (Island island in World.Current.Islands) {
        //    foreach (City c in island.Cities) {
        //        foreach (Structure str in c.Structures)
        //            OnStructureCreated(str, true);
        //    }
        //}
        BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
        BuildController.Instance.RegisterStructureDestroyed(OnStructureDestroyed);

        AIPlayer test = new AIPlayer(PlayerController.GetPlayer(1));
        test.CalculatePlayersCombatValue();
        //Calculate();

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
        _islandToMapSpaceValuedTiles = null;
        _islandToCurrentSpaceValuedTiles = null;
        _islandsTileToValue = null;
    }

    internal string GetTileValue(Tile tile) {
        if (tile.Type == TileType.Ocean)
            return "";
        if(IslandsTileToValue[tile.Island].ContainsKey(tile)==false) {
            return "ERROR";
        }
        return IslandsTileToValue[tile.Island][tile].ToString();
    }

    private static void OnStructureDestroyed(Structure structure, IWarfare iwarfare) {
        for (int y = 0; y < structure.TileHeight; y++) {
            for (int x = 0; x < structure.TileWidth; x++) {
                Tile t = structure.Tiles[x + y * structure.TileHeight];
                if (x == 0) {
                    ChangeTileValue(t, t.West(), Direction.E);
                }
                if (x < structure.TileWidth) {
                    ChangeTileValue(t, t.South(), Direction.N);
                }
                if (x == structure.TileWidth - 1) {
                    ChangeTileValue(t, t.East(), Direction.W);
                }
                if (y == structure.TileHeight - 1) {
                    ChangeTileValue(t, t.North(), Direction.S);
                }
            }
        }

    }

    private static void OnStructureCreated(Structure structure, bool load) {
        if (structure.CanBeBuildOver)
            return;
        if (IslandsTileToValue == null)
            CalculateIslandTileValues();
        Island island = structure.BuildTile.Island;
        if (island == null)
            return;
        Dictionary<Tile, TileValue> tileValue = IslandsTileToValue[island];
        for (int y = 0; y < structure.TileHeight; y++) {
            for (int x = 0; x < structure.TileWidth; x++) {
                Tile t = structure.Tiles[x * structure.TileHeight + y];
                tileValue[t].neValue = Vector2.zero;
                tileValue[t].swValue = Vector2.zero;
                if (x == 0) {
                    ChangeTileValue(t.East(), t, Direction.E);
                }
                if (y == structure.TileHeight - 1) {
                    ChangeTileValue(t.North(), t, Direction.N);
                }
                if (x == structure.TileWidth - 1) {
                    ChangeTileValue(t.West(), t, Direction.W);
                }
                if (y == 0) {
                    ChangeTileValue(t.South(), t, Direction.S);
                }
            }
        }

    }

    private static void ChangeTileValue(Tile t, Tile tValue, Direction direction) {
        if(t.Type == TileType.Ocean) {
            return;
        }
        Dictionary<Tile, TileValue> tileValue = IslandsTileToValue[t.Island];
        if (t.IsGenericBuildType() != tValue.IsGenericBuildType()) {
            if (t.Type != tValue.Type)
                return;
        }
        switch (direction) {
            case Direction.N:
                tileValue[t].swValue.y = tileValue[t.South()].swValue.y + 1;
                if (t.North().Structure != null)
                    return;
                ChangeTileValue(t.North(), t, Direction.N);
                break;
            case Direction.W:
                tileValue[t].neValue.x = tileValue[t.East()].neValue.x + 1;
                if (t.West().Structure != null)
                    return;
                ChangeTileValue(t.West(), t, Direction.W);
                break;
            case Direction.S:
                tileValue[t].neValue.y = tileValue[t.North()].neValue.y + 1;
                if (t.South().Structure != null)
                    return;
                ChangeTileValue(t.South(), t, Direction.S);
                break;
            case Direction.E:
                tileValue[t].swValue.x = tileValue[t.West()].swValue.x + 1;
                if (t.East().Structure != null)
                    return;
                ChangeTileValue(t.East(), t, Direction.E);
                break;
        }

    }

    // Update is called once per frame
    void Update () {
		
	}
    public static bool PlaceStructure(AIPlayer player, Structure structure, List<Tile> tiles, Unit buildUnit = null, bool onStart = false) {
        BuildController.Instance.BuildOnTile(structure, tiles, player.PlayerNummer, false, false, buildUnit, false, onStart);
        return true;
    }

    public static void CalculateIslandTileValues() {
        _islandToMapSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        _islandToCurrentSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
        _islandsTileToValue = new Dictionary<Island, Dictionary<Tile, TileValue>>();

        World world = World.Current;
        foreach (Island island in World.Current.Islands) {
            
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

            IslandToMapSpaceValuedTiles[island] = TileValue.CalculateStartingValues(island);
            IslandToCurrentSpaceValuedTiles[island] = new List<TileValue>(from TileValue in IslandToMapSpaceValuedTiles[island]
                                                                          select new TileValue(TileValue)); //copy them
            IslandsTileToValue[island] = new Dictionary<Tile, TileValue>();
            foreach (TileValue tv in IslandToCurrentSpaceValuedTiles[island]) {
                IslandsTileToValue[island][tv.tile] = tv;
            }
        }
    }

    //private void Calculate() {
    //    populationStructures = new PopulationStructures[PCInstance.NumberOfPopulationLevels];
    //    for (int i = 0; i < PCInstance.NumberOfPopulationLevels; i++) {
    //        populationStructures[i] = new PopulationStructures();
    //        CalculatePLevelStuff(PCInstance.GetPopulationLevelPrototypDataForLevel(i));
    //    }
    //}
    //PopulationStructures[] populationStructures;
    //private void CalculatePLevelStuff(PopulationLevelPrototypData populationLevelPrototypData) {
    //    int MaxUnlockCount = 0;
    //    //TODO: make this while loading data in PrototypController
    //    foreach (NeedGroup ng in populationLevelPrototypData.GetCopyGroupNeedList()) {
    //        foreach (Need need in ng.Needs) {
    //            if (need.StartPopulationCount > MaxUnlockCount) {
    //                MaxUnlockCount = need.StartPopulationCount;
    //            }
    //        }
    //    }
    //    populationStructures[populationLevelPrototypData.LEVEL].needAtLeastHomes 
    //        = Mathf.CeilToInt((float)MaxUnlockCount / (float)populationLevelPrototypData.HomeStructure.MaxLivingSpaces);


    //    foreach (NeedGroup ng in populationLevelPrototypData.GetCopyGroupNeedList()) {
    //        foreach (Need need in ng.Needs) {
    //            if (need.IsStructureNeed()) {
    //                //TODO: calc the needed thingabobs for this :(
    //                continue;
    //            }
    //            //Minimum Needed Amount
    //            //float neededItemAmount = need.Uses[populationLevelPrototypData.LEVEL] * MaxUnlockCount;
    //            ProduceChains chains = new ProduceChains {
    //            };
    //            List<Produce> produce = PCInstance.ItemIDToProduce[need.Item.ID];
    //            foreach (Produce p in produce) {
    //                if (p.ProducerStructure.populationLevel > populationLevelPrototypData.LEVEL)
    //                    continue;
    //                if (p.needed == null) {



    //                    continue;
    //                }
                        
    //                StructureProportions thisProp = p.Proportion;
    //                float Usage = need.Uses[populationLevelPrototypData.LEVEL] * (60f / City.useTick);
    //                float Produce = p.producePerMinute;
    //                int count = Mathf.FloorToInt(Produce / Usage);
    //                List<List<ProportionsRelation>> itemToProportionsRelations = new List<List<ProportionsRelation>>();
    //                for (int i = 0; i < p.needed.Length; i++) {

    //                    if (PCInstance.Proportions.ContainsKey(p.needed[i].ID)) {
    //                        StructureProportions prop = ;
    //                        itemToProportionsRelations.Add(new List<ProportionsRelation>());
    //                        GetAllRelations(
    //                            new ProportionsRelation { PeopleCount = count, proportions = thisProp }, prop, itemToProportionsRelations[i]);
    //                    } else {
    //                        itemToProportionsRelations.Add(new List<ProportionsRelation> {
    //                            new ProportionsRelation (p, count, thisProp.itemToRatios[p.needed[i].ID].Ratio[p])
    //                        });
    //                    }
    //                }
    //                List<ProportionsRelation> first = itemToProportionsRelations[0];
    //                itemToProportionsRelations.RemoveAt(0);
    //                for (int i = 0; i < first.Count; i++) {
    //                    chains.relations = Combine(first[i],0,itemToProportionsRelations);
    //                }
    //                //if (p.produce.needed.Length > 1) { 
    //                //    List<ProportionsRelation> final = new List<ProportionsRelation>();
    //                //    List<ProportionsRelation> list = itemToProportionsRelations[p.produce.needed[0].ID];
    //                //    for (int l = 1; l < p.produce.needed.Length; l++) { // first list loop (1,2,3...) of first item
    //                //        for (int i = 1; i < p.produce.needed.Length; i++) { //loops over the rest of list items
    //                //            List<ProportionsRelation> list2 = itemToProportionsRelations[p.produce.needed[i].ID];
    //                //            for (int y = 2; y < itemToProportionsRelations.Count; y++) { // loops again over the list but only takes the 
    //                //                ProportionsRelation pr = list[i].CloneAndAdd(list2[i]).Add();
    //                //            }
    //                //        }
    //                //    }
    //                //}
    //            }
    //        }
    //    }
    //}

    ////private List<ProportionsRelation> GetCombinedList(int index, ProportionsRelation first,  List<List<ProportionsRelation>> itemToProportionsRelations) {
    ////    List<ProportionsRelation> list = new List<ProportionsRelation>();
    ////    for (int i = 0; i < itemToProportionsRelations.Count; i++) {
    ////        list.AddRange(Combine(first, i,))
    ////    }
    ////    return list;
    ////}
    //private List<ProportionsRelation> Combine(ProportionsRelation first, int indexL, List<List<ProportionsRelation>> itemToProportionsRelations) {
    //    if (indexL == itemToProportionsRelations.Count)
    //        return new List<ProportionsRelation> { first };
    //    List<ProportionsRelation> list = new List<ProportionsRelation>();
    //    for (int i = 0; i < itemToProportionsRelations.Count; i++) {
    //        list.AddRange(Combine(first.CloneAndAdd(itemToProportionsRelations[indexL][i]), indexL+1, itemToProportionsRelations));
    //    }
    //    return list;
    //}

    //private void GetAllRelations(ProportionsRelation parentPR, StructureProportions current, List<ProportionsRelation> proportionsRelations) {
    //    if (current.produce.needed != null) {
    //        foreach (Item item in current.produce.needed) {
    //            List<StructureProportions> proportions = PCInstance.Proportions[item.ID];
    //            foreach (StructureProportions sp in proportions) {
    //                ProportionsRelation clone = parentPR.CloneAndAdd(current);
    //                GetAllRelations(clone, sp, proportionsRelations);
    //            }
    //        }
    //    }
    //    else {
    //        parentPR.Add(current);
    //        proportionsRelations.Add(parentPR);
    //    }
    //}
    ////private ProportionsRelation BottomUp(StructureProportions current, List<ProportionsRelation> proportionsRelations) {
    ////    ProportionsRelation clone = new ProportionsRelation();
    ////    if (current.produce.needed != null) {
    ////        foreach(Item item in current.produce.needed) {
    ////            List<StructureProportions> proportions = PCInstance.Proportions[item.ID];
    ////            foreach (StructureProportions sp in proportions) {
    ////                clone = BottomUp(sp, proportionsRelations);
    ////            }
    ////        }
    ////    }
    ////    clone.TotalBuildCost += current.produce.ProducerStructure.buildcost;
    ////    clone.TotalMaintenance += current.produce.ProducerStructure.maintenanceCost;
    ////    clone.AddBuildItems(current.produce.ProducerStructure.buildingItems, 0);
    ////    proportionsRelations.Add(clone);
    ////    return clone;
    ////}
    //class PopulationStructures {
    //    public int needAtLeastHomes;

    //}
    //class ProduceChains {
    //    public Item produced;
    //    public List<ProportionsRelation> relations = new List<ProportionsRelation>();

    //}
    //class ProportionsRelation {
    //    public int PeopleCount;
    //    public StructureProportions proportions;
    //    public float TotalBuildCost;
    //    public float TotalMaintenance;
    //    public Item[] TotalItemCost;
    //    public Dictionary<string, float> ItemCostTemp = new Dictionary<string, float>();
    //    private Produce p;

    //    public ProportionsRelation(Produce p, int peopleCount, float ratio) {
    //        PeopleCount = peopleCount;
    //        TotalBuildCost = p.ProducerStructure.buildcost;
    //        TotalMaintenance = p.ProducerStructure.maintenanceCost;
    //        foreach(Item item in p.ProducerStructure.buildingItems) {
    //            ItemCostTemp[item.ID] = item.count * ratio;
    //        }
    //    }
    //    public ProportionsRelation() {

    //    }
    //    void CalculateTotalItemCost() {
    //        TotalItemCost = new Item[ItemCostTemp.Count];
    //        int i = 0;
    //        foreach(string id in ItemCostTemp.Keys) {
    //            TotalItemCost[i] = new Item(id, Mathf.CeilToInt(ItemCostTemp[id]));
    //            i++;
    //        } 
    //    }
    //    public ProportionsRelation Clone() {
    //        return new ProportionsRelation {
    //            PeopleCount = PeopleCount,
    //            proportions = proportions,
    //            TotalBuildCost = TotalBuildCost,
    //            TotalMaintenance = TotalMaintenance,
    //            TotalItemCost = TotalItemCost,
    //            ItemCostTemp = ItemCostTemp,
    //        };
    //    }

    //    internal void AddBuildItems(Item[] buildingItems, float ratio) {
    //        foreach(Item item in buildingItems) {
    //            if (ItemCostTemp.ContainsKey(item.ID) == false)
    //                ItemCostTemp[item.ID] = 0;
    //            ItemCostTemp[item.ID] += item.count * ratio;
    //        }
    //    }

    //    internal ProportionsRelation CloneAndAdd(StructureProportions sp) {
    //        ProportionsRelation clone = Clone();
    //        clone.Add(sp);
    //        return clone;
    //    }

    //    internal void Add(StructureProportions current) {
    //        TotalBuildCost += current.produce.ProducerStructure.buildcost;
    //        TotalMaintenance += current.produce.ProducerStructure.maintenanceCost;
    //        AddBuildItems(current.produce.ProducerStructure.buildingItems, 0);
    //    }

    //    internal ProportionsRelation CloneAndAdd(ProportionsRelation proportionsRelation) {
    //        ProportionsRelation clone = Clone();
    //        clone.TotalBuildCost += proportionsRelation.TotalBuildCost;
    //        clone.TotalMaintenance += proportionsRelation.TotalMaintenance;
    //        clone.AddBuildItems(proportionsRelation.TotalItemCost, 0);
    //        return clone;
    //    }
    //}
}
