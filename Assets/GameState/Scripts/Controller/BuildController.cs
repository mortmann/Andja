#region License
// ====================================================
// Andja Copyright(C) 2016 Team Mortmann
// ====================================================
#endregion
using System;
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

    public Dictionary<int, Structure> StructurePrototypes {
        get { return PrototypController.Instance.structurePrototypes; }
    }
    public Structure toBuildStructure;

    Action<Structure, bool> cbStructureCreated;
    Action<City> cbCityCreated;
    Action<BuildStateModes> cbBuildStateChange;

    public Dictionary<int, Item> GetCopieOfAllItems() {
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

    public void SettleFromUnit(Unit buildUnit = null) {
        //FIXME: get a way to get this id for warehouse
        OnClick(6, buildUnit);
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
    public void DestroyStructureOnTile(Tile t, Player destroyPlayer) {
        if (t.Structure == null) {
            return;
        }
        if (t.Structure.PlayerNumber == destroyPlayer.Number) {
            t.Structure.Destroy();
        }
    }
    public void OnClick(int id, Unit buildInRangeUnit = null) {
        if (StructurePrototypes.ContainsKey(id) == false) {
            Debug.LogError("BUTTON has ID that is not a structure prototypes ->o_O<- ");
            return;
        }
        toBuildStructure = StructurePrototypes[id].Clone();
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Path) {
            MouseController.Instance.mouseState = MouseState.Path;
            MouseController.Instance.Structure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Single) {
            MouseController.Instance.mouseState = MouseState.Single;
            MouseController.Instance.Structure = toBuildStructure;
        }
        if (StructurePrototypes[id].BuildTyp == BuildTypes.Drag) {
            MouseController.Instance.mouseState = MouseState.Drag;
            MouseController.Instance.Structure = toBuildStructure;
        }
        BuildState = BuildStateModes.Build;
    }
    public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, int playerNumber, bool wild = false, Unit buildInRange = null) {
        if (toBuildStructure == null) {
            return;
        }
        BuildOnTile(tiles, forEachTileOnce, toBuildStructure, playerNumber, wild, buildInRange);
    }
    /// <summary>
    /// USED ONLY FOR LOADING
    /// DONT USE THIS FOR ANYTHING ELSE!!
    /// </summary>
    /// <param name="s">S.</param>
    /// <param name="t">T.</param>
    private void BuildOnTile(Structure s, Tile t) {
        if (s == null || t == null) {
            Debug.LogError("Something went wrong by loading Structure! " + t + " " + s);
            return;
        }
        RealBuild(s.GetBuildingTiles(t.X, t.Y), s, -1, true, false);
    }
    public void BuildOnTile(List<Tile> tiles, bool forEachTileOnce, Structure structure, int playerNumber, bool wild = false, Unit buildInRange = null) {
        if (tiles == null || tiles.Count == 0 || WorldController.Instance.IsPaused) {
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
    protected void RealBuild(List<Tile> tiles, Structure s, int playerNumber, bool loading = false, bool wild = false, Unit buildInRangeUnit = null) {
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
        int rotate = s.rotated;
        if (loading == false) {
            s = s.Clone();
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
        s.rotated = rotate;
        //if is build in wilderniss city
        s.buildInWilderniss = wild;


        //before we need to check if we can build THERE
        //we need to know if there is if we COULD build 
        //it anyway? that means enough ressources and enough Money
        if (loading == false && wild == false) {
            //TODO: Check for Event restricting building from players
            //return;
            //find a city that matches the player 
            //and check for money
            if (PlayerHasEnoughMoney(s, playerNumber) == false) {
                Debug.Log("not playerHasEnoughMoney -- Give UI feedback");
                return;
            }
            //Is the player allowed to place it here? -> city

            Tile block = tiles.Find(x => x.MyCity.IsWilderness() == false && x.MyCity.playerNumber != playerNumber);
            if (block != null) {
                return; // there is a tile that is owned by another player
            }
            Tile hasCity = tiles.Find(x => x.MyCity.playerNumber == playerNumber);
            if (hasCity == null) {
                if (s.GetType() == typeof(WarehouseStructure)) {
                    s.City = CreateCity(tiles[0].MyIsland, playerNumber);
                }
                else {
                    return; // SO no city found and no warehouse to create on
                }
            }
            else {
                s.City = hasCity.MyCity;
            }
            if (noBuildCost == false) {
                if (s.GetBuildingItems() != null) {
                    Inventory inv = null;
                    if (buildInRangeUnit != null) {
                        inv = buildInRangeUnit.inventory;
                    }
                    else {
                        inv = hasCity.MyCity.inventory;
                    }
                    if (inv == null) {
                        Debug.LogError("Build something with smth that has no inventory");
                        return;
                    }
                    if (inv.ContainsItemsWithRequiredAmount(s.GetBuildingItems()) == false) {
                        Debug.Log("ContainsItemsWithRequiredAmount==null");
                        return;
                    }
                }
            }
        }
        if (wild) {
            s.City = tiles[0].MyIsland.Wilderness;
        }
        //now we know that we COULD build that structure
        //but CAN WE?
        //check to see if the structure can be placed there
        if (s.PlaceStructure(tiles) == false) {
            if (loading) {
                Debug.LogError("PLACING FAILED WHILE LOADING! " + s.buildID + " - " + s.SmallName);
            }
            return;
        }

        if (buildInRangeUnit != null && noBuildCost == false) {
            buildInRangeUnit.inventory.RemoveItemsAmount(s.GetBuildingItems());
        }
        s.City.AddStructure(s);

        //call all callbacks on structure created
        //FIXME remove this or smth
        cbStructureCreated?.Invoke(s, loading);
        if (loading == false) {
            // this is for loading so everything will be placed in order
            s.buildID = buildID;
            buildID++;
        }
        s.RegisterOnDestroyCallback(OnDestroyStructure);
    }
    public void OnDestroyStructure(Structure str) {
        //		str.City.removeStructure (str);
    }
    public bool PlayerHasEnoughMoney(Structure s, int playerNumber) {
        if (PlayerController.Instance.GetPlayer(playerNumber).Balance >= s.Buildcost) {
            return true;
        }
        return false;
    }
    public void BuildOnTile(int id, List<Tile> tiles, int playerNumber) {
        if (StructurePrototypes.ContainsKey(id) == false) {
            return;
        }
        BuildOnTile(tiles, true, StructurePrototypes[id], playerNumber);
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
        for (int i = 0; i < loadedStructures.Count; i++) {
            BuildOnTile(loadedStructures[i], loadedStructures[i].BuildTile);
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
