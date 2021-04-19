using Andja.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    /// <summary>
    /// /*Tile type.*/
    /// Ocean = Water outside of Islands -> have no own GameObjects
    /// Shore = Water/Land(Sand or smth) at the borders island -> normally it can be build(only onshore can build here)
    /// Water = Eg Sea or River inside islands
    /// Dirt = Not as good as Grass but can be build on by everything
    /// Grass = Normal Tile for forest and stuff
    /// Stone = its rocky like a big rock -> cant build here
    /// Desert = nothing grows here
    /// Steppe = Exotic goods like it here
    /// Jungle = Exotic goods Love it here
    /// Mountain = you cant build anything here except mines(andso)
    /// </summary>
    public enum TileType { Ocean, Shore, Cliff, Water, Dirt, Grass, Stone, Desert, Steppe, Jungle, Mountain, Volcano };

    public enum TileMark { None, Highlight, Dark }

    [JsonObject(MemberSerialization.OptIn, ItemTypeNameHandling = TypeNameHandling.None)]
    public class Tile : IComparable<Tile>, IEqualityComparer<Tile> {
        [JsonPropertyAttribute] protected ushort x;
        [JsonPropertyAttribute] protected ushort y;

        public int X {
            get {
                return x;
            }
        }

        public int Y {
            get {
                return y;
            }
        }

        public float Elevation;
        public float Moisture;

        [JsonPropertyAttribute]
        public virtual string SpriteName {
            get { return null; }
            set {
            }
        }

        public bool ShouldSerializeSpriteName() {
            return EditorController.IsEditor;
        }

        public Vector3 Vector { get { return new Vector3(x, y, 0); } }
        public Vector2 Vector2 { get { return new Vector2(x, y); } }

        public Tile() {
        }

        public Tile(int x, int y) {
            this.x = (ushort)x;
            this.y = (ushort)y;
        }

        public virtual TileType Type {
            get { return TileType.Ocean; }
            set {
            }
        }

        public float MovementCost {
            get {
                if (Type == TileType.Ocean) {
                    if (Structure != null) {
                        return float.PositiveInfinity;
                    }
                    return 1;
                }
                if (Type == TileType.Mountain) {
                    return Mathf.Infinity;
                }
                if (Structure == null) {
                    return 1;
                }
                if (Structure.StructureTyp != StructureTyp.Pathfinding && Structure.CanBeBuildOver == false) {
                    return float.PositiveInfinity;
                }
                if (Structure.StructureTyp == StructureTyp.Pathfinding) {
                    return 0.25f;
                }
                return 1;
            }
        }

        // Tells us if two tiles are adjacent.
        public bool IsNeighbour(Tile tile, bool diagOkay = false) {
            // Check to see if we have a difference of exactly ONE between the two
            // tile coordinates.  Is so, then we are vertical or horizontal neighbours.
            return
                Mathf.Abs(this.X - tile.X) + Mathf.Abs(this.Y - tile.Y) == 1 ||  // Check hori/vert adjacency
                (diagOkay && (Mathf.Abs(this.X - tile.X) == 1 && Mathf.Abs(this.Y - tile.Y) == 1)); // Check diag adjacency
        }

        internal string ToBaseString() {
            return string.Format("[{0}:{1}]Type:{2}", X, Y, Type);
        }

        /// <summary>
        /// Gets the neighbours.
        /// </summary>
        /// <returns>The neighbours.</returns>
        /// <param name="diagOkay">Is diagonal movement okay?.</param>
        public Tile[] GetNeighbours(bool diagOkay = false) {
            Tile[] ns;

            if (diagOkay == false) {
                ns = new Tile[4];   // Tile order: N E S W
            }
            else {
                ns = new Tile[8];   // Tile order : N E S W NE SE SW NW
            }

            Tile n;

            n = World.Current.GetTileAt(X, Y + 1);
            //NORTH
            ns[0] = n;  // Could be null, but that's okay.
                        //WEST
            n = World.Current.GetTileAt(X + 1, Y);
            ns[1] = n;  // Could be null, but that's okay.
                        //SOUTH
            n = World.Current.GetTileAt(X, Y - 1);
            ns[2] = n;  // Could be null, but that's okay.
                        //EAST
            n = World.Current.GetTileAt(X - 1, Y);
            ns[3] = n;  // Could be null, but that's okay.

            if (diagOkay == true) {
                n = World.Current.GetTileAt(X + 1, Y + 1);
                ns[4] = n;  // Could be null, but that's okay.
                n = World.Current.GetTileAt(X + 1, Y - 1);
                ns[5] = n;  // Could be null, but that's okay.
                n = World.Current.GetTileAt(X - 1, Y - 1);
                ns[6] = n;  // Could be null, but that's okay.
                n = World.Current.GetTileAt(X - 1, Y + 1);
                ns[7] = n;  // Could be null, but that's okay.
            }

            return ns;
        }

        public Tile North() {
            return World.Current.GetTileAt(X, Y + 1);
        }

        public Tile South() {
            return World.Current.GetTileAt(X, Y - 1);
        }

        public Tile East() {
            return World.Current.GetTileAt(X + 1, Y);
        }

        public Tile West() {
            return World.Current.GetTileAt(X - 1, Y);
        }

        /// <summary>
        /// Checks if Structure can be placed on the tile.
        /// </summary>
        /// <returns><c>true</c>, if tile is buildable, <c>false</c> otherwise.</returns>
        /// <param name="t"> if its ok to be build on special tiletypes, forced means if it has to be true for either mountain/shore</param>
        public virtual bool CheckTile() {
            if (Type == TileType.Ocean) {
                return false;
            }
            if (Type == TileType.Water) {
                return false;
            }
            if (Type == TileType.Cliff) {
                return false;
            }
            if (Type == TileType.Mountain) {
                return false;
            }
            if (Type == TileType.Stone) {
                return false;
            }
            if (Type == TileType.Shore) {
                return false;
            }
            if (Structure != null) {
                if (Structure.CanBeBuildOver == false) {
                    return false;
                }
            }
            return true;
        }

        public bool IsGenericBuildType() {
            return IsBuildType(Type);
        }

        public static bool IsBuildType(TileType t) {
            switch (t) {
                case TileType.Ocean:
                case TileType.Shore:
                case TileType.Cliff:
                case TileType.Water:
                case TileType.Mountain:
                case TileType.Volcano:
                    return false;

                case TileType.Dirt:
                case TileType.Grass:
                case TileType.Stone:
                case TileType.Desert:
                case TileType.Steppe:
                case TileType.Jungle:
                    return true;
            }
            Debug.LogError("TileType " + t + " is not defined in IsBuildType. FIX IT!");
            return false;
        }

        /// <summary>
        /// Water doesnt count as unbuildable!
        /// Determines if is unbuildable type the specified t.
        /// </summary>
        /// <returns><c>true</c> if is unbuildable type the specified t; otherwise, <c>false</c>.</returns>
        /// <param name="t">T.</param>
        public static bool IsUnbuildableType(TileType t, TileType toBuildOn) {
            if (t == TileType.Mountain && toBuildOn != TileType.Mountain) {
                return true;
            }
            if (t == TileType.Ocean && toBuildOn != TileType.Ocean) {
                return true;
            }
            return false;
        }

        //Want to have more than one structure in one tile!
        //more than one tree or tree and bench! But for now only one
        public virtual Structure Structure {
            get {
                return null;
            }
            set {
            }
        }

        public virtual Island Island { get { return null; } set { } }

        public virtual City City {
            get {
                return null;
            }
            set {
            }
        }

        public float DistanceFromVector(Vector3 vec) {
            return Vector3.Distance(this.Vector, vec);
        }

        public bool IsInRange(Vector3 vec, float Range) {
            return Vector3.Distance(this.Vector, vec) <= Range;
        }

        /// <summary>
        /// Register a function to be called back when our tile structure changes.
        /// --NEW--OLD--
        /// </summary>
        public virtual void RegisterTileOldNewStructureChangedCallback(Action<Structure, Structure> callback) {
        }

        /// <summary>
        /// Register a function to be called back when our tile structure changes.
        /// --This tile--NEW--
        /// </summary>
        public virtual void RegisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
        }

        public virtual void UnregisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
        }

        /// <summary>
        /// Unregister a callback.
        /// </summary>
        public virtual void UnregisterOldNewTileStructureChangedCallback(Action<Structure, Structure> callback) {
        }

        public virtual void AddNeedStructure(NeedStructure ns) {
        }

        public virtual void RemoveNeedStructure(NeedStructure ns) {
        }

        public virtual List<NeedStructure> GetListOfInRangeCityNeedStructures() {
            return null;
        }

        public virtual List<NeedStructure> GetListOfInRangeNeedStructures(int playernumber) {
            return null;
        }

        public override string ToString() {
            return string.Format("[{0}:{1}]Type:{2}", X, Y, Type);
        }

        public static Vector2 ToStringToTileVector(String tileToString) {
            string[] datas = tileToString.Split('_');
            if (datas.Length != 3) {
                Debug.LogError("Tried to call this with a wrong string.");
                return Vector2.zero;
            }
            if (datas[0] != "tile") {
                Debug.LogError("Tried to call this without a correct tile string.");
                return Vector2.zero;
            }
            return new Vector2(int.Parse(datas[1]), int.Parse(datas[2]));
        }

        public override bool Equals(object obj) {
            // If parameter cannot be cast to ThreeDPoint return false:
            Tile t = obj as Tile;
            if ((object)t == null) {
                return false;
            }
            // Return true if the fields match:
            return t.X == X && t.Y == Y;
        }

        public override int GetHashCode() {
            return X ^ Y;
        }

        public static bool operator ==(Tile a, Tile b) {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            // Return true if the fields match:
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Tile a, Tile b) {
            // If both are null, or both are same instance, return false.
            if (System.Object.ReferenceEquals(a, b)) {
                return false;
            }

            // If one is null, but not both, return true.
            if (((object)a == null) || ((object)b == null)) {
                return true;
            }

            // Return true if the fields not match:
            return a.X != b.X || a.Y != b.Y;
        }

        public static string GetSpriteAddonForTile(Tile t, Tile[] neighbours) {
            string connectOrientation = "";
            for (int i = 0; i < neighbours.Length; i++) {
                if (neighbours[i] != null)
                    connectOrientation += neighbours[i].Type.ToString().ToLower()[0];
            }
            return connectOrientation;
        }

        #region IComparable implementation

        public int CompareTo(Tile other) {
            return X.CompareTo(other.X) * Y.CompareTo(other.Y);
        }

        #endregion IComparable implementation

        #region IEqualityComparer implementation

        public bool Equals(Tile x, Tile y) {
            return x.X == y.X && x.Y == y.Y;
        }

        public int GetHashCode(Tile obj) {
            return x ^ y;
        }

        #endregion IEqualityComparer implementation
    }
}