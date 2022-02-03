using Andja.Editor;
using Andja.FogOfWar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    [JsonObject(MemberSerialization.OptIn, ItemTypeNameHandling = TypeNameHandling.None)]
    [MoonSharp.Interpreter.MoonSharpUserData]
    public class LandTile : Tile {

        //Want to have more than one structure in one tile!
        //more than one tree or tree and bench! But for now only one
        protected Structure _structures = null;

        public override Structure Structure {
            get {
                return _structures;
            }
            set {
                if (_structures != null && _structures == value) {
                    return;
                }
                if (_structures != null && null != value && _structures.ID == value.ID) {
                    Debug.LogWarning("Structure got build over even tho it is the same ID! Is this wanted?? " + value.ID);
                    return;
                }
                Structure oldStructure = _structures;
                if (_structures != null && _structures.CanBeBuildOver && value != null) {
                    _structures.Destroy();
                }
                _structures = value;
                cbTileOldNewStructureChanged?.Invoke(value, oldStructure);
                cbTileStructureChanged?.Invoke(this, value);
                Island.ChangeGridTile(this);
            }
        }

        [JsonPropertyAttribute] protected TileType _type = TileType.Ocean;

        public bool ShouldSerialize_type() {
            return EditorController.IsEditor;
        }

        public override TileType Type {
            get { return _type;  }
            set {
                _type = value;
            }
        }

        protected Island _Island;

        public override Island Island {
            get { return _Island; }
            set {
                if (value == null) {
                    Debug.LogError("setting island to NULL is not viable " + value);
                    return;
                }
                _Island = value;
            }
        }

        protected string _spriteName;

        [JsonPropertyAttribute]
        public override string SpriteName {
            get { return _spriteName; }
            set {
                _spriteName = value;
            }
        }

        public bool ShouldSerializeSpriteName() {
            return EditorController.IsEditor;
        }

        private Queue<City> cities;
        protected City _City;

        public override City City {
            get {
                return _City;
            }
            set {
                if (Island == null) {
                    Debug.LogWarning("TRYING TO SET CITY -- BUT TILE DOES NOT HAVE A ISLAND!");
                    return;
                }
                //if the tile gets unclaimed by the current owner of this
                //either wilderniss or other player
                if (value == null) {
                    if (cities != null && cities.Count > 0) {
                        //if this has more than one city claiming it
                        //its gonna go add them to a queue and giving it
                        //in that order the right to own it
                        City c = cities.Dequeue();
                        c.AddTile(this);
                        _City = c;
                        Island.ChangeGridTile(this, true);
                        return;
                    }
                    Island.Wilderness.AddTile(this);
                    _City = Island.Wilderness;
                    Island.ChangeGridTile(this, true);
                    return;
                }
                //warns about double wilderniss
                //can be removed for performance if
                //necessary but it helps for development
                if (_City != null && _City.PlayerNumber == -1 && value.PlayerNumber == -1) {
                    _City = value;
                    Island.ChangeGridTile(this, true);
                    return;
                }
                //remembers the order of the cities that have a claim
                //on that tile -- Maybe do a check if the city
                //that currently owns has a another claim onit?
                if (_City != null && _City.IsWilderness() == false) {
                    if (cities == null) {
                        cities = new Queue<City>();
                    }
                    cities.Enqueue(value);
                    return;
                }
                //if the current city is not null remove this from it
                //FIXME is there a performance problem here? ifso fix it
                if (_City != null) {
                    _City.RemoveTile(this);
                }
                _City = value;
                Island.ChangeGridTile(this, true);
            }
        }

        private List<NeedStructure> ListOfInRangeNeedStructures { get; set; }
        private Action<Tile, NeedStructure, bool> cbNeedStructureChange;

        public LandTile() {
        }

        public LandTile(int x, int y) {
            this.x = (ushort)x;
            this.y = (ushort)y;
        }

        public LandTile(int x, int y, Tile t) : this(x,y) {
            Elevation = t.Elevation;
            Moisture = t.Moisture;
            SpriteName = t.SpriteName;
            _type = t.Type;
        }
        public LandTile(Tile t, TileType type) : this(t.X, t.Y, t) {
            _type = type;
        }
        // The function we callback any time our tile's structure changes
        //some how the first == now is sometimes null even tho it IS NOT NULL
        //second one is the old ! that one is working
        private Action<Structure, Structure> cbTileOldNewStructureChanged;

        private Action<Tile, Structure> cbTileStructureChanged;
        //Does not get saved here - because this is handled by the fogOfWarController separat
        internal FogOfWarStructure fogOfWarStructure;

        /// <summary>
        /// Register a function to be called back when our tile type changes.
        /// </summary>
        public override void RegisterTileOldNewStructureChangedCallback(Action<Structure, Structure> callback) {
            cbTileOldNewStructureChanged += callback;
        }

        public override void UnregisterOldNewTileStructureChangedCallback(Action<Structure, Structure> callback) {
            cbTileOldNewStructureChanged -= callback;
        }

        public override void RegisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
            cbTileStructureChanged += callback;
        }

        public override void UnregisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
            cbTileStructureChanged -= callback;
        }

        public override void AddNeedStructure(NeedStructure ns) {
            if (IsBuildType(Type) == false) {
                return;
            }
            if (ListOfInRangeNeedStructures == null) {
                ListOfInRangeNeedStructures = new List<NeedStructure>();
            }
            cbNeedStructureChange?.Invoke(this, ns, true);
            ListOfInRangeNeedStructures.Add(ns);
        }

        public override void RemoveNeedStructure(NeedStructure ns) {
            if (IsBuildType(Type) == false) {
                return;
            }
            if (ListOfInRangeNeedStructures == null) {
                return;
            }
            if (ListOfInRangeNeedStructures.Contains(ns) == false) {
                return;
            }
            cbNeedStructureChange?.Invoke(this, ns, false);
            ListOfInRangeNeedStructures.Remove(ns);
        }

        /// <summary>
        /// Returns all in needStructures that are in range && in the same city than
        /// this tiles city, so its just a shortcut
        /// </summary>
        /// <returns></returns>
        public override List<NeedStructure> GetListOfInRangeCityNeedStructures() {
            if (ListOfInRangeNeedStructures == null)
                return null;
            List<NeedStructure> playerAll = new List<NeedStructure>(ListOfInRangeNeedStructures);
            playerAll.RemoveAll(x => x.PlayerNumber != City.PlayerNumber);
            return playerAll;
        }

        /// <summary>
        ///  Returns all in needStructures that are in range && the player owns the Structures
        /// </summary>
        /// <param name="playernumber"></param>
        /// <returns></returns>
        public override List<NeedStructure> GetListOfInRangeNeedStructures(int playernumber) {
            if (ListOfInRangeNeedStructures == null)
                return null;
            List<NeedStructure> playerAll = new List<NeedStructure>(ListOfInRangeNeedStructures);
            playerAll.RemoveAll(x => x.PlayerNumber != playernumber);
            return playerAll;
        }

        public void RegisterOnNeedStructureChange(Action<Tile, NeedStructure, bool> func) {
            cbNeedStructureChange += func;
        }

        public void UnregisterOnNeedStructureChange(Action<Tile, NeedStructure, bool> func) {
            cbNeedStructureChange -= func;
        }

        public override string ToString() {
            if (EditorController.IsEditor)
                return string.Format("[{0}:{1}]Type:{2}|Structure:{3}", X, Y, Type, Structure);
            return string.Format("[{0}:{1}]Type:{2}|Structure:{3}|Player:{4}", X, Y, Type, Structure, City?.PlayerNumber.ToString());
        }
    }
}