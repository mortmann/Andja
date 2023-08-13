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

        // The function we callback any time our tile's structure changes
        //some how the first == now is sometimes null even tho it IS NOT NULL
        //second one is the old ! that one is working
        private Action<Structure, Structure> _cbTileOldNewStructureChanged;

        private Action<Tile, Structure> _cbTileStructureChanged;
        //Does not get saved here - because this is handled by the fogOfWarController separate
        internal FogOfWarStructure fogOfWarStructure;
        public override Structure Structure {
            get => _structures;
            set {
                if (_structures != null && _structures == value) {
                    return;
                }
                if (_structures != null && null != value && _structures.ID == value.ID) {
                    Debug.LogWarning("Structure got build over even tho it is the same ID! Is this wanted?? " + value.ID);
                    return;
                }
                Structure oldStructure = _structures;
                if (_structures is { CanBeBuildOver: true } && value != null) {
                    oldStructure.Destroy();
                }
                _structures = value;
                _cbTileOldNewStructureChanged?.Invoke(value, oldStructure);
                _cbTileStructureChanged?.Invoke(this, value);
                Island.ChangeGridTile(this);
            }
        }

        [JsonPropertyAttribute] protected TileType _type = TileType.Ocean;

        public bool ShouldSerialize_type() {
            return EditorController.IsEditor;
        }

        public override TileType Type {
            get => _type;
            set => _type = value;
        }

        protected IIsland island;

        public override Island Island {
            get => island as Island;
            set {
                if (value == null) {
                    Debug.LogError("setting island to NULL is not viable ");
                    return;
                }
                island = value;
            }
        }

        [JsonPropertyAttribute]
        public sealed override string SpriteName { get; set; }

        public bool ShouldSerializeSpriteName() {
            return EditorController.IsEditor;
        }

        private Queue<ICity> _cities;
        private ICity _city;

        public override ICity City {
            get => _city;
            set {
                if (Island == null) {
                    Debug.LogWarning("TRYING TO SET CITY -- BUT TILE DOES NOT HAVE A ISLAND!");
                    return;
                }
                //if the tile gets unclaimed by the current owner of this
                //either wilderness or other player
                if (value == null) {
                    if (_cities != null && _cities.Count > 0) {
                        //if this has more than one city claiming it
                        //its gonna go add them to a queue and giving it
                        //in that order the right to own it
                        ICity c = _cities.Dequeue();
                        if(c == _city) {
                            return;
                        }
                        _city.RemoveTile(this);
                        c.AddTile(this);
                        _city = c;
                        Island.ChangeGridTile(this, true);
                        World.Current.OnTileChanged(this);
                        return;
                    }
                    _city.RemoveTile(this);
                    Island.Wilderness.AddTile(this);
                    _city = Island.Wilderness;
                    Island.ChangeGridTile(this, true);
                    World.Current.OnTileChanged(this);
                    return;
                }
                //warns about double wilderness
                //can be removed for performance if
                //necessary but it helps for development
                if (_city is { PlayerNumber: -1 } && value.PlayerNumber == -1) {
                    _city = value;
                    Island.ChangeGridTile(this, true);
                    return;
                }
                //remembers the order of the cities that have a claim
                //on that tile -- Maybe do a check if the city
                //that currently owns has a another claim on it?
                if (_city != null && _city.IsWilderness() == false) {
                    _cities ??= new Queue<ICity>();
                    _cities.Enqueue(value);
                    return;
                }
                //if the current city is not null remove this from it
                //FIXME is there a performance problem here? if so fix it
                _city?.RemoveTile(this);
                _city = value;
                Island.ChangeGridTile(this, true);
                World.Current.OnTileChanged(this);
            }
        }

        private List<NeedStructure> ListOfInRangeNeedStructures { get; set; }
        private Action<Tile, NeedStructure, bool> _cbNeedStructureChange;

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

        /// <summary>
        /// Register a function to be called back when our tile type changes.
        /// </summary>
        public override void RegisterTileOldNewStructureChangedCallback(Action<Structure, Structure> callback) {
            _cbTileOldNewStructureChanged += callback;
        }

        public override void UnregisterOldNewTileStructureChangedCallback(Action<Structure, Structure> callback) {
            _cbTileOldNewStructureChanged -= callback;
        }

        public override void RegisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
            _cbTileStructureChanged += callback;
        }

        public override void UnregisterTileStructureChangedCallback(Action<Tile, Structure> callback) {
            _cbTileStructureChanged -= callback;
        }

        public override void AddNeedStructure(NeedStructure ns) {
            if (IsBuildType(Type) == false) {
                return;
            }
            ListOfInRangeNeedStructures ??= new List<NeedStructure>();
            _cbNeedStructureChange?.Invoke(this, ns, true);
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
            _cbNeedStructureChange?.Invoke(this, ns, false);
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
            _cbNeedStructureChange += func;
        }

        public void UnregisterOnNeedStructureChange(Action<Tile, NeedStructure, bool> func) {
            _cbNeedStructureChange -= func;
        }

        public override string ToString() {
            if (EditorController.IsEditor)
                return $"[{X}:{Y}]Type:{Type}|Structure:{Structure}";
            return $"[{X}:{Y}]Type:{Type}|Structure:{Structure}|Player:{City?.PlayerNumber.ToString()}";
        }
    }
}