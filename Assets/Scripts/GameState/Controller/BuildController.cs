﻿using Andja.Editor;
using Andja.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    /// <summary>
    /// Build state modes.
    /// </summary>
    public enum BuildStateModes { None, Build, Destroy }

    /// <summary>
    /// Build controller is responsible for placing or destroying Structures.
    /// It gives each a unique id in which order it was build.
    /// So when a savegame is loading it can place them it the correct order again.
    /// </summary>
    public class BuildController : MonoBehaviour, IBuildController {
        public static IBuildController Instance { get;  set; }
        private BuildStateModes _buildState;

        /// <summary>
        /// Is the CurrentUser doing anything with BuildController.
        /// Triggers BuildStateChange when a diffrent value is assigned.
        /// </summary>
        public BuildStateModes BuildState {
            get => _buildState;
            set {
                if (_buildState == value) {
                    return;
                }
                _buildState = value;
                _cbBuildStateChange?.Invoke(_buildState);
            }
        }

        /// <summary>
        /// Unique ID that gets assigned to a placed structure.
        /// Should start with one -- so 0 is unset
        /// </summary>
        private uint _buildId = 1;
        private string _settleStructureId = null;
        /// <summary>
        /// Currently selected by the player to preview.
        /// </summary>
        public Structure toBuildStructure;
        //Cheats for testing
        public bool noBuildCost = false;
        public bool noUnitBuildRangeRestriction = false;
        public bool allStructuresEnabled = false;

        public IReadOnlyDictionary<string, Structure> StructurePrototypes => PrototypController.Instance.StructurePrototypes;

        public List<Structure> LoadedStructures { get; private set; }
        public Dictionary<uint, Structure> BuildIdToStructure { get; private set; }
        public bool AllStructuresEnabled { 
            get => allStructuresEnabled;
            set {
                allStructuresEnabled = value;
                UI.BuildMenuUIController.Instance?.OnAllStructureEnabledCheatToggle();
            }
        }


        /// <summary>
        /// Is called when any structure is build. 
        /// </summary>
        private Action<Structure, bool> _cbStructureCreated;
        /// <summary>
        /// Is called when any structure is destroyed. 
        /// </summary>
        private Action<Structure, IWarfare> _cbAnyStructureDestroyed;
        /// <summary>
        /// Is called when any new city is created.
        /// </summary>
        private Action<ICity> _cbCityCreated;
        /// <summary>
        /// Is called when any new city is destroyed.
        /// </summary>
        private Action<ICity> _cbAnyCityDestroyed;
        /// <summary>
        /// Is called when any build state is changed.
        /// </summary>
        private Action<BuildStateModes> _cbBuildStateChange;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two BuildController.");
            }
            Instance = this;
            if (EditorController.IsEditor) {
                noBuildCost = true;
                noUnitBuildRangeRestriction = true;
            }
            else
            if (noBuildCost && noUnitBuildRangeRestriction) {
                Debug.LogWarning("Cheats are activated.");
            }
            BuildState = BuildStateModes.None;
            BuildIdToStructure = new Dictionary<uint, Structure>();
        }

        public void PlaceWorldGeneratedStructure(Dictionary<Tile, Structure> tileToStructure) {
            foreach (Tile t in tileToStructure.Keys.ToArray()) {
                if(RealBuild(new List<Tile>() { t }, 
                    tileToStructure[t], GameData.WorldNumber, false, true, null, true, true) == false) {
                    tileToStructure.Remove(t);
                }
            }
            LoadedStructures = new List<Structure>(BuildIdToStructure.Values);
        }

        public void SettleFromUnit(Unit buildUnit = null) {
            _settleStructureId ??= PrototypController.Instance.GetFirstLevelStructureIDForStructureType(typeof(WarehouseStructure));
            StartStructureBuild(_settleStructureId);
        }
        /// <summary>
        /// Destroys ALL tiles to the given Tiles if allowed. 
        /// destroyPlayer is the one trying to destroy the selected.
        /// Except if the caller isGod = cheat enabled when destroyPlayer is not null -> it destroy always
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="destroyPlayer"></param>
        /// <param name="isGod"></param>
        public void DestroyStructureOnTiles(IEnumerable<Tile> tiles, Player destroyPlayer, bool isGod = false) {
            foreach (Tile t in tiles) {
                DestroyStructureOnTile(t, destroyPlayer, isGod);
            }
        }

        /// <summary>
        /// Destroy a single structure on tile.
        /// destroyPlayer is the one trying to destroy the selected.
        /// Except if the caller isGod = cheat enabled when destroyPlayer is not null.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="destroyPlayer"></param>
        /// <param name="isGod"></param>
        public void DestroyStructureOnTile(Tile tile, Player destroyPlayer, bool isGod = false) {
            if (EditorController.IsEditor == false && WorldController.Instance.IsPaused)
                return; // Not Editor but Paused Game -> no destruction
            if (tile.Structure == null) {
                return;
            }

            if (isGod == false && tile.Structure.PlayerNumber != destroyPlayer.Number) return;

            if(tile.Structure.Demolish(isGod) == false) {
                MouseController.Instance.ShowError(MapErrorMessage.CanNotDestroy);
            }
        }

        public void SetLoadedStructures(IEnumerable<Structure> values) {
            LoadedStructures = new List<Structure>(values);
        }
        /// <summary>
        /// On loaded it will replace every structure and trigger the add callback on the city, if it is placed.
        /// </summary>
        /// <param name="loadedStructures"></param>
        public void PlaceAllLoadedStructure(List<Structure> loadedStructures) {
            this.LoadedStructures = loadedStructures;
            //order by descending because we need to go from back to front -- for removing from the last
            LoadedStructures = LoadedStructures.OrderByDescending(x => x.BuildID).ToList();
            for (int i = LoadedStructures.Count - 1; i >= 0; i--) {
                if (LoadBuildOnTile(LoadedStructures[i], World.Current.GetTileAt(LoadedStructures[i].BuildTile.Vector2))) {
                    LoadedStructures[i].City.TriggerAddCallBack(LoadedStructures[i]);
                }
                else {
                    LoadedStructures.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Change to Build State Mode for current player.
        /// structureId what is being placed. -> MouseController Mode change based on typ.
        /// Add CityTileDecider to TileSpriteController so the Player sees what is his.
        /// buildInRangeUnit if the structure is being placed "from" a unit -> inside its range.
        /// EditorStructure is for EditorController ONLY -> needed to copy selected settings.
        /// </summary>
        /// <param name="structureId"></param>
        /// <param name="EditorStructure"></param>
        public void StartStructureBuild(string structureId, Structure EditorStructure = null) {
            if (StructurePrototypes.ContainsKey(structureId) == false) {
                Debug.LogError("BUTTON has ID that is not a structure prototypes ->o_O<- ");
                return;
            }
            toBuildStructure = StructurePrototypes[structureId].Clone();
            if (EditorStructure != null)
                toBuildStructure = EditorStructure;
            MouseController.Instance.SetStructureToBuild(toBuildStructure);

            if (EditorController.IsEditor == false)
                TileSpriteController.Instance.AddDecider(TileCityDecider, true);
            BuildState = BuildStateModes.Build;
        }

        public bool CurrentPlayerBuildOnTile(List<Tile> tiles, bool forEachTileOnce, int playerNumber, bool wild = false, Unit buildInRange = null) {
            return toBuildStructure != null && BuildOnTile(toBuildStructure, tiles, playerNumber, forEachTileOnce, wild, buildInRange);
        }

        public bool BuildOnEachTile(Structure structure, List<Tile> tiles, int playerNumber) {
            return BuildOnTile(structure, tiles, playerNumber, true);
        }

        public bool BuildOnTile(Structure structure, List<Tile> tiles, int playerNumber, bool forEachTileOnce,
                                    bool wild = false, Unit buildInRange = null, bool loading = false, bool onStart = false) {
            if (tiles == null || tiles.Count == 0 || WorldController.Instance?.IsPaused == true && loading == false && onStart == false) {
                return false;
            }
            if (forEachTileOnce == false) {
                return RealBuild(tiles, structure, playerNumber, loading, wild, buildInRange, onStart);
            }
            else {
                bool allBuild = true;
                foreach (Tile tile in tiles) {
                    allBuild = RealBuild(structure.GetBuildingTiles(tile), structure, playerNumber, loading, wild, buildInRange);
                }
                return allBuild;
            }
        }

        public void EditorBuildOnTile(Structure str, Tile tile) {
            RealBuild(str.GetBuildingTiles(tile), str, GameData.WorldNumber, true, true);
        }

        public void EditorBuildOnTile(Structure toPlace, List<Tile> t) {
            RealBuild(t, toPlace, GameData.WorldNumber, false, true, null, false, true);
        }

        public bool RealBuild(List<Tile> tiles, Structure structure, int playerNumber, bool loading = false,
            bool buildInWilderness = false, Unit buildInRangeUnit = null, bool onStart = false, bool noClone = false) {
            if (tiles == null || tiles.Count == 0) {
                Debug.LogError("tiles is null or empty");
                return false;
            }
            if (buildInWilderness == false && playerNumber != -1 && PlayerController.Instance.GetPlayer(playerNumber)?.HasLost == true) {
                return false;
            }
            if (tiles.Exists(x => x == null || x.Type == TileType.Ocean)) {
                return false;
            }
            tiles = tiles.OrderBy(x => x.Y).ThenBy(x => x.X).ToList();
            int rotate = structure.Rotation;
            if (loading == false && noClone == false) {
                structure = structure.Clone();
            }
            if (buildInRangeUnit != null && noUnitBuildRangeRestriction == false) {
                Vector3 unitPos = buildInRangeUnit.PositionVector2;
                Tile t = tiles.Find(x => x.IsInRange(unitPos, buildInRangeUnit.BuildRange));
                if (t == null) {
                    BuildError(MapErrorMessage.NotInRange, tiles, structure, playerNumber);
                    return false;
                }
            }
            //FIXME find a better solution for this?
            structure.ChangeRotation(rotate);
            //if is build in wilderniss city
            structure.buildInWilderness = buildInWilderness;
            if (loading) {
                if (structure.City != null)
                    playerNumber = structure.City.PlayerNumber;
                List<Tile> temp = new List<Tile>(tiles);
                //remove wilderniss
                temp.RemoveAll(x => x.City.IsWilderness());
                if (temp.Count == 0) {
                    //non over cities? -> place it there
                    buildInWilderness = true;
                    playerNumber = -1;
                }
                else {
                    //check if it is in a diffrent city -- and add it to that one
                    buildInWilderness = true; //so it doesnt check for incity
                                              //check first the current city owner
                    int currentMaxCityTilesCount = temp.RemoveAll(x => x.City.PlayerNumber == playerNumber);
                    while (temp.Count > 0) {
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
                        BuildError(MapErrorMessage.NotInCity, tiles, structure, playerNumber);
                        return false;
                    }
                    structure.City = tiles[0].Island.Cities.Find(x => x?.PlayerNumber == playerNumber);
                }
                else
                    structure.City = tiles[0].City;
            }

            //before we need to check if we can build THERE
            //we need to know if there is if we COULD build
            //it anyway? that means enough resources and enough Money
            Inventory inv = null;
            if (loading == false && buildInWilderness == false) {
                if(tiles[0].Island.HasNegativeEffect) {
                    BuildError(MapErrorMessage.CanNotBuildHere, tiles, structure, playerNumber);
                    return false;
                }
                //find a city that matches the player
                //and check for money
                if (PlayerHasEnoughMoney(structure, playerNumber) == false && noBuildCost == false && onStart == false) {
                    BuildError(MapErrorMessage.NotEnoughMoney, tiles, structure, playerNumber);
                    return false;
                }
                if (structure.City == null && structure.GetType() != typeof(WarehouseStructure)) {
                    return false; // SO no city found and no warehouse to create on
                }
                if (noBuildCost == false && onStart == false) {
                    if (structure.BuildingItems != null) {
                        if (buildInRangeUnit != null) {
                            inv = buildInRangeUnit.Inventory;
                        }
                        else {
                            if (structure.City != null)
                                inv = structure.City.Inventory;
                        }
                        if (inv == null) {
                            Debug.LogError("Build something with smth that has no Inventory");
                            return false;
                        }
                        if (inv.HasEnoughOfItems(structure.BuildingItems) == false) {
                            BuildError(MapErrorMessage.NotEnoughResources, tiles, structure, playerNumber);
                            return false;
                        }
                    }
                }
            }
            bool isUpgrade = false;
            if (tiles.All(x => x.Structure != null
                         && x.Structure.CanBeUpgradedTo != null
                         && Array.Exists(x.Structure.CanBeUpgradedTo, x => x == structure.ID))) {
                
                //We can upgrade the building instead
                tiles[0].Structure.UpgradeTo(structure.ID);
                isUpgrade = true;
            } else
            //now we know that we COULD build that structure
            //but CAN WE?
            //check to see if the structure can be placed there
            if (structure.CheckPlaceStructure(tiles, playerNumber) == false) {
                if (loading == false || EditorController.IsEditor) return false;
                Debug.LogWarning("PLACING FAILED WHILE LOADING! " + structure.BuildID + " - " + structure.SmallName);
                structure.Destroy(null, true);
                return false;
            } else
            //WE ARE HERE -- MEANS ALL CHECKS ARE DONE
            //IT WILL BE BUILD!
            //ALLOWS CREATION OF CITY when warehouse
            if (structure is WarehouseStructure) {
                if(structure.City == null && tiles[0].Island.FindCityByPlayer(playerNumber) == null) {
                    structure.City = CreateCity(tiles[0].Island, playerNumber);
                }
            }

            //pay for it -- if not otherwise disabled
            if (noBuildCost == false && onStart == false && buildInWilderness == false && loading == false) {
                if (structure.BuildingItems != null)
                    inv.RemoveItemsAmount(structure.BuildingItems);
                PlayerController.Instance.GetPlayer(playerNumber).ReduceTreasure(structure.BuildCost);
            }

            if(isUpgrade) {
                //if it is just an upgrade return here it is done
                return true;
            }

            structure.PlaceStructure(tiles, loading);
            structure.City.AddStructure(structure);

            if (onStart) {
                LoadedStructures ??= new List<Structure>();
                LoadedStructures.Add(structure);
            }

            if(loading) {
                if(BuildIdToStructure.ContainsKey(structure.BuildID)) {
                    Debug.LogError("Build ID duplicate found: " + BuildIdToStructure[structure.BuildID] + " " + structure);
                    structure.Destroy();
                    return false;
                }
            }
             
            if(structure.BuildID == 0) {
                structure.BuildID = _buildId;
                _buildId++;
            } else {
                _buildId = Math.Max(_buildId, structure.BuildID) + 1;
            }
            BuildIdToStructure[structure.BuildID] = structure;

            _cbStructureCreated?.Invoke(structure, loading);
            structure.RegisterOnDestroyCallback(OnStructureDestroy);
            return true;
        }

        public void BuildError(MapErrorMessage errorID, List<Tile> tiles, Structure structure, int playerNumber) {
            if (PlayerController.currentPlayerNumber != playerNumber) return;
            Vector3 position = tiles[0].Vector;
            position.x += structure.TileWidth / 2f;
            position.y += structure.TileHeight / 2f;
            MouseController.Instance.ShowError(errorID, position);
        }

        /// <summary>
        /// USED ONLY FOR LOADING
        /// DONT USE THIS FOR ANYTHING ELSE!!
        /// </summary>
        public bool LoadBuildOnTile(Structure s, Tile t) {
            if (s != null && t != null) 
                return RealBuild(s.GetBuildingTiles(t), s, -1, true, s.buildInWilderness);
            Debug.LogError("Something went wrong by loading Structure! " + t + " " + s);
            return false;
        }

        public void OnStructureDestroy(Structure str, IWarfare destroyer) {
            _cbAnyStructureDestroyed?.Invoke(str, destroyer);
            BuildIdToStructure.Remove(str.BuildID);
        }

        public bool PlayerHasEnoughMoney(Structure s, int playerNumber) {
            return PlayerController.Instance.GetPlayer(playerNumber).TreasuryBalance >= s.BuildCost;
        }

        public ICity CreateCity(IIsland i, int playernumber) {
            if (i == null) {
                Debug.LogError("CreateCity called not on a island!");
                return null;
            }
            ICity c = i.CreateCity(playernumber);
            c.RegisterCityDestroy(_cbAnyCityDestroyed);
            // needed for mapimage
            _cbCityCreated?.Invoke(c);
            return c;
        }
        /// <summary>
        /// Reset the BuildState & toBuildStructure to none and reset mousecontroller aswell.
        /// Removes TileCityDecider from TileSpriteController
        /// </summary>
        public void ResetBuild() {
            BuildState = BuildStateModes.None;
            if (MouseController.Instance.MouseState != MouseState.Idle) {
                //Reset MouseController out of BuildMode that this set it to
                //when a structure gets selected
                MouseController.Instance.SetMouseState(MouseState.Idle);
                MouseController.Instance.ResetBuild();
            }
            TileSpriteController.Instance.RemoveDecider(TileCityDecider, true);
            this.toBuildStructure = null;
        }

        /// <summary>
        /// Destroy Tool resets Build and changes the buildState to destroy.
        /// Notifys MouseController and adds the TileCityDecider to TileSpriteController
        /// </summary>
        public void DestroyToolSelect() {
            ResetBuild();
            BuildState = BuildStateModes.Destroy;
            MouseController.Instance.SetMouseState(MouseState.Destroy);
            TileSpriteController.Instance.AddDecider(TileCityDecider, true);
        }

        public void Escape() {
            ResetBuild();
        }
        /// <summary>
        /// Callback called on every structure build
        /// </summary>
        /// <param name="callbackfunc"></param>
        public void RegisterStructureCreated(Action<Structure, bool> callbackfunc) {
            _cbStructureCreated += callbackfunc;
        }

        public void UnregisterStructureCreated(Action<Structure, bool> callbackfunc) {
            _cbStructureCreated -= callbackfunc;
        }
        /// <summary>
        /// Callback called on every structure destroyed
        /// </summary>
        /// <param name="callbackfunc"></param>
        public void RegisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc) {
            _cbAnyStructureDestroyed += callbackfunc;
        }

        public void UnregisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc) {
            _cbAnyStructureDestroyed -= callbackfunc;
        }
        /// <summary>
        /// Callback called on every City created
        /// </summary>
        /// <param name="callbackfunc"></param>
        public void RegisterCityCreated(Action<ICity> callbackfunc) {
            _cbCityCreated += callbackfunc;
        }

        public void UnregisterCityCreated(Action<ICity> callbackfunc) {
            _cbCityCreated -= callbackfunc;
        }
        /// <summary>
        /// Callback called on every City destroyed
        /// </summary>
        /// <param name="callbackfunc"></param>
        public void RegisterAnyCityDestroyed(Action<ICity> callbackfunc) {
            _cbAnyCityDestroyed += callbackfunc;
        }

        public void UnregisterAnyCityDestroyed(Action<ICity> callbackfunc) {
            _cbAnyCityDestroyed -= callbackfunc;
        }
        /// <summary>
        /// Callback called on new BuildStateModes change
        /// </summary>
        /// <param name="callbackfunc"></param>
        public void RegisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
            _cbBuildStateChange += callbackfunc;
        }

        public void UnregisterBuildStateChange(Action<BuildStateModes> callbackfunc) {
            _cbBuildStateChange -= callbackfunc;
        }

        public void OnDestroy() {
            Instance = null;
        }

        public TileMark TileCityDecider(Tile t) {
            if (t == null) {
                return TileMark.None;
            }
            else if (t.City != null && t.City.IsCurrentPlayerCity()) {
                return TileMark.None;
            }
            else {
                return TileMark.Dark;
            }
        }

        public void RotateBuildStructure() {
            toBuildStructure?.Rotate();
        }

        public void ToggleBuildCost() {
            noBuildCost = !noBuildCost;
        }

        public void ToggleUnitBuildRangeRestriction() {
            noUnitBuildRangeRestriction = !noUnitBuildRangeRestriction;
        }
    }
}