using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public enum BuildTypes { Drag, Path, Single };
public enum StructureTyp { Pathfinding, Blocking, Free };
public enum Direction { None, N, E, S, W };
public enum ExtraUI { None, Range, Upgrade, Efficiency };
public enum ExtraBuildUI { None, Range, Efficiency };
public enum BuildRestriktions { Land, Shore, Mountain };

public class StructurePrototypeData : LanguageVariables {
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

    public Direction mustFrontBuildDir = Direction.None;

    //doenst get loaded in anyway
    private List<Tile> _myPrototypeTiles;

    public List<Tile> MyPrototypeTiles {
        get {
            if (_myPrototypeTiles == null) {
                CalculatePrototypTiles();
            }
            return _myPrototypeTiles;
        }
    }
    public ExtraUI extraUITyp;
    public StructureTyp myStructureTyp = StructureTyp.Blocking;
    public bool canStartBurning;
    public int maintenanceCost;

    public BuildRestriktions hasToBuildOnRestriktion;
    public bool canBeBuild = true;
    public int buildcost;
    public BuildTypes buildTyp;
    public ExtraBuildUI extraBuildUITyp;
    public Item[] buildingItems;
    public Item[] upgradeItems = null; // set inside prototypecontoller
    public int upgradeCost = 0; // set inside prototypecontoller

    public string spriteBaseName;

    private void CalculatePrototypTiles() {
        _myPrototypeTiles = new List<Tile>();
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
            if (temp.Contains(t) == false && _myPrototypeTiles.Contains(t) == false) {
                _myPrototypeTiles.Add(t);
                Tile[] ns = t.GetNeighbours(false);
                foreach (Tile t2 in ns) {
                    tilesToCheck.Enqueue(t2);
                }
            }
        }
        for (int width = 0; width < tileWidth; width++) {
            MyPrototypeTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y));
            for (int height = 1; height < tileHeight; height++) {
                MyPrototypeTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y + height));
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
            if (myStructureTiles == null)
                return null;
            return myStructureTiles[0];
        }
        set {
            if (myStructureTiles == null)
                myStructureTiles = new List<Tile>();
            myStructureTiles.Add(value);
        }
    }

    [JsonPropertyAttribute] public int rotated = 0;
    [JsonPropertyAttribute] public bool buildInWilderniss = false;
    [JsonPropertyAttribute] protected bool isActive = true;
    #endregion
    #region RuntimeOrOther
    public List<Tile> myStructureTiles;
    public HashSet<Tile> neighbourTiles;


    public HashSet<Tile> myRangeTiles;
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
            List<Tile> ts = new List<Tile>(myStructureTiles);
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
    public bool IsWalkable { get { return this.MyStructureTyp != StructureTyp.Blocking; } }
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

    public bool CanRotate { get { return Data.canRotate; } }
    public bool CanBeBuildOver { get { return Data.canBeBuildOver; } }
    public bool CanBeUpgraded { get { return Data.canBeUpgraded; } }
    public bool CanTakeDamage { get { return Data.canTakeDamage; } }

    public Direction MustFrontBuildDir { get { return Data.mustFrontBuildDir; } }
    public BuildRestriktions HasToBuildOnRestriktion { get { return Data.hasToBuildOnRestriktion; } }
    public BuildTypes BuildTyp { get { return Data.buildTyp; } }
    public StructureTyp MyStructureTyp { get { return Data.myStructureTyp; } }
    public ExtraUI ExtraUITyp { get { return Data.extraUITyp; } }
    public ExtraBuildUI ExtraBuildUITyp { get { return Data.extraBuildUITyp; } }

    public List<Tile> MyPrototypeTiles { get { return Data.MyPrototypeTiles; } }

    public bool CanStartBurning { get { return Data.canStartBurning; } }
    public bool MustBeBuildOnShore { get { return Data.hasToBuildOnRestriktion == BuildRestriktions.Shore; } }
    public bool MustBeBuildOnMountain { get { return Data.hasToBuildOnRestriktion == BuildRestriktions.Mountain; } }

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
        myStructureTiles = new List<Tile>();
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

        myStructureTiles.AddRange(tiles);
        //if we are here we can build this and
        //set the tiles to the this structure -> claim the tiles!
        neighbourTiles = new HashSet<Tile>();
        foreach (Tile mt in myStructureTiles) {
            mt.Structure = this;
            foreach (Tile nbt in mt.GetNeighbours()) {
                if (myStructureTiles.Contains(nbt) == false) {
                    neighbourTiles.Add(nbt);
                }
            }
        }

        //it searches all the tiles it has in its reach!
        myRangeTiles = GetInRangeTiles(myStructureTiles[0]);

        // do on place structure stuff here!
        OnBuild();
        City.RegisterOnEvent(OnEventCreate, OnEventEnded);

        return true;
    }

    public bool IsTileCityViable(Tile t, int player) {
        if (t.MyCity != null && t.MyCity.playerNumber != player) {
            //here it cant build cause someoneelse owns it
            if (t.MyCity.IsWilderness() == false) {
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
    public List<Tile> GetBuildingTiles(float x, float y, bool ignoreRotation = false) {
        x = Mathf.FloorToInt(x);
        y = Mathf.FloorToInt(y);
        List<Tile> tiles = new List<Tile>();
        if (ignoreRotation == false) {
            for (int w = 0; w < TileWidth; w++) {
                //				tiles.Add (World.current.GetTileAt (x + w, y));
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
        myRangeTiles = new HashSet<Tile>();
        float width = firstTile.X - StructureRange;
        float height = firstTile.Y - StructureRange;
        foreach (Tile t in MyPrototypeTiles) {
            myRangeTiles.Add(w.GetTileAt(t.X + width, t.Y + height));
        }
        return myRangeTiles;
    }
    public List<Tile> RoadsAroundStructure() {
        List<Tile> roads = new List<Tile>();
        foreach (Tile item in myStructureTiles) {
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
        foreach (Tile t in myStructureTiles) {
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
        List<bool> bools = new List<bool>(CorrectSpot(tiles).Values);
        return bools.Contains(false) == false;
    }

    public Dictionary<Tile, bool> CorrectSpot(List<Tile> tiles) {
        Dictionary<Tile, bool> tileToCanBuild = new Dictionary<Tile, bool>();
        //to make it faster
        if (MustFrontBuildDir == Direction.None && MustBeBuildOnShore == false && MustBeBuildOnMountain == false) {
            foreach (Tile item in tiles) {
                tileToCanBuild.Add(item, item.CheckTile());
            }
            return tileToCanBuild;
        }

        //TO simplify this we are gonna sort the array so it is in order
        //from the coordinationsystem that means 0,0->width,height
        int max = Mathf.Max(TileWidth, TileHeight);
        Tile[,] sortedTiles = new Tile[max, max];

        List<Tile> ts = new List<Tile>(tiles);

        if (ts.Count == 0)
            return null;

        //ts.RemoveAll (x=>x==null);
        ts.Sort((x, y) => x.X.CompareTo(y.X) * x.Y.CompareTo(y.Y));
        foreach (Tile t in ts) {
            int x = t.X - ts[0].X;
            int y = t.Y - ts[0].Y;
            if (TileWidth <= x || TileHeight <= y || x < 0 || y < 0) {
                Debug.Log(ts.Count);
            }
            sortedTiles[x, y] = t; // so we have the tile at the correct spot
        }

        Direction row = RowToTest();
        switch (row) {
            case Direction.None:
                Debug.LogWarning("Not implementet! How are we gonna do this?");
                return tileToCanBuild;
            case Direction.N:
                return CheckTilesWithRowFix(tileToCanBuild, sortedTiles, TileWidth, TileHeight, false);
            case Direction.E:
                return CheckTilesWithRowFix(tileToCanBuild, sortedTiles, TileWidth, TileHeight, true);
            case Direction.S:
                return CheckTilesWithRowFix(tileToCanBuild, sortedTiles, TileWidth, 0, false);
            case Direction.W:
                return CheckTilesWithRowFix(tileToCanBuild, sortedTiles, 0, TileHeight, true);
            default:
                return null;
        }
    }
    private Dictionary<Tile, bool> CheckTilesWithRowFix(Dictionary<Tile, bool> tileToCanBuild, Tile[,] tiles, int x, int y, bool fixX) {
        if (fixX) {
            x = Mathf.Max(x - 1, 0);
            for (int i = 0; i < y; i++) {
                if (tiles[x, i] == null) {
                    continue;
                }
                tileToCanBuild.Add(tiles[x, i], tiles[x, i].CheckTile(MustBeBuildOnShore, MustBeBuildOnMountain));
                tiles[x, i] = null;
            }
        }
        else {
            y = Mathf.Max(y - 1, 0);
            for (int i = 0; i < x; i++) {
                if (tiles[i, y] == null) {
                    continue;
                }
                tileToCanBuild.Add(tiles[i, y], tiles[i, y].CheckTile(MustBeBuildOnShore, MustBeBuildOnMountain));
                tiles[i, y] = null;
            }
        }
        foreach (Tile t in tiles) {
            if (t == null) {
                continue;
            }
            tileToCanBuild.Add(t, t.CheckTile());
        }
        return tileToCanBuild;
    }
    private Direction RowToTest() {
        if (MustFrontBuildDir == Direction.None) {
            return Direction.None;
        }
        int must = (int)MustFrontBuildDir;
        //so we have either 1,2,3 or 4
        //so just loop through those and add per 90: 1
        int rotNum = rotated / 90; // so we have now 1,2,3
                                   //we add this to the must be correct one
        must += rotNum;
        if (must > 4) {
            must -= 4;
        }
        return (Direction)must;
    }

    #endregion
    #region rotation
    public int ChangeRotation(int x, int y, int rotate = 0) {
        if (rotate == 360) {
            return 0;
        }
        this.rotated = rotate;
        return rotate;
    }
    public void RotateStructure() {
        if (CanRotate == false) {
            return;
        }
        rotated += 90;
        if (rotated == 360) {
            rotated = 0;
        }
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

