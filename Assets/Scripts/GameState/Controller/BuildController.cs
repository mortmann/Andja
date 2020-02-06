#region License
// ====================================================
// Andja Copyright(C) 2016 Team Mortmann
// ====================================================
#endregion
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

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

    public IReadOnlyDictionary<string, Structure> StructurePrototypes {
        get { return PrototypController.Instance.StructurePrototypes; }
    }

    public List<Structure> LoadedStructures { get; private set; }

    public Structure toBuildStructure;

    Action<Structure, bool> cbStructureCreated;
    Action<Structure> cbAnyStructureDestroyed;
    Action<City> cbCityCreated;
    Action<BuildStateModes> cbBuildStateChange;


    public Dictionary<string, Item> GetCopieOfAllItems() {
        return PrototypController.Instance.GetCopieOfAllItems();
    }

    public void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two BuildController.");
        }
        Instance = this;
        if (noBuildCost && noUnitRestriction) {
            Debug.LogWarning("Cheats are activated.");
        }
        BuildState = BuildStateModes.None;
        buildID = 0;
    }

    internal void PlaceWorldGeneratedStructure(Dictionary<Tile, Structure> tileToStructure) {
        foreach (Tile t in tileToStructure.Keys) {
            RealBuild(new List<Tile>() { t }, tileToStructure[t], -1, true, true);
        }
    }

    string settleStructure = null;
    public void SettleFromUnit(Unit buildUnit = null) {
        if (settleStructure == null)
            settleStructure = PrototypController.Instance.GetFirstLevelStructureIDForStructureType(typeof(WarehouseStructure));
        OnClick(settleStructure, buildUnit);
    }
    public void DestroyStructureOnTiles(IEnumerable<Tile> tiles, Player destroyPlayer) {
        foreach (Tile t in tiles) {
            DestroyStructureOnTile(t, destroyPlayer);
        }
    }
    /// <summary>
    /// Works only for current player not for someone else
    /// </summary>
    /// <param name="t">T.</param>
    public void DestroyStructureOnTile(Tile t, Player destroyPlayer, bool isGod = false) {
        if (t.Structure == null) {
            return;
        }
        if (isGod || t.Structure.PlayerNumber == destroyPlayer.Number) {
            t.Structure.Destroy();
        }
    }
    public void OnClick(string id, Unit buildInRangeUnit = null, Structure EditorStructure = null) {
        if (StructurePrototypes.ContainsKey(id) == false) {
            Debug.LogError("BUTTON has ID that is not a structure prototypes ->o_O<- ");
            return;
        }
        toBuildStructure = StructurePrototypes[id].Clone();
        if(EditorStructure!=null)
            toBuildStructure = EditorStructure;
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Path) {
            MouseController.Instance.mouseState = MouseState.BuildPath;
            MouseController.Instance.ToBuildStructure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Single) {
            MouseController.Instance.mouseState = MouseState.BuildSingle;
            MouseController.Instance.ToBuildStructure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Drag) {
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
    public void BuildOnTile(Structure structure, List<Tile> tiles, int playerNumber, bool forEachTileOnce, bool wild = false, Unit buildInRange = null) {
        if (tiles == null || tiles.Count == 0 || WorldController.Instance?.IsPaused == true) {
            return;
        }
        if (forEachTileOnce == false) {
            RealBuild(tiles, structure, playerNumber, false, wild, buildInRange);
        }
        else {
            foreach (Tile tile in tiles) {
                List<Tile> t = new List<Tile>();
                t.AddRange(structure.GetBuildingTiles(tile.X, tile.Y));
                RealBuild(t, structure, playerNumber, false, wild, buildInRange);
            }
        }
    }
    public void EditorBuildOnTile(Structure str, Tile tile) {
        RealBuild(str.GetBuildingTiles(tile.X, tile.Y), str, -1, true, true);
    }
    internal void EditorBuildOnTile(Structure toPlace, List<Tile> t, bool single) {
        if (single) {
            BuildOnTile(toPlace, t, -1, single, true);
        } else {
            RealBuild(t, toPlace, -1, true, true);
        }
    }

    protected void RealBuild(List<Tile> tiles, Structure structure, int playerNumber, bool loading = false, bool wild = false, Unit buildInRangeUnit = null) {
        if (tiles == null) {
            Debug.LogError("tiles is null");
            return;
        }
        if (tiles.Exists(x => x == null || x.Type == TileType.Ocean)) {
            return;
        }
        if (tiles.Count == 0) {
            Debug.LogError("tiles is empty");
            return;
        }
        tiles = tiles.OrderBy(x => x.X).ThenBy(x => x.Y).ToList();

        int rotate = structure.rotated;
        if (loading == false) {
            structure = structure.Clone();
        }

        if (buildInRangeUnit != null && noUnitRestriction == false) {
            Vector3 unitPos = buildInRangeUnit.pathfinding.Position;
            Tile t = tiles.Find(x => { return x.IsInRange(unitPos, buildInRangeUnit.BuildRange); });
            if (t == null) {
                Debug.LogWarning("failed Range check -- Give UI feedback");
                return;
            }
        }
        //FIXME find a better solution for this?
        structure.rotated = rotate;
        //if is build in wilderniss city
        structure.buildInWilderniss = wild;


        //before we need to check if we can build THERE
        //we need to know if there is if we COULD build 
        //it anyway? that means enough ressources and enough Money
        if (loading == false && wild == false) {
            //TODO: Check for Event restricting building from players
            //return;
            //find a city that matches the player 
            //and check for money
            if (PlayerHasEnoughMoney(structure, playerNumber) == false) {
                Debug.Log("not playerHasEnoughMoney -- Give UI feedback");
                return;
            }
            //Is the player allowed to place it here? -> city
            Tile block = tiles.Find(x => x.City.IsWilderness() == false && x.City.playerNumber != playerNumber);
            if (block != null) {
                return; // there is a tile that is owned by another player
            }
            structure.City = tiles.Find(x => x.City.playerNumber == playerNumber)?.City;
            if (structure.City == null && structure.GetType() != typeof(WarehouseStructure)) {
                return; // SO no city found and no warehouse to create on
            } else
            if (structure.GetType() == typeof(WarehouseStructure)) {
                City c = tiles[0].Island.FindCityByPlayer(playerNumber);
                if(c!=null && c.warehouse!=null) {
                    return; // Already City existing here && has already a Warehouse
                }
                structure.City = CreateCity(tiles[0].Island, playerNumber);
            }
            if (noBuildCost == false) {
                if (structure.GetBuildingItems() != null) {
                    Inventory inv = null;
                    if (buildInRangeUnit != null) {
                        inv = buildInRangeUnit.inventory;
                    }
                    else {
                        if(structure.City != null)
                            inv = structure.City.inventory;
                    }
                    if (inv == null) {
                        Debug.LogError("Build something with smth that has no inventory");
                        return;
                    }
                    if (inv.ContainsItemsWithRequiredAmount(structure.GetBuildingItems()) == false) {
                        Debug.Log("ContainsItemsWithRequiredAmount==null");
                        return;
                    }
                }
            }
        }
        if (wild) {
            structure.City = tiles[0].Island.Wilderness;
        }
        //now we know that we COULD build that structure
        //but CAN WE?
        //check to see if the structure can be placed there
        if (structure.PlaceStructure(tiles) == false) {
            if (loading && EditorController.IsEditor == false) {
                Debug.LogError("PLACING FAILED WHILE LOADING! " + structure.buildID + " - " + structure.SmallName);
            }
            return;
        }

        if (buildInRangeUnit != null && noBuildCost == false) {
            buildInRangeUnit.inventory.RemoveItemsAmount(structure.GetBuildingItems());
        }
        structure.City.AddStructure(structure);

        //call all callbacks on structure created
        //FIXME remove this or smth
        cbStructureCreated?.Invoke(structure, loading);
        if (loading == false) {
            // this is for loading so everything will be placed in order
            structure.buildID = buildID;
            buildID++;
        }
        structure.RegisterOnDestroyCallback(OnDestroyStructure);
    }


    /// <summary>
    /// USED ONLY FOR LOADING
    /// DONT USE THIS FOR ANYTHING ELSE!!
    /// </summary>
    private void LoadBuildOnTile(Structure s, Tile t) {
        if (s == null || t == null) {
            Debug.LogError("Something went wrong by loading Structure! " + t + " " + s);
            return;
        }
        RealBuild(s.GetBuildingTiles(t.X, t.Y), s, -1, true, false);
    }
    public void OnDestroyStructure(Structure str) {
        cbAnyStructureDestroyed?.Invoke(str);
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
        // needed for mapimage
        cbCityCreated?.Invoke(c);
        return c;
    }


    public void PlaceAllLoadedStructure(List<Structure> loadedStructures) {
        this.LoadedStructures = loadedStructures;
        for (int i = 0; i < loadedStructures.Count; i++) {
            LoadBuildOnTile(loadedStructures[i], loadedStructures[i].BuildTile);
            loadedStructures[i].City.TriggerAddCallBack(loadedStructures[i]);
        }
    }
    public void ResetBuild() {
        BuildState = BuildStateModes.None;
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
    public void RegisterStructureDestroyed(Action<Structure> callbackfunc) {
        cbAnyStructureDestroyed += callbackfunc;
    }
    public void UnregisterStructureDestroyed(Action<Structure> callbackfunc) {
        cbAnyStructureDestroyed -= callbackfunc;
    }
    public void RegisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated += callbackfunc;
    }
    public void UnregisterCityCreated(Action<City> callbackfunc) {
        cbCityCreated -= callbackfunc;
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
