using System.Collections.Generic;
using UnityEngine;

namespace Andja.Model {

    public class TileValue {
        public TileType Type => tile.Type;
        public int X => tile.X;
        public int Y => tile.Y;
        public int MaxValue => (int)Mathf.Max(swValue.x, swValue.y, neValue.x, swValue.y);
        public int MinValue => (int)Mathf.Min(swValue.x, swValue.y, neValue.x, swValue.y);
        public Vector2 MaxVector => Vector2.Max(swValue, neValue);
        public Vector2 MinVector => Vector2.Min(swValue, neValue);
        public Tile tile;
        public Vector2 swValue;
        public Vector2 neValue;

        public TileValue(Tile tile, Vector2 seValue, Vector2 nwValue) {
            this.tile = tile;
            this.swValue = seValue;
            this.neValue = nwValue;
        }

        public TileValue(TileValue tileValue) {
            this.tile = tileValue.tile;
            this.swValue = tileValue.swValue;
            this.neValue = tileValue.neValue;
        }

        public override bool Equals(object obj) {
            TileValue p = obj as TileValue;
            if ((object)p == null) {
                return false;
            }
            // Return true if the fields match:
            return p == this;
        }

        public override int GetHashCode() {
            var hashCode = 971533886;
            hashCode = hashCode * -1521134295 + swValue.GetHashCode();
            hashCode = hashCode * -1521134295 + neValue.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(TileValue a, TileValue b) {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b)) {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null)) {
                return false;
            }

            // Return true if the fields match:
            return a.swValue == b.swValue && a.neValue == b.neValue;
        }

        public static bool operator !=(TileValue a, TileValue b) {
            // If both are null, or both are same instance, return false.
            if (System.Object.ReferenceEquals(a, b)) {
                return false;
            }

            // If one is null, but not both, return true.
            if (((object)a == null) || ((object)b == null)) {
                return true;
            }

            // Return true if the fields not match:
            return a.swValue != b.swValue && a.neValue != b.neValue;
        }

        public override string ToString() {
            return "N" + neValue.y + "\nW" + swValue.x + "  E" + neValue.x + "\nS" + swValue.y;
        }

        public static List<TileValue> CalculateStartingValues(Island island) {
            Vector2[,] swValue = new Vector2[island.Width, island.Height];
            Vector2[,] neValue = new Vector2[island.Width, island.Height];
            Dictionary<TileType, Vector2[,]> typeToSWValue = new Dictionary<TileType, Vector2[,]>();
            Dictionary<TileType, Vector2[,]> typeToNEValue = new Dictionary<TileType, Vector2[,]>();
            foreach (TileType tt in typeof(TileType).GetEnumValues()) {
                typeToSWValue[tt] = new Vector2[island.Width, island.Height];
                typeToNEValue[tt] = new Vector2[island.Width, island.Height];
            }
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    Tile t = World.Current.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.Type == TileType.Ocean)
                        continue;
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x > 0)
                            startX = swValue[x - 1, y].x;
                        if (y > 0)
                            startY = swValue[x, y - 1].y;
                        swValue[x, y].x = startX + 1;
                        swValue[x, y].y = startY + 1;
                    }
                    if (x > 0)
                        startX = typeToSWValue[t.Type][x - 1, y].x;
                    if (y > 0)
                        startY = typeToSWValue[t.Type][x, y - 1].y;
                    typeToSWValue[t.Type][x, y].x = startX + 1;
                    typeToSWValue[t.Type][x, y].y = startY + 1;
                }
            }
            for (int y = island.Height - 1; y > 0; y--) {
                for (int x = island.Width - 1; x > 0; x--) {
                    Tile t = World.Current.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x < island.Width - 1)
                            startX = neValue[x + 1, y].x;
                        if (y < island.Height - 1)
                            startY = neValue[x, y + 1].y;
                        neValue[x, y].x = startX + 1;
                        neValue[x, y].y = startY + 1;
                    }
                    if (x < island.Width - 1)
                        startX = typeToNEValue[t.Type][x + 1, y].x;
                    if (y < island.Height - 1)
                        startY = typeToNEValue[t.Type][x, y + 1].y;
                    typeToNEValue[t.Type][x, y].x = startX + 1;
                    typeToNEValue[t.Type][x, y].y = startY + 1;
                }
            }
            List<TileValue> values = new List<TileValue>();
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    Tile t = World.Current.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                    if (t.Type == TileType.Ocean)
                        continue;
                    if (t.CheckTile()) {
                        values.Add(new TileValue(t,
                                        swValue[x, y],
                                        neValue[x, y]
                              ));
                    }
                    else {
                        values.Add(new TileValue(t,
                                                typeToSWValue[t.Type][x, y],
                                                typeToNEValue[t.Type][x, y]
                                        ));
                    }
                }
            }
            return values;
        }

        internal bool Exact(Vector2Int requiredSpace) {
            if (swValue.x == requiredSpace.x && swValue.y == requiredSpace.y) {
                return true;
            }
            if (neValue.x == requiredSpace.x && neValue.y == requiredSpace.y) {
                return true;
            }
            return false;
        }

        internal bool Smaller(Vector2Int requiredSpace, bool allDirections = false) {
            if (allDirections) {
                if (swValue.x < requiredSpace.x && swValue.y < requiredSpace.y &&
                        neValue.x < requiredSpace.x && neValue.y < requiredSpace.y) {
                    return true;
                }
            }
            else {
                if (swValue.x < requiredSpace.x && swValue.y < requiredSpace.y) {
                    return true;
                }
                if (neValue.x < requiredSpace.x && neValue.y < requiredSpace.y) {
                    return true;
                }
            }
            return false;
        }

        internal bool Fits(Vector2Int requiredSpace, bool allDirections = false) {
            if (allDirections) {
                if (swValue.x >= requiredSpace.x && swValue.y >= requiredSpace.y &&
                        neValue.x >= requiredSpace.x && neValue.y >= requiredSpace.y) {
                    return true;
                }
            }
            else {
                if (swValue.x >= requiredSpace.x && swValue.y >= requiredSpace.y) {
                    return true;
                }
                if (neValue.x >= requiredSpace.x && neValue.y >= requiredSpace.y) {
                    return true;
                }
            }
            return false;
        }

        public static TileValue[,] CalculateStartingValues(int Width, int Height, Tile[] Tiles) {
            Vector2[,] swValue = new Vector2[Width, Height];
            Vector2[,] neValue = new Vector2[Width, Height];
            Dictionary<TileType, Vector2[,]> typeToSWValue = new Dictionary<TileType, Vector2[,]>();
            Dictionary<TileType, Vector2[,]> typeToNEValue = new Dictionary<TileType, Vector2[,]>();
            foreach (TileType tt in typeof(TileType).GetEnumValues()) {
                typeToSWValue[tt] = new Vector2[Width, Height];
                typeToNEValue[tt] = new Vector2[Width, Height];
            }
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x > 0)
                            startX = swValue[x - 1, y].x;
                        if (y > 0)
                            startY = swValue[x, y - 1].y;
                        swValue[x, y].x = startX + 1;
                        swValue[x, y].y = startY + 1;
                    }
                    if (x > 0)
                        startX = typeToSWValue[t.Type][x - 1, y].x;
                    if (y > 0)
                        startY = typeToSWValue[t.Type][x, y - 1].y;
                    typeToSWValue[t.Type][x, y].x = startX + 1;
                    typeToSWValue[t.Type][x, y].y = startY + 1;
                }
            }
            for (int y = Height - 1; y > 0; y--) {
                for (int x = Width - 1; x > 0; x--) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    float startX = 0;
                    float startY = 0;
                    if (t.CheckTile()) {
                        if (x < Width - 1)
                            startX = neValue[x + 1, y].x;
                        if (y < Height - 1)
                            startY = neValue[x, y + 1].y;
                        neValue[x, y].x = startX + 1;
                        neValue[x, y].y = startY + 1;
                    }
                    if (x < Width - 1)
                        startX = typeToNEValue[t.Type][x + 1, y].x;
                    if (y < Height - 1)
                        startY = typeToNEValue[t.Type][x, y + 1].y;
                    typeToNEValue[t.Type][x, y].x = startX + 1;
                    typeToNEValue[t.Type][x, y].y = startY + 1;
                }
            }
            TileValue[,] values = new TileValue[Width, Height];
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    if (t.CheckTile()) {
                        values[x, y] = (new TileValue(t,
                                        swValue[x, y],
                                        neValue[x, y]
                              ));
                    }
                    else {
                        values[x, y] = (new TileValue(t,
                                                typeToSWValue[t.Type][x, y],
                                                typeToNEValue[t.Type][x, y]
                                        ));
                    }
                }
            }
            return values;
        }

        public static Tile GetTileAt(Tile[] Tiles, int Width, int Height, int x, int y) {
            if (x >= Width || y >= Height) {
                return null;
            }
            if (x < 0 || y < 0) {
                return null;
            }
            return Tiles[x * Height + y];
        }
    }
}