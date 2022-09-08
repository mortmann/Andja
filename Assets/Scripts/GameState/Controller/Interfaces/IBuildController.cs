using System;
using System.Collections.Generic;
using Andja.Model;

namespace Andja.Controller {
    public interface IBuildController {
        /// <summary>
        /// Is the CurrentUser doing anything with BuildController.
        /// Triggers BuildStateChange when a diffrent value is assigned.
        /// </summary>
        BuildStateModes BuildState { get; set; }

        bool AllStructuresEnabled { get; set; }
        IReadOnlyDictionary<string, Structure> StructurePrototypes { get; }
        List<Structure> LoadedStructures { get; }
        Dictionary<uint, Structure> BuildIdToStructure { get; }
        void Awake();
        void PlaceWorldGeneratedStructure(Dictionary<Tile, Structure> tileToStructure);
        void SettleFromUnit(Unit buildUnit = null);

        /// <summary>
        /// Destroys ALL tiles to the given Tiles if allowed. 
        /// destroyPlayer is the one trying to destroy the selected.
        /// Except if the caller isGod = cheat enabled when destroyPlayer is not null -> it destroy always
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="destroyPlayer"></param>
        /// <param name="isGod"></param>
        void DestroyStructureOnTiles(IEnumerable<Tile> tiles, Player destroyPlayer, bool isGod = false);

        /// <summary>
        /// Destroy a single structure on tile.
        /// destroyPlayer is the one trying to destroy the selected.
        /// Except if the caller isGod = cheat enabled when destroyPlayer is not null.
        /// </summary>
        /// <param name="tile"></param>
        /// <param name="destroyPlayer"></param>
        /// <param name="isGod"></param>
        void DestroyStructureOnTile(Tile tile, Player destroyPlayer, bool isGod = false);

        void SetLoadedStructures(IEnumerable<Structure> values);

        /// <summary>
        /// On loaded it will replace every structure and trigger the add callback on the city, if it is placed.
        /// </summary>
        /// <param name="loadedStructures"></param>
        void PlaceAllLoadedStructure(List<Structure> loadedStructures);

        /// <summary>
        /// Change to Build State Mode for current player.
        /// structureId what is being placed. -> MouseController Mode change based on typ.
        /// Add CityTileDecider to TileSpriteController so the Player sees what is his.
        /// buildInRangeUnit if the structure is being placed "from" a unit -> inside its range.
        /// EditorStructure is for EditorController ONLY -> needed to copy selected settings.
        /// </summary>
        /// <param name="structureId"></param>
        /// <param name="EditorStructure"></param>
        void StartStructureBuild(string structureId, Structure EditorStructure = null);

        bool CurrentPlayerBuildOnTile(List<Tile> tiles, bool forEachTileOnce, int playerNumber, bool wild = false, Unit buildInRange = null);
        bool BuildOnEachTile(Structure structure, List<Tile> tiles, int playerNumber);

        bool BuildOnTile(Structure structure, List<Tile> tiles, int playerNumber, bool forEachTileOnce,
            bool wild = false, Unit buildInRange = null, bool loading = false, bool onStart = false);

        void EditorBuildOnTile(Structure str, Tile tile);
        void EditorBuildOnTile(Structure toPlace, List<Tile> t);

        bool RealBuild(List<Tile> tiles, Structure structure, int playerNumber, bool loading = false,
            bool buildInWilderness = false, Unit buildInRangeUnit = null, bool onStart = false, bool noClone = false);

        void BuildError(MapErrorMessage errorID, List<Tile> tiles, Structure structure, int playerNumber);

        /// <summary>
        /// USED ONLY FOR LOADING
        /// DONT USE THIS FOR ANYTHING ELSE!!
        /// </summary>
        bool LoadBuildOnTile(Structure s, Tile t);

        void OnStructureDestroy(Structure str, IWarfare destroyer);
        bool PlayerHasEnoughMoney(Structure s, int playerNumber);
        ICity CreateCity(IIsland i, int playernumber);

        /// <summary>
        /// Reset the BuildState & toBuildStructure to none and reset mousecontroller aswell.
        /// Removes TileCityDecider from TileSpriteController
        /// </summary>
        void ResetBuild();

        /// <summary>
        /// Destroy Tool resets Build and changes the buildState to destroy.
        /// Notifys MouseController and adds the TileCityDecider to TileSpriteController
        /// </summary>
        void DestroyToolSelect();

        void Escape();

        /// <summary>
        /// Callback called on every structure build
        /// </summary>
        /// <param name="callbackfunc"></param>
        void RegisterStructureCreated(Action<Structure, bool> callbackfunc);

        void UnregisterStructureCreated(Action<Structure, bool> callbackfunc);

        /// <summary>
        /// Callback called on every structure destroyed
        /// </summary>
        /// <param name="callbackfunc"></param>
        void RegisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc);

        void UnregisterStructureDestroyed(Action<Structure, IWarfare> callbackfunc);

        /// <summary>
        /// Callback called on every City created
        /// </summary>
        /// <param name="callbackfunc"></param>
        void RegisterCityCreated(Action<ICity> callbackfunc);

        void UnregisterCityCreated(Action<ICity> callbackfunc);

        /// <summary>
        /// Callback called on every City destroyed
        /// </summary>
        /// <param name="callbackfunc"></param>
        void RegisterAnyCityDestroyed(Action<ICity> callbackfunc);

        void UnregisterAnyCityDestroyed(Action<ICity> callbackfunc);

        /// <summary>
        /// Callback called on new BuildStateModes change
        /// </summary>
        /// <param name="callbackfunc"></param>
        void RegisterBuildStateChange(Action<BuildStateModes> callbackfunc);

        void UnregisterBuildStateChange(Action<BuildStateModes> callbackfunc);
        void OnDestroy();
        TileMark TileCityDecider(Tile t);
        void RotateBuildStructure();
        void ToggleBuildCost();
        void ToggleUnitBuildRangeRestriction();
    }
}