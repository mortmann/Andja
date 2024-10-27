using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.Model {

    public class TileValue {
        static readonly short N = 0, E = 1, S = 2, W = 3;
        public TileType Type => tile.Type;
        public int X => tile.X;
        public int Y => tile.Y;
        public int MaxValue => Mathf.Max(WestValue, SouthValue, EastValue, NorthValue);
        public int MinValue => Mathf.Min(WestValue, SouthValue, EastValue, NorthValue);
        public Vector2Int MaxRectangle => Vector2Int.Max(
            Vector2Int.Max(new Vector2Int(EastValue, SouthValue), new Vector2Int(WestValue, SouthValue)),
            Vector2Int.Max(new Vector2Int(EastValue, NorthValue), new Vector2Int(WestValue, NorthValue)));
        public Tile tile;
        int SouthValue = 1;
        int NorthValue = 1;
        int WestValue = 1;
        int EastValue = 1;
        public float Value => ((WestValue + EastValue) / 2) * ((SouthValue + NorthValue) / 2);

        public TileValue(Tile tile, int southValue, int northValue, int westValue, int eastValue) {
            this.tile = tile;
            SouthValue = southValue;
            NorthValue = northValue;
            WestValue = westValue;
            EastValue = eastValue;
        }

        public TileValue(TileValue tileValue) {
            this.tile = tileValue.tile;
            SouthValue = tileValue.SouthValue;
            NorthValue = tileValue.NorthValue;
            WestValue = tileValue.WestValue;
            EastValue = tileValue.EastValue;
        }

        public override string ToString() {
            return "N" + NorthValue + "\nW" + WestValue + "  E" + EastValue + "\nS" + SouthValue;
        }

        public static ConcurrentDictionary<Tile, TileValue> CalculateStartingValues(IIsland island, ICity city = null, bool structureLimit = false) {
            Tile[] tiles = new Tile[island.Height * island.Width];
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    tiles[x * island.Height + y] = World.Current.GetTileAt(island.Minimum.x + x, island.Minimum.y + y);
                }
            }
            TileValue[,] tileValues = CalculateStartingValues(island.Width, island.Height, tiles, city, structureLimit);
            ConcurrentDictionary<Tile, TileValue> values = new ConcurrentDictionary<Tile, TileValue>();
            for (int y = 0; y < island.Height; y++) {
                for (int x = 0; x < island.Width; x++) {
                    if (tileValues[x, y] != null)
                        values.TryAdd(tileValues[x,y].tile, tileValues[x, y]);
                }
            }
            return values;
        }

        internal bool Exact(Vector2Int requiredSpace) {
            if (WestValue == requiredSpace.x && SouthValue == requiredSpace.y) {
                return true;
            }
            if (EastValue == requiredSpace.x && NorthValue == requiredSpace.y) {
                return true;
            }
            return false;
        }

        internal bool Smaller(Vector2Int requiredSpace, bool allDirections = false) {
            if (allDirections) {
                if (WestValue < requiredSpace.x && SouthValue < requiredSpace.y &&
                        EastValue < requiredSpace.x && NorthValue < requiredSpace.y) {
                    return true;
                }
            }
            else {
                if (WestValue < requiredSpace.x && SouthValue < requiredSpace.y) {
                    return true;
                }
                if (EastValue < requiredSpace.x && NorthValue < requiredSpace.y) {
                    return true;
                }
            }
            return false;
        }

        internal bool Fits(Vector2Int requiredSpace, bool allDirections = false) {
            if (allDirections) {
                if (WestValue >= requiredSpace.x && SouthValue >= requiredSpace.y &&
                        EastValue >= requiredSpace.x && NorthValue >= requiredSpace.y) {
                    return true;
                }
            }
            else {
                if (WestValue >= requiredSpace.x && SouthValue >= requiredSpace.y) {
                    return true;
                }
                if (EastValue >= requiredSpace.x && NorthValue >= requiredSpace.y) {
                    return true;
                }
            }
            return false;
        }

        internal void SetValuesToZero() {
            SouthValue = 0;
            NorthValue = 0;
            WestValue = 0;
            EastValue = 0;
        }

        public static TileValue[,] CalculateStartingValues(int Width, int Height, Tile[] Tiles, ICity city = null, bool structureLimit = false) {
            int[,,] value = new int[Width, Height, 4];
            Dictionary<TileType, int[,,]> typeToValue = new Dictionary<TileType, int[,,]>();
            foreach (TileType tt in typeof(TileType).GetEnumValues()) {
                typeToValue[tt] = new int[Width, Height, 4];
            }
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    if (t.Type == TileType.Ocean)
                        continue;
                    if (city != null && t.City != city)
                        continue;
                    if (structureLimit) {
                        if (t.Structure != null && t.Structure.ShouldAICountTileAsFree() == false) {
                            continue;
                        }
                    }
                    int startX = 0;
                    int startY = 0;
                    if (t.IsGenericBuildType()) {
                        if (x > 0)
                            startX = value[x - 1, y, W];
                        if (y > 0)
                            startY = value[x, y - 1, S];
                        value[x, y, W] = startX + 1;
                        value[x, y, S] = startY + 1;
                    }
                    if (x > 0)
                        startX = typeToValue[t.Type][x - 1, y, W];
                    if (y > 0)
                        startY = typeToValue[t.Type][x, y - 1, S];
                    typeToValue[t.Type][x, y, W] = startX + 1;
                    typeToValue[t.Type][x, y, S] = startY + 1;
                }
            }
            for (int y = Height - 1; y > 0; y--) {
                for (int x = Width - 1; x > 0; x--) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    if (t.Type == TileType.Ocean)
                        continue;
                    if (city != null && t.City != city)
                        continue;
                    if (structureLimit) {
                        if (t.Structure != null && t.Structure.ShouldAICountTileAsFree() == false) {
                            continue;
                        }
                    }
                    int startX = 0;
                    int startY = 0;
                    if (t.IsGenericBuildType()) {
                        if (x < Width - 1)
                            startX = value[x + 1, y, E];
                        if (y < Height - 1)
                            startY = value[x, y + 1, N];
                        value[x, y, E] = startX + 1;
                        value[x, y, N] = startY + 1;
                    }
                    if (x < Width - 1)
                        startX = typeToValue[t.Type][x + 1, y, E];
                    if (y < Height - 1)
                        startY = typeToValue[t.Type][x, y + 1, N];
                    typeToValue[t.Type][x, y, E] = startX + 1;
                    typeToValue[t.Type][x, y, N] = startY + 1;
                }
            }
            TileValue[,] values = new TileValue[Width, Height];
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    Tile t = GetTileAt(Tiles, Width, Height, x, y);
                    if (t.CheckTile()) {
                        values[x, y] = new TileValue(t, value[x, y, S], value[x, y, N], value[x, y, W], value[x, y, E]);

                    }
                    else {
                        if (t.Type == TileType.Ocean)
                            continue;
                        values[x, y] = new TileValue(t,
                                                typeToValue[t.Type][x, y, S],
                                                typeToValue[t.Type][x, y, N],
                                                typeToValue[t.Type][x, y, W],
                                                typeToValue[t.Type][x, y, E]
                                        );
                    }
                }
            }
            return values;
        }

        internal bool HasToDoCheck(TileValue tile, Direction direction) {
            return direction switch {
                Direction.N => tile.NorthValue > 1 && tile.NorthValue - 1 > NorthValue,
                Direction.E => tile.EastValue > 1 && tile.EastValue - 1 > EastValue,
                Direction.S => tile.SouthValue > 1 && tile.SouthValue - 1 > SouthValue,
                Direction.W => tile.WestValue > 1 && tile.WestValue - 1 > WestValue,
                _ => throw new ArgumentException(nameof(direction)),
            };
        }

        internal void SetValuePlusOne(Direction direction, TileValue tileValue) {
            switch (direction) {
                case Direction.None:
                    throw new ArgumentException(nameof(direction));
                case Direction.S:
                    NorthValue = tileValue.NorthValue + 1;
                    return;
                case Direction.W:
                    EastValue = tileValue.EastValue + 1;
                    return;
                case Direction.N:
                    SouthValue = tileValue.SouthValue + 1;
                    return;
                case Direction.E:
                    WestValue = tileValue.WestValue + 1;
                    return;
            }
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

        internal void SetValue(Direction direction, int value) {
            switch (direction) {
                case Direction.None:
                    throw new ArgumentException(nameof(direction));
                case Direction.S:
                    NorthValue = value;
                    return;
                case Direction.W:
                    EastValue = value;
                    return;
                case Direction.N:
                    SouthValue = value;
                    return;
                case Direction.E:
                    WestValue = value;
                    return;
            }
        }
    }
}