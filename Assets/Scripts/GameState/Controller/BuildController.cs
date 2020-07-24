#region License
// ====================================================
// Andja Copyright(C) 2016 Team Mortmann
// ====================================================
#endregion
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Build state modes.
/// </summary>
public enum BuildStateModes { None, Build, Destroy }

/// <summary>
/// Build controller.
/// </summary>
public class BuildController : MonoBehaviour {
    public static BuildController Instance { get; protected set; }
    protected BuildStateModes _buildState;
    public BuildStateModes BuildState {
        get { return _buildState; }
        set {
            if (_buildState == value) {
                return;
            }
            _buildState = value;
            cbBuildStateChange?.Invoke(_buildState);
        }
    }
    public uint buildID = 0;
    public bool noBuildCost = false;
    public bool noUnitRestriction = false;
    public bool allStructuresEnabled = false;

    public IReadOnlyDictionary<string, Structure> StructurePrototypes {
        get { return PrototypController.Instance.StructurePrototypes; }
    }

    public List<Structure> LoadedStructures { get; private set; }

    public Structure toBuildStructure;

    Action<Structure, bool> cbStructureCreated;
    Action<Structure, IWarfare> cbAnyStructureDestroyed;
    Action<City> cbCityCreated;
    Action<City> cbAnyCityDestroyed;
    Action<BuildStateModes> cbBuildStateChange;


    public Dictionary<string, Item> GetCopieOfAllItems() {
        return PrototypController.Instance.GetCopieOfAllItems();
    }

    public void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two BuildController.");
        }
        Instance = this;
        if(EditorController.IsEditor) {
            noBuildCost = true;
            noUnitRestriction = true;
        } else
        if (noBuildCost && noUnitRestriction) {
            Debug.LogWarning("Cheats are activated.");
        }
        BuildState = BuildStateModes.None;
        buildID = 0;
    }

    internal void PlaceWorldGeneratedStructure(Dictionary<Tile, Structure> tileToStructure) {
        foreach (Tile t in tileToStructure.Keys) {
            RealBuild(new List<Tile>() { t }, tileToStructure[t], -1, false, true, null, true);
        }
        LoadedStructures = new List<Structure>(tileToStructure.Values);
        buildID = LoadedStructures.Max(x => x.buildID) + 1;
    }

    string settleStructure = null;

    public void SettleFromUnit(Unit buildUnit = null) {
        if (settleStructure == null)
            settleStructure = PrototypController.Instance.GetFirstLevelStructureIDForStructureType(typeof(WarehouseStructure));
        StartStructureBuild(settleStructure, buildUnit);
    }
    public void DestroyStructureOnTiles(IEnumerable<Tile> tiles, Player destroyPlayer, bool isGod=false) {
        foreach (Tile t in tiles) {
            DestroyStructureOnTile(t, destroyPlayer, isGod);
        }
    }
    /// <summary>
    /// Works only for current player not for someone else
    /// </summary>
    /// <param name="t">T.</param>
    public void DestroyStructureOnTile(Tile t, Player destroyPlayer, bool isGod = false) {
        if (EditorController.IsEditor == false && WorldController.Instance.IsPaused)
            return; // Not Editor but Paused Game -> no destruction
        if (t.Structure == null) {
            return;
        }
        if (isGod || t.Structure.PlayerNumber == destroyPlayer.Number) {
            t.Structure.Destroy();
        }
    }

    internal void SetLoadedStructures(IEnumerable<Structure> values) {
        LoadedStructures = new List<Structure>(values);
    }
    public void PlaceAllLoadedStructure(List<Structure> loadedStructures) {
        this.LoadedStructures = loadedStructures;
        //order by descending because we need to go from back to front -- for removing from the lost
        LoadedStructures = LoadedStructures.OrderByDescending(x=> x.buildID).ToList();
        for (int i = LoadedStructures.Count-1; i >= 0; i--) {
            if(LoadBuildOnTile(LoadedStructures[i], LoadedStructures[i].BuildTile)) {
                LoadedStructures[i].City.TriggerAddCallBack(LoadedStructures[i]);
            } else {
                LoadedStructures.RemoveAt(i);
            }
        }
        buildID = LoadedStructures[LoadedStructures.Count - 1].buildID++;
    }
    public void StartStructureBuild(string id, Unit buildInRangeUnit = null, Structure EditorStructure = null) {
        if (StructurePrototypes.ContainsKey(id) == false) {
            Debug.LogError("BUTTON has ID that is not a structure prototypes ->o_O<- ");
            return;
        }
        toBuildStructure = StructurePrototypes[id].Clone();
        if(EditorStructure!=null)
            toBuildStructure = EditorStructure;
        if (StructurePrototypes[id].BuildTyp == BuildType.Path) {
            MouseController.Instance.mouseState = MouseState.BuildPath;
            MouseController.Instance.ToBuildStructure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildType.Single) {
            MouseController.Instance.mouseState = MouseState.BuildSingle;
            MouseController.Instance.ToBuildStructure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildType.Drag) {
            MouseController.Instance.mouseState = MouseState.BuildDrag;
            MouseController.Instance.ToBuildStructure = toBuildStructure;
        }
        BuildState = BuildStateModes.Build;
    }
    public void CurrentPlayerBuildOnTile(List<Tile> tiles, bool forEachTileOnce, int playerNumber, bool wild = false, Unit buildInRange = null) {
        if (toBuildStructure == null) {
            return;
        }
        BuildOnTile(toBuildStructure, tiles, playerNumber, forEachTileOnce, wild, buildInRange);
    }
    public void BuildOnEachTile(Structure structure, List<Tile> tiles, int playerNumber) {
        BuildOnTile(structure, tiles, playerNumber, true);
    }
    public void BuildOnTile(Structure structure, List<Tile> tiles, int playerNumber, bool forEachTileOnce, 
                                bool wild = false, Unit buildInRange = null, bool loading = false, bool onStart = false) {
        if (tiles == null || tiles.Count == 0 || WorldController.Instance?.IsPaused == true && loading == false && onStart == false) {
            return;
        }
        if (forEachTileOnce == false) {
            RealBuild(tiles, structure, playerNumber, loading, wild, buildInRange, onStart);
        }
        else {
            foreach (Tile tile in tiles) {
                List<Tile> t = new List<Tile>();
                t.AddRange(structure.GetBuildingTiles(tile));
                RealBuild(t, structure, playerNumber, loading, wild, buildInRange);
            }
        }
    }
    public void EditorBuildOnTile(Structure str, Tile tile) {
        RealBuild(str.GetBuildingTiles(tile), str, -1, true, true);
    }
    internal void EditorBuildOnTile(Structure toPlace, List<Tile> t, bool single) {
        if (single) {
            BuildOnTile(toPlace, t, -1, single, true, null, true);
        } else {
            RealBuild(t, toPlace, -1, true, true);
        }
    }

    protected bool RealBuild(List<Tile> tiles, Structure structure, int playerNumber, bool loading = false, 
        bool buildInWilderness = false, Unit buildInRangeUnit = null, bool onStart = false) {
        if (tiles == null || tiles.Count == 0) {
            Debug.LogError("tiles is null or empty");
            return false;
        }
        if (buildInWilderness == false && playerNumber != -1 && PlayerController.GetPlayer(playerNumber)?.HasLost == true) {
            return false;
        }
        if (tiles.Exists(x => x == null || x.Type == TileType.Ocean)) {
            return false;
        }
        tiles = tiles.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
        int rotate = structure.rotation;
        if (loading == false) {
            structure = structure.Clone();
        }
        if (buildInRangeUnit != null && noUnitRestriction == false) {
            Vector3 unitPos = buildInRangeUnit.PositionVector2;
            Tile t = tiles.Find(x => { return x.IsInRange(unitPos, buildInRangeUnit.BuildRange); });
            if (t == null) {
                Debug.LogWarning("failed Range check -- Give UI feedback");
                return false;
            }
        }
        if(tiles[0].X == 260 && tiles[0].X == 198) {
            Debug.Log("WHY ARE YA STUPID");
        }
        //FIXME find a better solution for this?
        structure.ChangeRotation(rotate);
        //if is build in wilderniss city
        structure.buildInWilderniss = buildInWilderness;
        if (loading) {
            if(structure.City != null)
                playerNumber = structure.City.PlayerNumber;
            List<Tile> temp = new List<Tile>(tiles);
            //remove wilderniss
            temp.RemoveAll(x => x.City.IsWilderness());
            if(temp.Count == 0) {
                //non over cities? -> place it there
                buildInWilderness = true;
                playerNumber = -1;
            } else {
                //check if it is in a diffrent city -- and add it to that one 
                buildInWilderness = true; //so it doesnt check for incity
                //check first the current city owner
                int currentMaxCityTilesCount = temp.RemoveAll(x => x.City.PlayerNumber == playerNumber);
                while(temp.Count>0) {
                    //if there is a bigger 
                    int tempPlayerNR = temp[0].City.PlayerNumber;
                    int tempCityTiles = temp.RemoveAll(x => x.City.PlayerNumber == tempPlayerNR);
                    if (tempCityTiles > currentMaxCityTilesCount) {
                        //replace it and place it in that city
                        playerNumber = tempPlayerNR;
                        currentMaxCityTilesCount = tempCityTiles;
                    }
                }
            }
        }
        //search for a city that is from the placing player
        //and the structure gets deleted if it cant be placed anyway
        if (structure.City == null) {
            if (buildInWilderness == false) {
                bool inCity = structure.InCityCheck(tiles, playerNumber);
                //Failed to be in city range -- return
                if (inCity == false) {
                    return false;
                }
                structure.City = tiles[0].Island.Cities.Find(x => x?.PlayerNumber == playerNumber);
            }
            else
                structure.City = tiles[0].City;
        }
        //before we need to check if we can build THERE
        //we need to know if there is if we COULD build 
        //it anyway? that means enough ressources and enough Money
        Inventory inv = null;

        if (loading == false && buildInWilderness == false) {
            //TODO: Check for Event restricting building from players
            //return;
            //find a city that matches the player 
            //and check for money
            if (PlayerHasEnoughMoney(structure, playerNumber) == false && noBuildCost == false && onStart == false) {
                Debug.LogWarning("Building failed -> Player has not enough money -- Give UI feedback");
                return false;
            }
            if (structure.City == null && structure.GetType() != typeof(WarehouseStructure)) {
                return false; // SO no city found and no warehouse to create on
            }
            if (noBuildCost == false && onStart == false) {
                if (structure.GetBuildingItems() != null) {
                    if (buildInRangeUnit != null) {
                        inv = buildInRangeUnit.inventory;
                    }
                    else {
                        if(structure.City != null)
                            inv = structure.City.Inventory;
                    }
                    if (inv == null) {
                        Debug.LogError("Build something with smth that has no inventory");
                        return false;
                    }
                    if (inv.ContainsItemsWithRequiredAmount(structure.GetBuildingItems()) == false) {
                        Debug.Log("UI FEEDBACK NEEDED -- NOT ENOUGH MATERIAL TO BUILD");
                        return false;
                    }
                }
            }
        }
        //now we know that we COULD build that structure
        //but CAN WE?
        //check to see if the structure can be placed there
        if (structure.CheckPlaceStructure(tiles) == false) {
            if (loading && EditorController.IsEditor == false) {
                Debug.LogError("PLACING FAILED WHILE LOADING! " + structure.buildID + " - " + structure.SmallName);
                structure.Destroy();
            }
            return false;
        }
        //WE ARE HERE -- MEANS ALL CHECKS ARE DONE
        //IT WILL BE BUILD!
        //ALLOWS CREATION OF CITY when warehouse
        if (structure is WarehouseStructure && structure.City == null) {
            structure.City = CreateCity(tiles[0].Island, playerNumber);
        }
        structure.PlaceStructure(tiles);

        //pay for it -- if not otherwise disabled
        if (noBuildCost == false && onStart == false && buildInWilderness == false && loading == false) {
            if(structure.GetBuildingItems()!=null)
                inv.RemoveItemsAmount(structure.GetBuildingItems());
            PlayerController.GetPlayer(playerNumber).ReduceTreasure(structure.BuildCost);
        }
        structure.City.AddStructure(structure);
        //call all callbacks on structure created
        //FIXME remove this or smth -- why?
        if (loading == false) {
            // this is for loading so everything will be placed in order
            structure.buildID = buildID;
            buildID++;
        }
        if(onStart) {
            if (LoadedStructures == null) //should never happen but just in case
                LoadedStructures = new List<Structure>();
            LoadedStructures.Add(structure);
        }
        cbStructureCreated?.Invoke(structure, loading);
        structure.RegisterOnDestroyCallback(OnDestroyStructure);
        return true;
    }


    /// <summary>
    /// USED ONLY FOR LOADING
    /// DONT USE THIS FOR ANYTHING ELSE!!
    /// </summary>
    private bool LoadBuildOnTile(Structure s, Tile t) {
        if (s == null || t == null) {
            Debug.LogError("Something went wrong by loading Structure! " + t + " " + s);
            return false;
        }
        return RealBuild(s.GetBuildingTiles(t), s, -1, true, s.buildInWilderniss);
    }
    public void OnDestroyStructure(Structure str, IWarfare destroyer) {
        cbAnyStructureDestroyed?.Invoke(str, destroyer);
    }
    public bool PlayerHasEnoughMoney(Structure s, int playerNumber) {
        if (PlayerController.GetPlayer(playerNumber).TreasuryBalance >= s.BuildCost) {
            return true;
        }
        return false;
    }
    
    public City CreateCity(Island i, int playernumber) {
        if (i == null) {
            Debug.LogError("CreateCity called not on a island!");
            return null;
        }
        City c = i.CreateCity(playernumber);
        c.RegisterCityDestroy(cbAnyCityDestroyed);
        // needed for mapimage
        cbCityCreated?.Invoke(c);
        return c;
    }

    public void ResetBuild() {
        BuildState = BuildStateModes.None;
        if (MouseController.Instance.mouseState != MouseState.Idle) {
            //Reset MouseController out of BuildMode that this set it to 
            //when a structure gets selected 
            MouseController.Instance.mouseState = MouseState.Idle;
            MouseController.Instance.ResetBuild(null);
        }
        this.toBuildStructure = null;
    }
    public void DestroyToolSelect() {
        ResetBuild();
        BuildState = BuildStateModes.Destroy;
        MouseController.Instance.mouseState = MouseState.Destroy;
    }
    public void Escape() {
        ResetBuild();
    }


    public void RegisterStructureCreated(Action<Structure, bool> callbackfunc) {
        cbStructureCreated += callbackfunc;
    }
    public void UnregisterStructureCreated(Action<Structure, bool> callbackfunc) {
        cbStructureCreated -= callbackfunc;
    }
    public void RegisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc) {
        cbAnyStructureDestroyed += callbackfunc;
    }
    public void UnregisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc) {
        cbAnyStructureDestroyed -= callbackfunc;
    }
    public void RegisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated += callbackfunc;
    }
    public void UnregisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated -= callbackfunc;
    }
    public void RegisterAnyCityDestroyed(Action<City> callbackfunc) {
        cbAnyCityDestroyed += callbackfunc;
    }
    public void UnregisterAnyCityDestroyed(Action<City> callbackfunc) {
        cbAnyCityDestroyed -= callbackfunc;
    }
    public void RegisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
        cbBuildStateChange += callbackfunc;
    }
    public void UnregisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
        cbBuildStateChange -= callbackfunc;
    }
    void OnDestroy() {
        Instance = null;
    }
}
