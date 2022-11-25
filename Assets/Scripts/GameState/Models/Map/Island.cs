using Andja.Controller;
using Andja.Pathfinding;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public enum Climate { Cold, Middle, Warm };

    [JsonObject(MemberSerialization.OptIn)]
    public class Island : IGEventable, IIsland {

        #region Serialize

        [JsonPropertyAttribute] public List<ICity> Cities { get; set; }
        [JsonPropertyAttribute] public Climate Climate { get; set; }
        [JsonPropertyAttribute] public Dictionary<string, int> Resources { get; set; }
        [JsonPropertyAttribute] public Tile StartTile { get; set; }

        #endregion Serialize

        #region RuntimeOrOther

        //TODO: find better space for ai variable?
        public bool startClaimed;

        public List<Fertility> Fertilities { get; set; }
        public PathGrid Grid { get; protected set; }

        public int Width {
            get {
                return Mathf.CeilToInt(Maximum.x - Minimum.x) + 1;
            }
        }

        public int Height {
            get {
                return Mathf.CeilToInt(Maximum.y - Minimum.y) + 1;
            }
        }

        public ICity Wilderness {
            get {
                if (_wilderness == null)
                    _wilderness = Cities.Find(x => x.PlayerNumber == GameData.WorldNumber);
                return _wilderness;
            }

            set {
                _wilderness = value;
            }
        }

        public void ChangeGridTile(LandTile landTile, bool cityChange = false) {
            if (cityChange) {
                Grid?.ChangeCityNode(landTile);
            }
            else {
                Grid?.ChangeNode(landTile);
            }
        }

        public List<IslandFeature> Features { get; set; }
        public bool AlreadyHighlighted { get; set; }

        public List<Tile> Tiles;
        public Vector2 Placement { get; set; }
        public Vector2 Minimum { get; set; }
        public Vector2 Maximum { get; set; }
        public Vector2 Center { get; set; }
        private ICity _wilderness;

        #endregion RuntimeOrOther

        public void RemoveResources(string resourceID, int count) {
            if (Resources.ContainsKey(resourceID) == false)
                return;
            Resources[resourceID] -= count;
        }

        public void AddResources(string resourceID, int count) {
            if (Resources.ContainsKey(resourceID) == false)
                return;
            Resources[resourceID] += count;
        }

        public bool HasResource(string resourceID) {
            if (Resources.ContainsKey(resourceID) == false)
                return false;
            return Resources[resourceID] > 0;
        }

        public Island(Tile[] tiles, Climate climate = Climate.Middle) {
            Resources = new Dictionary<string, int>();
            Cities = new List<ICity>();
            this.Climate = climate;
            SetTiles(tiles);
            Setup();
        }

        public Island() {
        }

        private void Setup() {
            AlreadyHighlighted = false;
            ((World)World.Current).RegisterOnEvent(OnEventCreate, OnEventEnded);
            //city that contains all the structures like trees that doesnt belong to any player
            //so it has the playernumber -1 -> needs to be checked for when buildings are placed
            //have a function like is notplayer city
            //it does not need NEEDs
            if (Cities.Count > 0) {

            }
            else {
                Cities.Add(new City(Tiles, this));
                Wilderness = Cities[0];
            }
            Grid = new PathGrid(this);
        }

        public IEnumerable<Structure> Load() {
            Setup();
            List<Structure> structs = new List<Structure>();
            foreach (City c in Cities) {
                if (c.PlayerNumber == -1) {
                    Wilderness = c;
                }
                c.Island = this;
                structs.AddRange(c.Load(this));
            }
            return structs;
        }

        public void SetTiles(Tile[] tiles) {
            this.Tiles = new List<Tile>(tiles);
            StartTile = tiles[0];
            Vector2 minimum = new Vector2(tiles[0].X, tiles[0].Y);
            Vector2 maximum = new Vector2(tiles[0].X, tiles[0].Y);
            foreach (Tile t in tiles) {
                t.Island = this;
                if (minimum.x > t.X) {
                    minimum.x = t.X;
                }
                if (minimum.y > t.Y) {
                    minimum.y = t.Y;
                }
                if (maximum.x < t.X) {
                    maximum.x = t.X;
                }
                if (maximum.y < t.Y) {
                    maximum.y = t.Y;
                }
            }
            this.Maximum = maximum;
            this.Minimum = minimum;
            Center = minimum + ((maximum - minimum) / 2);
            if (Wilderness != null)
                Wilderness.AddTiles(Tiles);
        }

        public void Update(float deltaTime) {
            for (int i = 0; i < Cities.Count; i++) {
                Cities[i].Update(deltaTime);
            }
        }

        public ICity FindCityByPlayer(int playerNumber) {
            return Cities.Find(x => x.PlayerNumber == playerNumber);
        }

        public ICity CreateCity(int playerNumber) {
            if (Cities.Exists(x => x.PlayerNumber == playerNumber)) {
                Debug.LogError("TRIED TO CREATE A SECOND CITY -- IS NEVER ALLOWED TO HAPPEN!");
                return Cities.Find(x => x.PlayerNumber == playerNumber);
            }
            AlreadyHighlighted = false;
            ICity c = new City(playerNumber, this);
            Cities.Add(c);
            return c;
        }

        public void RemoveCity(ICity c) {
            if (c.IsWilderness()) {
                //We could remove it still and recreate it, if it is needed but for now just prevent the deletion 
                Debug.LogWarning("Wanted to remove Wilderniss. It is still needed even if empty: For the case that their will be once again.");
                return;
            }
            Cities.Remove(c);
        }

        #region igeventable

        public override void OnEventCreate(GameEvent ge) {
            OnEvent(ge, cbEventCreated, true);
        }

        private void OnEvent(GameEvent ge, Action<GameEvent> ac, bool start) {
            if (ge.target is Island) {
                if (ge.target == this) {
                    ge.EffectTarget(this, start);
                    ac?.Invoke(ge);
                }
                return;
            }
            else {
                ac?.Invoke(ge);
                return;
            }
        }

        internal ICity GetCurrentPlayerCity() {
            return Cities.Find(c => c.PlayerNumber == PlayerController.currentPlayerNumber);
        }

        public override void OnEventEnded(GameEvent ge) {
            OnEvent(ge, cbEventEnded, false);
        }

        public override int GetPlayerNumber() {
            return -1;
        }

        #endregion igeventable

        public static Range GetRangeForSize(Size sizeType) {
            switch (sizeType) {
                case Size.VerySmall:
                    return new Range(40, 80);

                case Size.Small:
                    return new Range(80, 120);

                case Size.Medium:
                    return new Range(120, 160);

                case Size.Large:
                    return new Range(160, 200);

                case Size.VeryLarge:
                    return new Range(200, int.MaxValue);

                default:
                    //Debug.LogError("NOT RECOGNISED ISLAND SIZE! Nothing has no size!");
                    return new Range(0, 0);
            }
        }

        public static Size GetSizeTyp(int width, int height) {
            foreach (Size size in Enum.GetValues(typeof(Size))) {
                int middle = width + height;
                middle /= 2;
                if (GetRangeForSize(size).IsBetween(middle)) {
                    return size;
                }
            }
            Debug.LogError("The Island does not fit any Range! Widht = " + width + " : Height " + height);
            return Size.Other;
        }
    }
}