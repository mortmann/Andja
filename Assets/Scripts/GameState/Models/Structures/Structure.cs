using Andja.Controller;
using Andja.Editor;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Andja.Model {

    public enum BuildType { Drag, Path, Single };

    public enum StructureTyp { Pathfinding, Blocking, Free };

    public enum Direction { N, E, S, W, None };

    public enum ExtraUI { None, Range, Upgrade, Efficiency };

    public enum ExtraBuildUI { None, Range, Efficiency };

    public enum BuildRestriktions { Land, Shore, Mountain };

    public class StructurePrototypeData : BaseThingData {

        public int structureRange = 0;
        public int tileWidth;
        public int tileHeight;
        public bool canRotate = true;
        public bool canBeBuildOver = false;
        public bool hasHitbox;// { get; protected set; }
        /// <summary>
        /// Null means no restrikiton so all buildable tiles
        /// </summary>
        public TileType?[,] buildTileTypes;
        public string[] canBeUpgradedTo;

        [Ignore]
        private List<Tile> _prototypeRangeTiles;
        public List<Tile> PrototypeRangeTiles =>
            _prototypeRangeTiles ??= Util.CalculateRangeTiles(structureRange, tileWidth, tileHeight);

        [Ignore]
        Dictionary<TileType, int> buildTileTypesToMinLength;
        public Dictionary<TileType, int> BuildTileTypesToMinLength {
            get {
                if (buildTileTypes == null)
                    return null;
                if (buildTileTypesToMinLength != null)
                    return buildTileTypesToMinLength;
                var temp = new Dictionary<TileType, int>();
                for (int x = 0; x < buildTileTypes.GetLength(0); x++) {
                    for (int y = 0; y < buildTileTypes.GetLength(1); y++) {
                        if(buildTileTypes[x, y] == null) continue;
                        if (temp.ContainsKey(buildTileTypes[x, y].Value) == false) {
                            temp[buildTileTypes[x, y].Value] = 1;
                        }
                        if (x <= 0) continue;
                        if (buildTileTypes[x - 1, y] == buildTileTypes[x, y]) {
                            temp[buildTileTypes[x, y].Value]++;
                        }
                    }
                }
                buildTileTypesToMinLength = new Dictionary<TileType, int>();
                for (int x = 0; x < buildTileTypes.GetLength(0); x++) {
                    for (int y = 0; y < buildTileTypes.GetLength(1); y++) {
                        if (buildTileTypes[x, y] == null) continue;
                        if (buildTileTypesToMinLength.ContainsKey(buildTileTypes[x, y].Value) == false) {
                            buildTileTypesToMinLength[buildTileTypes[x, y].Value] = 1;
                        }
                        if (y <= 0) continue;
                        if (buildTileTypes[x, y - 1] == buildTileTypes[x, y]) {
                            buildTileTypesToMinLength[buildTileTypes[x, y].Value]++;
                        }
                    }
                }
                foreach (var item in temp) {
                    if(buildTileTypesToMinLength.ContainsKey(item.Key) == false) {
                        buildTileTypesToMinLength[item.Key] = item.Value;
                    } else {
                        if (item.Value > buildTileTypesToMinLength[item.Key]) {
                            buildTileTypesToMinLength[item.Key] = item.Value;
                        }
                    }
                }
                return buildTileTypesToMinLength;
            }
        }

        [Ignore]
        protected int rangeTileCount = -1;

        public int RangeTileCount {
            get {
                if (rangeTileCount < 0)
                    rangeTileCount = Util.CalculateRangeTilesVector2(structureRange, tileWidth, tileHeight).Count;
                return rangeTileCount;
            }
        }

        public ExtraUI extraUITyp;
        public StructureTyp structureTyp = StructureTyp.Blocking;
        public bool canStartBurning;
        public bool canBeBuild = true;
        public BuildType buildTyp;
        public ExtraBuildUI extraBuildUITyp;

        public Dictionary<Climate, string[]> climateSpriteModifier;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Structure : BaseThing {
        public const string InactiveEffectID = "inactive";

        #region variables

        #region Serialize

        //build id -- when it was build
        [JsonPropertyAttribute] public uint BuildID;
        [JsonPropertyAttribute]
        public LandTile BuildTile {
            get {
                if (Tiles == null || Tiles.Count == 0)
                    return null;
                return (LandTile)Tiles[0];
            }
            set {
                Tiles ??= new List<Tile>();
                Tiles.Add(value);
            }
        }
        [JsonPropertyAttribute] protected int rotation = 0;
        [JsonPropertyAttribute] public bool buildInWilderness = false;
        [JsonPropertyAttribute] protected bool isActive = true;
        [JsonPropertyAttribute] protected string spriteVariant;
        #endregion Serialize

        #region RuntimeOrOther

        public List<Tile> Tiles;
        public HashSet<Tile> NeighbourTiles;
        protected ICity city;
        public HashSet<Tile> RangeTiles;
        public string connectOrientation;
        public bool HasExtraUI => ExtraUITyp != ExtraUI.None; 

        //player id
        public int PlayerNumber {
            get {
                if (City == null) {
                    return -1;
                }
                return City.GetPlayerNumber();
            }
        }

        protected StructurePrototypeData prototypeData;

        public StructurePrototypeData Data => prototypeData ??= PrototypController.Instance.GetStructurePrototypDataForID(ID);

        private Vector2 _center;

        public Vector2 Center {
            get {
                if (_center != Vector2.zero)
                    return _center;
                _center = Tiles[0].Vector2 + new Vector2(TileWidth / 2f, TileHeight / 2f);
                return _center;
            }
        }

        public bool CanBeBuild => Data.canBeBuild; 
        public bool IsWalkable => this.StructureTyp != StructureTyp.Blocking; 
        public bool HasHitbox => Data.hasHitbox; 

        #region EffectVariables

        public int StructureRange => CalculateRealValue(nameof(Data.structureRange), Data.structureRange); 

        #endregion EffectVariables

        public string Name => Data.Name; 
        public string Description => Data.Description; 
        public string ToolTip => Data.HoverOver; 
        public int PrototypeTileWidth => Data.tileWidth; 
        public int PrototypeTileHeight => Data.tileHeight; 
        public TileType?[,] BuildTileTypes => Data.buildTileTypes;

        public bool CanRotate => Data.canRotate; 
        public bool CanBeBuildOver => Data.canBeBuildOver;

        public string[] CanBeUpgradedTo => Data.canBeUpgradedTo;
        public virtual bool CanBeUpgraded => CanBeUpgradedTo != null && CanBeUpgradedTo.Length > 0; 

        public BuildType BuildTyp => Data.buildTyp; 
        public StructureTyp StructureTyp => Data.structureTyp; 
        public ExtraUI ExtraUITyp => Data.extraUITyp; 
        public ExtraBuildUI ExtraBuildUITyp => Data.extraBuildUITyp; 

        public List<Tile> PrototypeTiles => Data.PrototypeRangeTiles; 

        public bool CanStartBurning => Data.canStartBurning; 

        public Dictionary<Climate, string[]> ClimateSpriteModifier => Data.climateSpriteModifier;
        protected Action<Structure> cbStructureChanged;
        protected Action<Structure, IWarfare> cbStructureDestroy;
        protected Action<Structure, bool> cbStructureExtraUI;
        protected Action<Structure, string, bool> cbStructureSound;
        protected Action<Structure, ICity, ICity> cbOwnerChange;
        protected Action<Structure> cbRoutesChanged;

        protected HashSet<Route> Routes = new HashSet<Route>();
        protected List<RoadStructure> Roads = new();

        protected void BaseCopyData(Structure str) {
            ID = str.ID;
            prototypeData = str.Data;
        }

        #endregion RuntimeOrOther

        #endregion variables

        #region Properties
        public Vector2 Size => new Vector2(TileWidth, TileHeight);
        public virtual bool IsActive => isActive;
        public virtual bool IsActiveAndWorking => isActive;

        public string SmallName => SpriteName.ToLower(); 

        public ICity City {
            get => city;
            set {
                if (city != null && city != value) {
                    cbOwnerChange?.Invoke(this, city, value);
                    city.RemoveStructure(this);
                }
                city = value;
            }
        }

        public bool NeedsRepair => CurrentHealth < MaximumHealth;

        public int TileWidth {
            get {
                switch (Rotation) {
                    case 0:
                    case 180:
                        return PrototypeTileWidth;
                    case 90:
                    case 270:
                        return PrototypeTileHeight;
                    default:
                        // should never come to this if its an error
                        Debug.LogError("Structure was rotated out of angle bounds: " + Rotation);
                        return 0;
                }
            }
        }

        public int TileHeight {
            get {
                switch (Rotation) {
                    case 0:
                    case 180:
                        return PrototypeTileHeight;
                    case 90:
                    case 270:
                        return PrototypeTileWidth;
                    default:
                        // should never come to this if its an error
                        Debug.LogError("Structure was rotated out of angle bounds: " + Rotation);
                        return 0;
                }
            }
        }
        #endregion Properties

        #region Virtual/Abstract

        public virtual string SortingLayer => "Structures";

        public int Rotation {
            get => rotation;
            protected set => rotation = value;
        }

        public virtual void OpenExtraUI() {
            cbStructureExtraUI?.Invoke(this, true);
        }

        public virtual void CloseExtraUI() {
            cbStructureExtraUI?.Invoke(this, false);
        }
        public abstract Structure Clone();

        public virtual void OnDestroy() {
        }

        /// <summary>
        /// Extra Build UI for showing stuff when building
        /// structures. Or so.
        /// </summary>
        public virtual object GetExtraBuildUIData() {
            //does nothing normally
            //stuff here to show for when building this
            //using this for e.g. farm efficiency bar!
            return null;
        }

        public virtual void UpdateExtraBuildUI(GameObject parent, Tile t) {
            //does nothing normally
            //stuff here to show for when building this
            //using this for e.g. farm efficiency bar!
        }

        public virtual void OnEventCreateVirtual(GameEvent ge) {
            ge.EffectTarget(this, true);
        }

        public virtual void OnEventEndedVirtual(GameEvent ge) {
            ge.EffectTarget(this, false);
        }

        public virtual void Load() {
        }

        public virtual string GetSpriteName() {
            return spriteVariant == null ? SpriteName : SpriteName + "_" + spriteVariant;
        }

        #endregion Virtual/Abstract

        #region callbacks

        public void CallbackChangeIfNotNull() {
            cbStructureChanged?.Invoke(this);
        }

        public void RegisterOnChangedCallback(Action<Structure> cb) {
            cbStructureChanged += cb;
        }

        public void UnregisterOnChangedCallback(Action<Structure> cb) {
            cbStructureChanged -= cb;
        }

        public void RegisterOnDestroyCallback(Action<Structure, IWarfare> cb) {
            cbStructureDestroy += cb;
        }

        public void UnregisterOnDestroyCallback(Action<Structure, IWarfare> cb) {
            cbStructureDestroy -= cb;
        }

        public void RegisterOnSoundCallback(Action<Structure, string, bool> cb) {
            cbStructureSound += cb;
        }

        public void UnregisterOnSoundCallback(Action<Structure, string, bool> cb) {
            cbStructureSound -= cb;
        }

        public void RegisterOnExtraUICallback(Action<Structure, bool> cb) {
            cbStructureExtraUI += cb;
        }

        public void UnregisterOnExtraUICallback(Action<Structure, bool> cb) {
            cbStructureExtraUI -= cb;
        }
        public void RegisterOnRoutesChangedCallback(Action<Structure> cb) {
            cbRoutesChanged += cb;
        }

        public void UnregisterOnRoutesChangedCallback(Action<Structure> cb) {
            cbRoutesChanged -= cb;
        }
        /// <summary>
        /// 1st Structure (Changed)
        /// 2st OldCity (Owner)
        /// 3st NewCity (Owner)
        /// </summary>
        /// <param name="cb"></param>
        public void RegisterOnOwnerChange(Action<Structure, ICity, ICity> cb) {
            cbOwnerChange += cb;
        }

        public void UnregisterOnOwnerChange(Action<Structure, ICity, ICity> cb) {
            cbOwnerChange -= cb;
        }

        #endregion callbacks

        #region placestructure

        public bool CheckPlaceStructure(List<Tile> tiles, int playerNumber/*only for error sending*/) {
            if (tiles.Count == 0 || tiles.Contains(null)) {
#if !UNITY_INCLUDE_TESTS
                Debug.LogError("PlaceStructure FAILED -- tiles is empty or contains null tile!");
#endif
                return false;
            }
            //test if the place is buildable
            // if it has to be on land
            if (CanBuildOnSpot(tiles) == false) {
                BuildController.Instance.BuildError(MapErrorMessage.NoSpace, tiles, this, playerNumber);
                return false;
            }
            //special check for some structures
            if (SpecialCheckForBuild(tiles) == false) {
                BuildController.Instance.BuildError(MapErrorMessage.CanNotBuildHere, tiles, this, playerNumber);
                return false;
            }
            return true;
        }

        public virtual bool InCityCheck(IEnumerable<Tile> tiles, int playerNumber) {
            return tiles.Count(x => x.City?.PlayerNumber == playerNumber) >= tiles.Count() * GameData.nonCityTilesPercentage;
        }

        public void PlaceStructure(List<Tile> tiles, bool loading) {
            Tiles = new List<Tile>();
            Tiles.AddRange(tiles);
            if (loading == false) {
                CurrentHealth = MaximumHealth;
                DecideClimateSprite();
            }
            //if we are here we can build this and
            //set the tiles to the this structure -> claim the tiles!
            CalculateNeighbourTiles();
            //it searches all the tiles it has in its reach!
            RangeTiles = GetInRangeTiles(Tiles[0]);
            // do on place structure stuff here!
            OnBaseThingBuild(loading);
            City.RegisterOnEvent(OnEventCreate, OnEventEnded);
        }

        protected void CalculateNeighbourTiles() {
            NeighbourTiles = new HashSet<Tile>();
            foreach (Tile mt in Tiles) {
                mt.Structure = this;
                if (EditorController.IsEditor) continue;
                foreach (Tile nbt in mt.GetNeighbours()) {
                    if (Tiles.Contains(nbt) == false) {
                        NeighbourTiles.Add(nbt);
                    }

                    if (nbt.Structure is RoadStructure road) {
                        AddRoadStructure(road);
                    }
                }
            }
        }

        protected void DecideClimateSprite() {
            if (Data.climateSpriteModifier == null || Data.climateSpriteModifier.Count <= 0) return;
            Climate c = BuildTile.Island.Climate;
            if (ClimateSpriteModifier.ContainsKey(c) == false) return;
            spriteVariant = ClimateSpriteModifier[c][Random.Range(0, ClimateSpriteModifier[c].Length)];
            spriteVariant += StructureSpriteController.GetRandomVariant(ID, spriteVariant);
        }

        public static bool IsTileCityViable(Tile t, int player) {
            if (t.City == null)
                return false;
            return t.City.IsWilderness() || t.City.PlayerNumber == player;
        }

        public virtual bool SpecialCheckForBuild(List<Tile> tiles) {
            return true;
        }

        #endregion placestructure

        #region igeventable

        /// <summary>
        /// Do not override this function!
        /// USE virtual to override the reaction to an event that
        /// influences this Structure.
        /// </summary>
        /// <param name="ge">Ge.</param>
        public override void OnEventCreate(GameEvent ge) {
            //every subtype has do decide what todo
            //maybe some above reactions here
            if (ge.target is Structure) {
                if (ge.target == this) {
                    OnEventCreateVirtual(ge);
                }
                return;
            }
            if (ge.IsTarget(this)) {
                OnEventCreateVirtual(ge);
            }
        }

        /// <summary>
        /// Do not override this function!
        /// USE virtual to override the reaction to an event that
        /// influences this Structure.
        /// </summary>
        /// <param name="ge">Ge.</param>
        public override void OnEventEnded(GameEvent ge) {
            //every subtype has do decide what todo
            //maybe some above reactions here
            if (ge.target is Structure) {
                if (ge.target == this) {
                    OnEventEndedVirtual(ge);
                }
                return;
            }
            if (ge.IsTarget(this)) {
                OnEventEndedVirtual(ge);
            }
        }

        public void UpgradeTo(string ID) {
            this.ID = ID;
            OnUpgrade();
            cbStructureChanged?.Invoke(this);
        }

        protected virtual void OnUpgrade() {
            prototypeData = null;
        }

        protected override void AddSpecialEffect(Effect effect) {
            Debug.Log("NO Special Effect handeld! " + effect);
        }

        protected override void RemoveSpecialEffect(Effect effect) {
            Debug.Log("NO Special Effect handeld! " + effect);
        }

        public override int GetPlayerNumber() {
            return PlayerNumber;
        }

        public override string GetID() {
            return ID;
        } // only needs to get changed WHEN there is diffrent ids

        #endregion igeventable

        #region List<Tile>

        /// <summary>
        /// x/y = position of tile
        /// left = true = start tile is on the top right going to bottom left -> used by bots
        /// left = false = start tile is on the bottom left going to top right
        /// ignoreRotation = takes structure how it was designed
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="ignoreRotation"></param>
        /// <param name="left"></param>
        /// <returns></returns>
        public List<Tile> GetBuildingTiles(Tile tile, bool ignoreRotation = false) {
            List<Tile> tiles = new List<Tile>();
            if (ignoreRotation == false) {
                for (int w = 0; w < TileWidth; w++) {
                    for (int h = 0; h < TileHeight; h++) {
                        tiles.Add(World.Current.GetTileAt(tile.X + w, tile.Y + h));
                    }
                }
            }
            else {
                for (int w = 0; w < PrototypeTileWidth; w++) {
                    tiles.Add(World.Current.GetTileAt(tile.X + w, tile.Y));
                    for (int h = 1; h < PrototypeTileHeight; h++) {
                        tiles.Add(World.Current.GetTileAt(tile.X + w, tile.Y + h));
                    }
                }
            }
            return tiles;
        }

        /// <summary>
        /// Gets the in range tiles.
        /// </summary>
        /// <returns>The in range tiles.</returns>
        /// <param name="firstTile">The most left one, first row.</param>
        public HashSet<Tile> GetInRangeTiles(Tile firstTile) {
            if (StructureRange == 0) {
                return null;
            }
            if (firstTile == null) {
                Debug.LogError("Range Tiles Tile is null -> cant calculated of that");
                return null;
            }
            RangeTiles = new HashSet<Tile>();
            float width = firstTile.X - StructureRange;
            float height = firstTile.Y - StructureRange;
            foreach (Tile t in Util.CalculateRangeTiles(StructureRange, TileWidth, TileHeight)) {
                RangeTiles.Add(World.Current.GetTileAt(t.X + width, t.Y + height));
            }
            return RangeTiles;
        }

        public List<RoadStructure> RoadsAroundStructure() {
            return Roads;
        }

        public HashSet<Route> GetRoutes() {
            return Routes;
        }

        #endregion List<Tile>

        #region Functions

        public List<Structure> GetNeighbourStructuresInTileDistance(int spreadTileRange) {
            Vector2 lower = Center - new Vector2(TileWidth / 2 + spreadTileRange, TileHeight / 2 + spreadTileRange);
            Vector2 upper = Center + new Vector2(TileWidth / 2 + spreadTileRange, TileHeight / 2 + spreadTileRange);
            List<Structure> structures = new List<Structure>();
            for (float x = lower.x; x <= upper.x; x++) {
                for (float y = lower.y; y <= upper.y; y++) {
                    Tile t = World.Current.GetTileAt(x, y);
                    if (t.Structure == null || t.Structure == this) {
                        continue;
                    }
                    structures.Add(t.Structure);
                }
            }
            return structures;
        }

        public bool Demolish(bool isGod = false) {
            if (HasNegativeEffect && isGod == false)
                return false; // we cannot just destroy structures that have a negative effect e.g. burning or illness or similar
            if (GameData.ReturnResources) {
                //If return resources is on. 
                //then added those to the city
                Item[] res = BuildingItems;
                for (int i = 0; i < res.Length; i++) {
                    res[i].count = Mathf.RoundToInt(res[i].count * GameData.ReturnResourcesPercentage); 
                }
                City.Inventory.AddItems(res);
            }
            return Destroy();
        }
        /// <summary>
        /// Destroys this structure immedietly and without any further checks. 
        /// For playerside destruction please call demolish.
        /// </summary>
        /// <param name="destroyer"></param>
        /// <param name="onLoad"></param>
        /// <returns></returns>
        protected override bool OnDestroy(IWarfare destroyer = null, bool onLoad = false) {
            currentHealth = 0;
            City.RemoveStructure(this);
            cbStructureDestroy?.Invoke(this, destroyer);
            if (onLoad == false) {
                foreach (Tile t in Tiles) {
                    t.Structure = null;
                }
            }
            OnDestroy();
            //TODO: add here for getting res back when destroyer = null? negative effect?
            return true;
        }

        public bool CanReachStructure(Structure s) {
            HashSet<Route> otherRoutes = s.GetRoutes();
            foreach (Route route in GetRoutes()) {
                if (otherRoutes.Contains(route))
                    return true;
            }
            return false;
        }

        #endregion Functions

        #region correctspot

        public bool CanBuildOnSpot(List<Tile> tiles) {
            return new List<bool>(CheckForCorrectSpot(tiles).Values).Contains(false) == false;
        }

        public Dictionary<Tile, bool> CheckForCorrectSpot(List<Tile> tiles) {
            if (tiles.Count == 0)
                return null;
            Dictionary<Tile, bool> tileToCanBuild = new Dictionary<Tile, bool>();
            //to make it faster
            if (BuildTileTypes == null) {
                foreach (Tile item in tiles) {
                    tileToCanBuild.Add(item, item.CheckTile(this));
                }
                return tileToCanBuild;
            }

            //TO simplify this we are gonna sort the array so it is in order
            //from the coordinationsystem that means 0,0->width,height
            Tile[,] sortedTiles = new Tile[TileWidth, TileHeight];
            tiles = tiles.OrderBy(x => x.X).ThenBy(x => x.Y).ToList();
            foreach (Tile t in tiles) {
                int x = t.X - tiles[0].X;
                int y = t.Y - tiles[0].Y;
                sortedTiles[x, y] = t; // so we have the tile at the correct spot
            }
            for (int y = 0; y < TileHeight; y++) {
                for (int x = 0; x < TileWidth; x++) {
                    int cX = x;
                    int cY = y;
                    int startX = 0;
                    int startY = 0;
                    switch (Rotation) {
                        case 90:
                            cX = -y;
                            cY = x;
                            startX = PrototypeTileWidth - 1;
                            startY = 0;
                            break;
                        case 180:
                            cX = -x;
                            cY = -y;
                            startX = PrototypeTileWidth - 1;
                            startY = PrototypeTileHeight - 1;
                            break;
                        case 270:
                            cX = y;
                            cY = -x;
                            startX = 0;
                            startY = PrototypeTileHeight - 1;
                            break;
                    }
                    if ((startX + cX) >= BuildTileTypes.GetLength(0) || (startY + cY) >= BuildTileTypes.GetLength(1)) {
                        tileToCanBuild.Add(sortedTiles[x, y], sortedTiles[x, y].CheckTile(this));
                    }
                    else {
                        TileType? requiredTile = BuildTileTypes[startX + cX, startY + cY];
                        if (requiredTile == null) {
                            tileToCanBuild.Add(sortedTiles[x, y], sortedTiles[x, y].CheckTile(this));
                        }
                        else {
                            tileToCanBuild.Add(sortedTiles[x, y], requiredTile == sortedTiles[x, y].Type);
                        }
                    }
                }
            }
            return tileToCanBuild;
        }

        #endregion correctspot

        #region rotation

        public int ChangeRotation(int rotate = 0) {
            this.Rotation = rotate % 360;
            return Rotation;
        }

        public void Rotate() {
            if (CanRotate == false) {
                return;
            }
            Rotation += 90;
            Rotation %= 360;
        }

        public void AddTimes90ToRotate(int times) {
            if (CanRotate == false) {
                return;
            }
            for (int i = 0; i < times; i++) {
                Rotate();
            }
        }

        #endregion rotation

        public float AICalculatedCost() {
            float itemValue = 0;
            foreach (Item item in BuildingItems) {
                itemValue = item.count * item.Data.AIValue;
            }
            return Mathf.RoundToInt(BuildCost + 3 * UpkeepCost + 2 * itemValue);
        }


        public override string ToString() {
            if (BuildTile == null) {
                return Name + "@error";
            }
            return Name + "@ X=" + BuildTile.X + " Y=" + BuildTile.Y + ": " + BuildID;
        }

        public virtual void AddRoadStructure(RoadStructure roadStructure) {
            Roads.Add(roadStructure);
            roadStructure.RegisterOnRouteCallback(OnRouteChange);
            if (Routes.Contains(roadStructure.Route) == false) {
                Routes.Add(roadStructure.Route);
                cbRoutesChanged?.Invoke(this);
            }
            roadStructure.RegisterOnDestroyCallback(OnRoadDestroy);
        }

        protected virtual void OnRouteChange(Route o, Route n) {
            Routes.Remove(o);
            Routes.Add(n);
            cbRoutesChanged?.Invoke(this);
        }

        protected void OnRoadDestroy(Structure structure, IWarfare warfare) {
            RoadStructure road = structure as RoadStructure;
            Roads.Remove(road);
            if(Roads.Select(r => r.Route).Contains(road.Route) == false) {
                RemoveRoute(road.Route);
            }
        }

        public virtual void RemoveRoute(Route route) {
            Routes.Remove(route);
            cbRoutesChanged?.Invoke(this);
        }

        public bool IsPlayer() {
            return PlayerNumber == PlayerController.currentPlayerNumber;
        }
        /// <summary>
        /// Should return if the tileValue should be 0 for this structure tile
        /// </summary>
        /// <returns></returns>
        public bool ShouldAICountTileAsFree() {
            if (this is GrowableStructure g) {
                return !g.IsBeingWorked;
            }
            return CanBeBuildOver;
        }
        public virtual void ToggleActive() {
            //not all structures can be paused -- if it can it is handled in subclass
            isActive = !isActive;
        }

    }
}