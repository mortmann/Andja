using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

public enum BuildTypes { Drag, Path, Single };
public enum StructureTyp { Pathfinding, Blocking, Free };
public enum Direction { None, N, E, S, W };
public enum ExtraUI { None, Range, Upgrade, Efficiency };
public enum ExtraBuildUI { None, Range, Efficiency };
public enum BuildRestriktions { Land, Shore, Mountain };

public class StructurePrototypeData : LanguageVariables {
    public string ID;
    public bool hasHitbox;// { get; protected set; }
    public float maxHealth;

    public int structureRange = 0;
    public int populationLevel = 0;
    public int populationCount = 0;
    public int structureLevel = 0;

    public int tileWidth;
    public int tileHeight;

    public bool canRotate = true;
    public bool canBeBuildOver = false;
    public bool canBeUpgraded = false;
    public bool canTakeDamage = false;
    /// <summary>
    /// Null means no restrikiton so all buildable tiles
    /// </summary>
    public TileType?[,] buildTileTypes;

    public Direction mustFrontBuildDir = Direction.None;

    //doenst get loaded in anyway
    private List<Tile> _PrototypeTiles;

    public List<Tile> PrototypeTiles {
        get {
            if (_PrototypeTiles == null) {
                CalculatePrototypTiles();
            }
            return _PrototypeTiles;
        }
    }
    public ExtraUI extraUITyp;
    public StructureTyp structureTyp = StructureTyp.Blocking;
    public bool canStartBurning;
    public int maintenanceCost;

    public bool canBeBuild = true;
    public int buildcost;
    public BuildTypes buildTyp;
    public ExtraBuildUI extraBuildUITyp;
    public Item[] buildingItems;
    public Item[] upgradeItems = null; // set inside prototypecontoller
    public int upgradeCost = 0; // set inside prototypecontoller

    public string spriteBaseName;

    private void CalculatePrototypTiles() {
        _PrototypeTiles = new List<Tile>();
        if (structureRange == 0) {
            return;
        }
        float x;
        float y;
        //get the tile at bottom left to create a "prototype circle"
        Tile firstTile = World.Current.GetTileAt(0 + structureRange, 0 + structureRange);
        float w = (float)tileWidth / 2f - 0.5f;
        float h = (float)tileHeight / 2f - 0.5f;
        Vector2 center = new Vector2(structureRange + w, structureRange + h);

        World world = World.Current;
        HashSet<Tile> temp = new HashSet<Tile>();
        float radius = this.structureRange + 1f;
        for (float a = 0; a < 360; a += 0.5f) {
            x = center.x + radius * Mathf.Cos(a);
            y = center.y + radius * Mathf.Sin(a);
            x = Mathf.RoundToInt(x);
            y = Mathf.RoundToInt(y);
            for (int i = 0; i < structureRange; i++) {
                Tile circleTile = world.GetTileAt(x, y);
                if (temp.Contains(circleTile) == false) {
                    temp.Add(circleTile);
                }
            }
        }
        //like flood fill the inner circle
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(firstTile.South());
        while (tilesToCheck.Count > 0) {
            Tile t = tilesToCheck.Dequeue();
            if (temp.Contains(t) == false && _PrototypeTiles.Contains(t) == false) {
                _PrototypeTiles.Add(t);
                Tile[] ns = t.GetNeighbours(false);
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
        for (int width = 0; width < tileWidth; width++) {
            PrototypeTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y));
            for (int height = 1; height < tileHeight; height++) {
                PrototypeTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y + height));
            }
        }
    }

}

[JsonObject(MemberSerialization.OptIn)]
public abstract class Structure : IGEventable {
    #region variables
    #region Serialize
    //prototype id
    [JsonPropertyAttribute] public string ID;

    //build id -- when it was build
    [JsonPropertyAttribute] public uint buildID;

    [JsonPropertyAttribute] protected City _city;

    [JsonPropertyAttribute] protected float _health;

    [JsonPropertyAttribute]
    public Tile BuildTile {
        get {
            if (StructureTiles == null|| StructureTiles.Count==0) 
                return null;
            return StructureTiles[0];
        }
        set {
            if (StructureTiles == null)
                StructureTiles = new List<Tile>();
            StructureTiles.Add(value);
        }
    }

    [JsonPropertyAttribute] public int rotated = 0;
    [JsonPropertyAttribute] public bool buildInWilderniss = false;
    [JsonPropertyAttribute] protected bool isActive = true;
    #endregion
    #region RuntimeOrOther
    public List<Tile> StructureTiles;
    public HashSet<Tile> NeighbourTiles;


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
    public Vector2 _middlePoint;
    public Vector2 MiddlePoint {
        get {
            if (_middlePoint != Vector2.zero)
                return _middlePoint;
            Tile[,] sortedTiles = new Tile[TileWidth, TileHeight];
            List<Tile> ts = new List<Tile>(StructureTiles);
            ts.Sort((x, y) => x.X.CompareTo(y.X) + x.Y.CompareTo(y.Y));
            foreach (Tile ti in ts) {
                int x = ti.X - ts[0].X;
                int y = ti.Y - ts[0].Y;
                sortedTiles[x, y] = ti; // so we have the tile at the correct spot
            }
            _middlePoint = sortedTiles[0, 0].Vector2 + new Vector2(TileWidth / 2, TileHeight / 2);
            return _middlePoint;
        }
    }

    public bool CanBeBuild { get { return Data.canBeBuild; } }
    public bool IsWalkable { get { return this.StructureTyp != StructureTyp.Blocking; } }
    public bool HasHitbox { get { return Data.hasHitbox; } }

    #region EffectVariables
    public float MaxHealth { get { return CalculateRealValue("maxHealth", Data.maxHealth); } }
    public int MaintenanceCost { get {
            //Is not allowed to be negativ AND it is not allowed to be <0
            return Mathf.Clamp(CalculateRealValue("maintenancecost", Data.maintenanceCost),0,int.MaxValue);
    } }
    public int StructureRange { get { return CalculateRealValue("structureRange", Data.structureRange); } }

    #endregion
    public string Name { get { return Data.Name; } }
    public string Description { get { return Data.Description; } }
    public string HoverOver { get { return Data.HoverOver; } }

    public int PopulationLevel { get { return Data.populationLevel; } }
    public int PopulationCount { get { return Data.populationCount; } }
    public int StructureLevel { get { return Data.structureLevel; } }
    public int _tileWidth { get { return Data.tileWidth; } }
    public int _tileHeight { get { return Data.tileHeight; } }
    public TileType?[,] BuildTileTypes => Data.buildTileTypes;

    public bool CanRotate { get { return Data.canRotate; } }
    public bool CanBeBuildOver { get { return Data.canBeBuildOver; } }
    public bool CanBeUpgraded { get { return Data.canBeUpgraded; } }
    public bool CanTakeDamage { get { return Data.canTakeDamage; } }

    public Direction MustFrontBuildDir { get { return Data.mustFrontBuildDir; } }
    public BuildTypes BuildTyp { get { return Data.buildTyp; } }
    public StructureTyp StructureTyp { get { return Data.structureTyp; } }
    public ExtraUI ExtraUITyp { get { return Data.extraUITyp; } }
    public ExtraBuildUI ExtraBuildUITyp { get { return Data.extraBuildUITyp; } }

    public List<Tile> PrototypeTiles { get { return Data.PrototypeTiles; } }

    public bool CanStartBurning { get { return Data.canStartBurning; } }

    public int BuildCost { get { return Data.buildcost; } }

    public Item[] BuildingItems { get { return Data.buildingItems; } }
    public Item[] UpgradeItems { get { return Data.upgradeItems; } }
    public int UpgradeCost { get { return Data.upgradeCost; } } // set inside prototypecontoller


    public string SpriteName { get { return Data.spriteBaseName/*TODO: make multiple saved sprites possible*/; } }

    protected Action<Structure> cbStructureChanged;
    protected Action<Structure> cbStructureDestroy;
    protected Action<Structure, bool> cbStructureExtraUI;
    protected Action<Structure, string> cbStructureSound;


    protected void BaseCopyData(Structure str) {
        ID = str.ID;
        _prototypData = str.Data;
    }

    #endregion
    #endregion
    #region Properties 
    public virtual bool IsActiveAndWorking => isActive;
    public bool IsDestroyed => CurrentHealth <= 0;

    public Vector2 MiddleVector { get { return new Vector2(BuildTile.X + (float)TileWidth / 2f, BuildTile.Y + (float)TileHeight / 2f); } }


    public string SmallName { get { return SpriteName.ToLower(); } }
    public City City {
        get { return _city; }
        set {
            if (_city != null && _city != value) {
                OnCityChange(_city, value);
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
            if (CanTakeDamage == false) {
                return;
            }
            _health = value;
            if (_health <= 0) {
                Destroy();
            }
        }
    }
    public bool NeedsRepair => CurrentHealth < MaxHealth;
    public int TileWidth {
        get {
            if (rotated == 0 || rotated == 180) {
                return _tileWidth;
            }
            if (rotated == 90 || rotated == 270) {
                return _tileHeight;
            }
            // should never come to this if its an error
            Debug.LogError("Structure was rotated out of angle bounds: " + rotated);
            return 0;
        }
    }
    public int TileHeight {
        get {
            if (rotated == 0 || rotated == 180) {
                return _tileHeight;
            }
            if (rotated == 90 || rotated == 270) {
                return _tileWidth;
            }
            // should never come to this if its an error
            Debug.LogError("Structure was rotated out of angle bounds: " + rotated);
            return 0;
        }
    }
    public void OpenExtraUI() {
        cbStructureExtraUI?.Invoke(this, true);
    }
    public void CloseExtraUI() {
        cbStructureExtraUI?.Invoke(this, false);
    }
    #endregion
    #region Virtual/Abstract
    public abstract Structure Clone();
    public virtual void OnUpdate(float deltaTime) {
        
    }
    public void Update(float deltaTime) {
        UpdateEffects(deltaTime);
        OnUpdate(deltaTime);
    }
    public abstract void OnBuild();

    protected virtual void OnDestroy() { }
    protected virtual void OnCityChange(City old, City newOne) { }
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



    public virtual string GetSpriteName() {
        return SpriteName;
    }
    #endregion

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
    public void RegisterOnDestroyCallback(Action<Structure> cb) {
        cbStructureDestroy += cb;
    }
    public void UnregisterOnDestroyCallback(Action<Structure> cb) {
        cbStructureDestroy -= cb;
    }
    public void RegisterOnSoundCallback(Action<Structure, string> cb) {
        cbStructureSound += cb;
    }
    public void UnregisterOnSoundCallback(Action<Structure, string> cb) {
        cbStructureSound -= cb;
    }
    public void RegisterOnExtraUICallback(Action<Structure, bool> cb) {
        cbStructureExtraUI += cb;
    }
    public void UnregisterOnExtraUICallback(Action<Structure, bool> cb) {
        cbStructureExtraUI -= cb;
    }
    #endregion
    #region placestructure
    public bool PlaceStructure(List<Tile> tiles) {
        StructureTiles = new List<Tile>();
        CurrentHealth = MaxHealth;
        //test if the place is buildable
        // if it has to be on land
        if (CanBuildOnSpot(tiles) == false) {
            Debug.Log("canBuildOnSpot FAILED -- Give UI feedback");
            return false;
        }

        //special check for some structures 
        if (SpecialCheckForBuild(tiles) == false) {
            Debug.Log("specialcheck failed -- Give UI feedback");
            return false;
        }

        StructureTiles.AddRange(tiles);
        //if we are here we can build this and
        //set the tiles to the this structure -> claim the tiles!
        NeighbourTiles = new HashSet<Tile>();
        foreach (Tile mt in StructureTiles) {
            mt.Structure = this;
            foreach (Tile nbt in mt.GetNeighbours()) {
                if (StructureTiles.Contains(nbt) == false) {
                    NeighbourTiles.Add(nbt);
                }
            }
        }

        //it searches all the tiles it has in its reach!
        RangeTiles = GetInRangeTiles(StructureTiles[0]);

        // do on place structure stuff here!
        OnBuild();
        City.RegisterOnEvent(OnEventCreate, OnEventEnded);

        return true;
    }

    public bool IsTileCityViable(Tile t, int player) {
        if (t.City != null && t.City.playerNumber != player) {
            //here it cant build cause someoneelse owns it
            if (t.City.IsWilderness() == false) {
                return false;
            }
            else {
                //HERE it can be build if 
                //EXCEPTION warehouses can be build on new islands
                if (this is WarehouseStructure == false) {
                    return false;
                }
            }
        }
        return true;
    }

    public virtual bool SpecialCheckForBuild(List<Tile> tiles) {
        return true;
    }
    #endregion
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
    public override string GetID() { return ID; } // only needs to get changed WHEN there is diffrent ids
    #endregion
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
    public List<Tile> GetBuildingTiles(float x, float y, bool ignoreRotation = false,bool left = false) {
        x = Mathf.FloorToInt(x);
        y = Mathf.FloorToInt(y);
        List<Tile> tiles = new List<Tile>();
        if(left) {
            for (int w = 0; w < TileWidth; w++) {
                for (int h = 0; h < TileHeight; h++) {
                    tiles.Add(World.Current.GetTileAt(x - w, y - h));
                }
            }
        } else
        if (ignoreRotation == false) {
            for (int w = 0; w < TileWidth; w++) {
                for (int h = 0; h < TileHeight; h++) {
                    tiles.Add(World.Current.GetTileAt(x + w, y + h));
                }
            }
        }
        else {
            for (int w = 0; w < _tileWidth; w++) {
                tiles.Add(World.Current.GetTileAt(x + w, y));
                for (int h = 1; h < _tileHeight; h++) {
                    tiles.Add(World.Current.GetTileAt(x + w, y + h));
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
    public List<Tile> RoadsAroundStructure() {
        List<Tile> roads = new List<Tile>();
        foreach (Tile item in StructureTiles) {
            foreach (Tile n in item.GetNeighbours()) {
                if (n.Structure != null) {
                    if (n.Structure is RoadStructure) {
                        roads.Add(n);
                    }
                }
            }
        }
        return roads;
    }
    public HashSet<Route> GetRoutes() {
        HashSet<Route> r = new HashSet<Route>();
        foreach(Tile t in RoadsAroundStructure()) {
            r.Add(((RoadStructure)t.Structure).Route);
        }
        return r;
    }
    #endregion
    #region Functions
    internal List<Structure> GetNeighbourStructuresInRange(int spreadTileRange) {
        Vector2 lower = MiddlePoint - new Vector2(TileWidth + spreadTileRange, TileHeight + spreadTileRange);
        Vector2 upper = MiddlePoint + new Vector2(TileWidth + spreadTileRange, TileHeight + spreadTileRange);
        List<Structure> structures = new List<Structure>();
        for (float x = lower.x; x <= upper.x; x++) {
            for (float y = lower.y; y <= upper.y; y++) {
                Tile t = World.Current.GetTileAt(x, y);
                if(t.Structure == null || t.Structure == this) {
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
    public void Destroy() {
        _health = 0;
        OnDestroy();
        foreach (Tile t in StructureTiles) {
            t.Structure = null;
        }
        //TODO: add here for getting res back 
        City.RemoveStructure(this);
        cbStructureDestroy?.Invoke(this);
    }
    public bool CanReachStructure(Structure s) {
        HashSet<Route> otherRoutes = s.GetRoutes();
        foreach (Route route in GetRoutes()) {
            if (otherRoutes.Contains(route))
                return true;
        }
        return false;
    }
    #endregion
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
        if (BuildTileTypes==null) {
            foreach (Tile item in tiles) {
                tileToCanBuild.Add(item, item.CheckTile());
            }
            return tileToCanBuild;
        }

        //TO simplify this we are gonna sort the array so it is in order
        //from the coordinationsystem that means 0,0->width,height
        int max = Mathf.Max(TileWidth, TileHeight);
        Tile[,] sortedTiles = new Tile[max, max];
        tiles = tiles.OrderBy(x => x.X).ThenBy(x => x.Y).ToList();
        foreach (Tile t in tiles) {
            int x = t.X - tiles[0].X;
            int y = t.Y - tiles[0].Y;
            if (TileWidth <= x || TileHeight <= y || x < 0 || y < 0) {
                Debug.Log(tiles.Count);
            }
            sortedTiles[x, y] = t; // so we have the tile at the correct spot
        }
        for (int y = 0; y < TileHeight; y++) {
            for (int x = 0; x < TileWidth; x++) {
                int cX = x;
                int cY = y;
                int startX = 0;
                int startY = 0;
                if (rotated == 90) {
                    cX = -y;
                    cY = x;
                    startX = _tileWidth - 1;
                    startY = 0;
                } else
                if (rotated == 180) {
                    cX = -x;
                    cY = -y;
                    startX = _tileWidth - 1;
                    startY = _tileHeight - 1;
                } else
                if (rotated == 270) {
                    cX = y;
                    cY = -x;
                    startX = 0;
                    startY = _tileHeight - 1;
                }
                if((startX + cX)>= BuildTileTypes.GetLength(0) || (startY + cY)>= BuildTileTypes.GetLength(1)) {
                    Debug.Log(rotated + " "+ (startX +"+"+ cX) + " " + (startX + cX) + " " + " " + (startY + "+" + cY) + " " + + (startY + cY) + " " + BuildTileTypes.GetLength(0) + " " + BuildTileTypes.GetLength(1));
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

    #endregion
    #region rotation
    public int ChangeRotation(int x, int y, int rotate = 0) {
        this.rotated = rotate % 360;
        return rotated;
    }
    public void RotateStructure() {
        if (CanRotate == false) {
            return;
        }
        rotated += 90;
        rotated %= 360;
    }
    public void AddTimes90ToRotate(int times) {
        if (CanRotate == false) {
            return;
        }
        rotated += 90 * times;
        rotated %= 360;
    }
    #endregion
    #region override
    public override string ToString() {
        if (BuildTile == null) {
            return SpriteName + "@error";
        }
        return SpriteName + "@ X=" + BuildTile.X + " Y=" + BuildTile.Y;
    }
    #endregion

}

