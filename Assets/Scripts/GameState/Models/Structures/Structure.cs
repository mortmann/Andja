﻿using Andja.Controller;
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

    public enum Direction { None, N, E, S, W };

    public enum ExtraUI { None, Range, Upgrade, Efficiency };

    public enum ExtraBuildUI { None, Range, Efficiency };

    public enum BuildRestriktions { Land, Shore, Mountain };

    public class StructurePrototypeData : BaseThing {

        public int structureLevel = 0;
        public int structureRange = 0;
        public int tileWidth;
        public int tileHeight;
        public bool canRotate = true;
        public bool canBeBuildOver = false;
        public bool canBeUpgraded = false;
        public bool canTakeDamage = false;
        public bool hasHitbox;// { get; protected set; }
        /// <summary>
        /// Null means no restrikiton so all buildable tiles
        /// </summary>
        public TileType?[,] buildTileTypes;

        //doenst get loaded in anyway
        private List<Tile> _PrototypeRangeTiles;

        public List<Tile> PrototypeRangeTiles {
            get {
                if (_PrototypeRangeTiles == null) {
                    _PrototypeRangeTiles = Util.CalculateRangeTiles(structureRange, tileWidth, tileHeight);
                }
                return _PrototypeRangeTiles;
            }
        }

        [Ignore]
        public int _RangeTileCount = -1;

        public int RangeTileCount {
            get {
                if (_RangeTileCount < 0)
                    _RangeTileCount = Util.CalculateRangeTilesVector2(structureRange, tileWidth, tileHeight).Count;
                return _RangeTileCount;
            }
        }

        public ExtraUI extraUITyp;
        public StructureTyp structureTyp = StructureTyp.Blocking;
        public bool canStartBurning;
        public bool canBeBuild = true;
        public BuildType buildTyp;
        public ExtraBuildUI extraBuildUITyp;

        // set inside prototypecontoller
        public Item[] upgradeItems = null; 
        public int upgradeCost = 0;
        public Dictionary<Climate, string[]> climateSpriteModifier;

    }

    [JsonObject(MemberSerialization.OptIn)]
    public abstract class Structure : IGEventable {

        #region variables

        #region Serialize

        //prototype id
        [JsonPropertyAttribute] public string ID;
        //build id -- when it was build
        [JsonPropertyAttribute] public uint buildID;
        [JsonPropertyAttribute] protected float _health;
        [JsonPropertyAttribute]
        public LandTile BuildTile {
            get {
                if (Tiles == null || Tiles.Count == 0)
                    return null;
                return (LandTile)Tiles[0];
            }
            set {
                if (Tiles == null)
                    Tiles = new List<Tile>();
                Tiles.Add(value);
            }
        }
        [JsonPropertyAttribute] public int rotation = 0;
        [JsonPropertyAttribute] public bool buildInWilderniss = false;
        [JsonPropertyAttribute] protected bool isActive = true;
        [JsonPropertyAttribute] protected string spriteVariant;
        #endregion Serialize

        #region RuntimeOrOther

        public List<Tile> Tiles;
        public HashSet<Tile> NeighbourTiles;
        protected City _city;
        public HashSet<Tile> RangeTiles;
        public string connectOrientation;
        public bool HasExtraUI { get { return ExtraUITyp != ExtraUI.None; } }

        //player id
        public int PlayerNumber {
            get {
                if (City == null) {
                    return -1;
                }
                return City.GetPlayerNumber();
            }
        }

        protected StructurePrototypeData _prototypData;

        public StructurePrototypeData Data {
            get {
                if (_prototypData == null) {
                    _prototypData = PrototypController.Instance.GetStructurePrototypDataForID(ID);
                }
                return _prototypData;
            }
        }

        private Vector2 _Center;

        public Vector2 Center {
            get {
                if (_Center != Vector2.zero)
                    return _Center;
                _Center = Tiles[0].Vector2 + new Vector2(TileWidth / 2, TileHeight / 2);
                return _Center;
            }
        }

        public bool CanBeBuild { get { return Data.canBeBuild; } }
        public bool IsWalkable { get { return this.StructureTyp != StructureTyp.Blocking; } }
        public bool HasHitbox { get { return Data.hasHitbox; } }

        #region EffectVariables

        public float MaxHealth { get { return CalculateRealValue(nameof(Data.maxHealth), Data.maxHealth); } }

        public int UpkeepCost {
            get {
                //Is not allowed to be negativ AND it is not allowed to be <0
                return Mathf.Clamp(CalculateRealValue(nameof(Data.upkeepCost), Data.upkeepCost), 0, int.MaxValue);
            }
        }

        public int StructureRange { get { return CalculateRealValue(nameof(Data.structureRange), Data.structureRange); } }

        #endregion EffectVariables

        public string Name { get { return Data.Name; } }
        public string Description { get { return Data.Description; } }
        public string HoverOver { get { return Data.HoverOver; } }
        public int PopulationLevel { get { return Data.populationLevel; } }
        public int PopulationCount { get { return Data.populationCount; } }
        public int StructureLevel { get { return Data.structureLevel; } }
        public int prototypeTileWidth { get { return Data.tileWidth; } }
        public int prototypeTileHeight { get { return Data.tileHeight; } }
        public TileType?[,] BuildTileTypes => Data.buildTileTypes;

        public bool CanRotate { get { return Data.canRotate; } }
        public bool CanBeBuildOver { get { return Data.canBeBuildOver; } }
        public bool CanBeUpgraded { get { return Data.canBeUpgraded; } }
        public bool CanTakeDamage { get { return Data.canTakeDamage; } }

        public BuildType BuildTyp { get { return Data.buildTyp; } }
        public StructureTyp StructureTyp { get { return Data.structureTyp; } }
        public ExtraUI ExtraUITyp { get { return Data.extraUITyp; } }
        public ExtraBuildUI ExtraBuildUITyp { get { return Data.extraBuildUITyp; } }

        public List<Tile> PrototypeTiles { get { return Data.PrototypeRangeTiles; } }

        public bool CanStartBurning { get { return Data.canStartBurning; } }

        public int BuildCost { get { return Data.buildCost; } }

        public Item[] BuildingItems { get { return Data.buildingItems; } }

        public Item[] UpgradeItems { get { return Data.upgradeItems; } }

        public int UpgradeCost { get { return Data.upgradeCost; } } // set inside prototypecontoller

        public string SpriteName { get { return Data.spriteBaseName/*TODO: make multiple saved sprites possible*/; } }
        public Dictionary<Climate, string[]> ClimateSpriteModifier => Data.climateSpriteModifier;
        protected Action<Structure> cbStructureChanged;
        protected Action<Structure, IWarfare> cbStructureDestroy;
        protected Action<Structure, bool> cbStructureExtraUI;
        protected Action<Structure, string, bool> cbStructureSound;
        protected Action<Structure, City, City> cbOwnerChange;

        protected HashSet<Route> Routes = new HashSet<Route>();
        protected List<RoadStructure> Roads = new List<RoadStructure>();

        protected void BaseCopyData(Structure str) {
            ID = str.ID;
            _prototypData = str.Data;
        }

        #endregion RuntimeOrOther

        #endregion variables

        #region Properties

        public virtual bool IsActiveAndWorking => isActive;
        public bool IsDestroyed => CurrentHealth <= 0;

        public string SmallName { get { return SpriteName.ToLower(); } }

        public City City {
            get { return _city; }
            set {
                if (_city != null && _city != value) {
                    cbOwnerChange?.Invoke(this, _city, value);
                    _city.RemoveStructure(this);
                }
                _city = value;
            }
        }

        public float CurrentHealth {
            get {
                return _health;
            }
            set {
                _health = value;
                if (CanTakeDamage == false) {
                    return;
                }
                if (_health <= 0) {
                    Destroy();
                }
            } 
        } 

        public bool NeedsRepair => CurrentHealth < MaxHealth;

        public int TileWidth {
            get {
                if (rotation == 0 || rotation == 180) {
                    return prototypeTileWidth;
                }
                if (rotation == 90 || rotation == 270) {
                    return prototypeTileHeight;
                }
                // should never come to this if its an error
                Debug.LogError("Structure was rotated out of angle bounds: " + rotation);
                return 0;
            }
        }

        public int TileHeight {
            get {
                if (rotation == 0 || rotation == 180) {
                    return prototypeTileHeight;
                }
                if (rotation == 90 || rotation == 270) {
                    return prototypeTileWidth;
                }
                // should never come to this if its an error
                Debug.LogError("Structure was rotated out of angle bounds: " + rotation);
                return 0;
            }
        }

        public virtual string SortingLayer => "Structures";

        public virtual void OpenExtraUI() {
            cbStructureExtraUI?.Invoke(this, true);
        }

        public virtual void CloseExtraUI() {
            cbStructureExtraUI?.Invoke(this, false);
        }

        #endregion Properties

        #region Virtual/Abstract

        public abstract Structure Clone();

        public virtual void OnUpdate(float deltaTime) {
        }

        public void Update(float deltaTime) {
            if(CurrentHealth > MaxHealth) {
                //Values got changed or maybe upgrade lost? we need to reduce it slowly
                CurrentHealth = Mathf.Clamp(CurrentHealth - 10 * deltaTime, MaxHealth, CurrentHealth);
            }
            UpdateEffects(deltaTime);
            OnUpdate(deltaTime);
        }

        public abstract void OnBuild();

        protected virtual void OnDestroy() {
        }

        /// <summary>
        /// Extra Build UI for showing stuff when building
        /// structures. Or so.
        /// </summary>
        /// <param name="parent">Its the parent for the extra UI.</param>
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

        public void CallbackChangeIfnotNull() {
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

        /// <summary>
        /// 1st Structure (Changed)
        /// 2st OldCity (Owner)
        /// 3st NewCity (Owner)
        /// </summary>
        /// <param name="cb"></param>
        public void RegisterOnOwnerChange(Action<Structure, City, City> cb) {
            cbOwnerChange += cb;
        }

        public void UnregisterOnOwnerChange(Action<Structure, City, City> cb) {
            cbOwnerChange -= cb;
        }

        #endregion callbacks

        #region placestructure

        public bool CheckPlaceStructure(List<Tile> tiles, int playerNumber/*only for error sending*/) {
            if (tiles.Count == 0 || tiles.Contains(null)) {
                Debug.LogError("PlaceStructure FAILED -- tiles is empty or contains null tile!");
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
            return tiles.Count(x => x.City?.PlayerNumber == playerNumber) >= tiles.Count() * GameData.nonCityTilesPercantage;
        }

        public void PlaceStructure(List<Tile> tiles, bool loading) {
            Tiles = new List<Tile>();
            Tiles.AddRange(tiles);
            if (loading == false) {
                CurrentHealth = MaxHealth;
                if(Data.climateSpriteModifier != null && Data.climateSpriteModifier.Count > 0) {
                    Climate c = BuildTile.Island.Climate;
                    if (ClimateSpriteModifier.ContainsKey(c)) {
                        spriteVariant = ClimateSpriteModifier[c][Random.Range(0, ClimateSpriteModifier[c].Length)];
                        spriteVariant += "_" + StructureSpriteController.GetRandomVariant(ID, spriteVariant);
                    }
                }
            }
            //if we are here we can build this and
            //set the tiles to the this structure -> claim the tiles!
            NeighbourTiles = new HashSet<Tile>();
            foreach (Tile mt in Tiles) {
                mt.Structure = this;
                if (EditorController.IsEditor == false) {
                    foreach (Tile nbt in mt.GetNeighbours()) {
                        if (Tiles.Contains(nbt) == false) {
                            NeighbourTiles.Add(nbt);
                        }
                        if (nbt.Structure is RoadStructure) {
                            AddRoadStructure((RoadStructure)nbt.Structure);
                        }
                    }
                }
            }
            //it searches all the tiles it has in its reach!
            RangeTiles = GetInRangeTiles(Tiles[0]);
            // do on place structure stuff here!
            OnBuild();
            City?.RegisterOnEvent(OnEventCreate, OnEventEnded);
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
        public List<Tile> GetBuildingTiles(Tile tile, bool ignoreRotation = false, bool left = false) {
            List<Tile> tiles = new List<Tile>();
            if (left) {
                for (int w = 0; w < TileWidth; w++) {
                    for (int h = 0; h < TileHeight; h++) {
                        tiles.Add(World.Current.GetTileAt(tile.X - w, tile.Y - h));
                    }
                }
            }
            else
            if (ignoreRotation == false) {
                for (int w = 0; w < TileWidth; w++) {
                    for (int h = 0; h < TileHeight; h++) {
                        tiles.Add(World.Current.GetTileAt(tile.X + w, tile.Y + h));
                    }
                }
            }
            else {
                for (int w = 0; w < prototypeTileWidth; w++) {
                    tiles.Add(World.Current.GetTileAt(tile.X + w, tile.Y));
                    for (int h = 1; h < prototypeTileHeight; h++) {
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
            World w = World.Current;
            RangeTiles = new HashSet<Tile>();
            float width = firstTile.X - StructureRange;
            float height = firstTile.Y - StructureRange;
            foreach (Tile t in PrototypeTiles) {
                RangeTiles.Add(w.GetTileAt(t.X + width, t.Y + height));
            }
            return RangeTiles;
        }

        public List<RoadStructure> RoadsAroundStructure() {
            //List<Tile> roads = new List<Tile>();
            //foreach (Tile item in Tiles) {
            //    foreach (Tile n in item.GetNeighbours()) {
            //        if (n.Structure != null) {
            //            if (n.Structure is RoadStructure) {
            //                roads.Add(n);
            //            }
            //        }
            //    }
            //}
            return Roads;
        }

        public HashSet<Route> GetRoutes() {
            //HashSet<Route> r = new HashSet<Route>();
            //foreach(Tile t in RoadsAroundStructure()) {
            //    r.Add(((RoadStructure)t.Structure).Route);
            //}
            return Routes;
        }

        #endregion List<Tile>

        #region Functions

        internal List<Structure> GetNeighbourStructuresInRange(int spreadTileRange) {
            Vector2 lower = Center - new Vector2(TileWidth / 2f + spreadTileRange, TileHeight / 2f + spreadTileRange);
            Vector2 upper = Center + new Vector2(TileWidth / 2f + spreadTileRange, TileHeight / 2f + spreadTileRange);
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

        public void ReduceHealth(float damage) {
            if (CanTakeDamage == false) {
                return;
            }
            if (CurrentHealth <= 0) // fix for killing it too many times -- triggering destroy multiple times
                return;
            if (damage < 0) {
                damage = -damage;
                Debug.LogWarning("Damage should be never smaller than 0 - Fixed it!");
            }
            CurrentHealth = Mathf.Clamp(CurrentHealth - damage, 0, MaxHealth);
        }

        public void RepairHealth(float heal) {
            if (heal < 0) {
                heal = -heal;
                Debug.LogWarning("Healing should be never smaller than 0 - Fixed it!");
            }
            CurrentHealth += heal;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
        }

        internal void ChangeHealth(float change) {
            if (change < 0)
                ReduceHealth(-change); //damage should not be negativ
            if (change > 0)
                RepairHealth(change);
        }
        public bool Demolish(bool isGod = false) {
            if (HasNegativEffect && isGod == false)
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
        public bool Destroy(IWarfare destroyer = null, bool onLoad = false) {
            _health = 0;
            if(onLoad == false) {
                foreach (Tile t in Tiles) {
                    t.Structure = null;
                }
            }
            OnDestroy();
            //TODO: add here for getting res back when destroyer = null? negative effect?
            City.RemoveStructure(this);
            cbStructureDestroy?.Invoke(this, destroyer);
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

        public virtual Item[] GetBuildingItems() {
            return BuildingItems;
        }

        public bool CanBuildOnSpot(List<Tile> tiles) {
            List<bool> bools = new List<bool>(CheckForCorrectSpot(tiles).Values);
            return bools.Contains(false) == false;
        }

        public Dictionary<Tile, bool> CheckForCorrectSpot(List<Tile> tiles) {
            if (tiles.Count == 0)
                return null;
            Dictionary<Tile, bool> tileToCanBuild = new Dictionary<Tile, bool>();
            //to make it faster
            if (BuildTileTypes == null) {
                foreach (Tile item in tiles) {
                    tileToCanBuild.Add(item, item.CheckTile());
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
                    if (rotation == 90) {
                        cX = -y;
                        cY = x;
                        startX = prototypeTileWidth - 1;
                        startY = 0;
                    }
                    else
                    if (rotation == 180) {
                        cX = -x;
                        cY = -y;
                        startX = prototypeTileWidth - 1;
                        startY = prototypeTileHeight - 1;
                    }
                    else
                    if (rotation == 270) {
                        cX = y;
                        cY = -x;
                        startX = 0;
                        startY = prototypeTileHeight - 1;
                    }
                    if ((startX + cX) >= BuildTileTypes.GetLength(0) || (startY + cY) >= BuildTileTypes.GetLength(1)) {
                        tileToCanBuild.Add(sortedTiles[x, y], sortedTiles[x, y].CheckTile());
                    }
                    else {
                        TileType? requiredTile = BuildTileTypes[startX + cX, startY + cY];
                        if (requiredTile == null) {
                            tileToCanBuild.Add(sortedTiles[x, y], sortedTiles[x, y].CheckTile());
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
            this.rotation = rotate % 360;
            return rotation;
        }

        public void RotateStructure() {
            if (CanRotate == false) {
                return;
            }
            rotation += 90;
            rotation %= 360;
        }

        public void AddTimes90ToRotate(int times) {
            if (CanRotate == false) {
                return;
            }
            rotation += 90 * times;
            rotation %= 360;
        }

        #endregion rotation


        public override string ToString() {
            if (BuildTile == null) {
                return Name + "@error";
            }
            return Name + "@ X=" + BuildTile.X + " Y=" + BuildTile.Y;
        }

        internal void AddRoadStructure(RoadStructure roadStructure) {
            Roads.Add(roadStructure);
            roadStructure.RegisterOnRouteCallback(OnRouteChange);
            if (Routes.Contains(roadStructure.Route) == false) {
                Routes.Add(roadStructure.Route);
            }
            roadStructure.RegisterOnDestroyCallback(OnRoadDestroy);
        }

        private void OnRouteChange(Route o, Route n) {
            Routes.Remove(o);
            Routes.Add(n);
        }

        private void OnRoadDestroy(Structure structure, IWarfare warfare) {
            RoadStructure road = structure as RoadStructure;
            Roads.Remove(road);
            Routes.Remove(road.Route);
            foreach (RoadStructure r in Roads) {
                if (Routes.Contains(r.Route) == false)
                    Routes.Add(r.Route);
            }
        }

        internal bool IsPlayer() {
            return PlayerNumber == PlayerController.currentPlayerNumber;
        }

        internal virtual void ToggleActive() {
            //not all structures can be paused -- if it can it is handled in subclass
            isActive = !isActive;
        }

    }
}