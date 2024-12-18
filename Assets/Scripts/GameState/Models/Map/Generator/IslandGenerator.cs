﻿using Andja.Controller;
using Andja.Editor;
using Andja.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model.Generator {

    public class IslandGenerator {
        public float Progress;
        public readonly int seed;

        public static readonly float shoreElevation = 0.07f;
        public static readonly float cliffElevation = 0.12f;
        public static readonly float dirtElevation = 0.12f;
        public static readonly float mountainElevation = 0.7f;
        public static readonly float landThreshold = cliffElevation;
        public static readonly float islandThreshold = dirtElevation;
        private ThreadRandom random;
        public int Width;
        public int Height;
        private string debug_string; //TODO: remove when done with generator
        public Tile[] Tiles { get; protected set; }
        private List<IslandFeature> features = new List<IslandFeature>();
        public Climate climate;
        public Dictionary<Tile, Structure> tileToStructure;

        public IslandGenerator(int Width, int Height, int seed, Climate climate) {
            this.climate = climate;
            this.Width = Width;
            this.Height = Height;
            this.seed = seed;
            if (EditorController.IsEditor)
                this.seed = 1643854473;
            random = new ThreadRandom(this.seed);
            tileToStructure = new Dictionary<Tile, Structure>();
            Progress = 0.01f;
        }

        public void Start() {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            SetupTile();

            float[,] heightValues = HeightGenerator.Generate(Width, Height, random, 0.39f, ref Progress);
            //Debug.Log("Generated height float[] in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)!" );
            float[,] moistureValues = MoistureGenerator.Generate(Width, Height, heightValues, random, 0.1f, ref Progress);
            //Progress += 0.1f;
            ////IslandOceanFloodFill(values, out HashSet<Tile> ocean);
            //Debug.Log("IslandOceanFloodFill in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)!");

            //Progress += 0.2f;
            ////MakeShore(values);
            //Debug.Log("MakeShore in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)!");

            //Debug.Log ("FloodFillOcean");

            //Debug.Log("IslandOceanFloodFill in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)!");

            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tile t = GetTileAt(x, y);
                    t.Elevation = heightValues[x, y];
                    if (t.Elevation == 0)
                        continue;
                    t.Moisture = moistureValues[x, y];
                    if (t.Elevation >= shoreElevation) {
                        t = new LandTile(x, y, t);
                        t.Type = TileType.Dirt;
                        SetTileAt(x, y, t);
                    }
                    if (t.Elevation >= mountainElevation) {
                        t.Type = TileType.Mountain;
                    }
                    else if (t.Elevation >= dirtElevation) {
                        t.Type = TileType.Dirt;
                    }
                    else if (t.Elevation >= shoreElevation && HasNeighbourOcean(t, true)) {
                        t.Type = TileType.Shore;
                    }
                    else
                    if (t.Elevation < shoreElevation && t.Elevation > 0) {
                        t = new LandTile(x, y, t);
                        SetTileAt(x, y, t);
                        t.Type = TileType.Water;
                    }
                }
            }
            Progress += 0.1f;

            RandomFeatures();

            Progress += 0.2f;
            //We need to give it a random tilesprite
            //giving sprite needs to be done somewhere else?
            //some depend on already set types
            float f = 0;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tile t = GetTileAt(x, y);
                    t.SpriteName = GetRandomSprite(t);
                    f += t.Moisture;
                }
            }
            Progress += 0.1f;
            PlaceStructures();
            Progress += 0.1f;
            sw.Stop();
            Log.GENERATION_INFO("Generated island " + seed + " with size " + Width + ":" + Height + " in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)! \n" + debug_string);
        }

        private void RandomFeatures() {
            TileValue[,] PreTileValues = TileValue.CalculateStartingValues(Width, Height, Tiles);
            Dictionary<string, int> featureToCount = new Dictionary<string, int>();
            IslandFeaturePrototypeData[] allFeatures = PrototypController.Instance.IslandFeaturePrototypeDatas.Values.ToArray();
            foreach (IslandFeaturePrototypeData feature in allFeatures) {
                featureToCount[feature.ID] = 1;
            }
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    foreach (IslandFeaturePrototypeData proto in allFeatures) {
                        IslandFeature feature = new IslandFeature(proto.ID);
                        if (feature.RequiredTile == PreTileValues[x, y]?.Type) {
                            bool fits = false;
                            switch (feature.fitType) {
                                case FitType.Exact:
                                    fits = PreTileValues[x, y].Exact(feature.RequiredSpace);
                                    break;

                                case FitType.Bigger:
                                    fits = PreTileValues[x, y].Fits(feature.RequiredSpace, true);
                                    break;

                                case FitType.Smaller:
                                    fits = PreTileValues[x, y].Smaller(feature.RequiredSpace, true);
                                    break;
                            }
                            if (fits) {
                                float c = random.Range(0f, 1f);
                                if (feature.GenerateProbability(featureToCount[feature.ID]) > c) {
                                    features.Add(feature);
                                    featureToCount[feature.ID]++;
                                    feature.Generate(this, x, y);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void IncreaseMoisture(Vector2 pos, Vector2 move, int length) {
            if (length <= 0)
                return;
            Tile tile = GetTileAt(pos + move);
            if (tile.Type == TileType.Water || tile.Type == TileType.Ocean)
                return;
            tile.Moisture = (tile.Moisture + GetTileAt(pos).Moisture) / 2f;
            IncreaseMoisture(tile.Vector2, move, --length);
        }

        private void PlaceStructures() {
            FastNoise cubicNoise = new FastNoise();
            cubicNoise.SetFractalGain(1f);
            cubicNoise.SetFractalOctaves(5);
            cubicNoise.SetFractalLacunarity(2f);
            cubicNoise.SetFrequency(0.05f);
            cubicNoise.SetFractalType(FastNoise.FractalType.Billow);
            cubicNoise.SetNoiseType(FastNoise.NoiseType.Cubic);
            cubicNoise.SetSeed(random.Integer());
            FastNoise valueNoise = new FastNoise();
            valueNoise.SetFractalGain(1f);
            valueNoise.SetFractalOctaves(5);
            valueNoise.SetFractalLacunarity(2f);
            valueNoise.SetFrequency(0.75f);
            valueNoise.SetNoiseType(FastNoise.NoiseType.ValueFractal);
            valueNoise.SetSeed(random.Integer());
            List<SpawnStructureGenerationInfo> spstrs = new List<SpawnStructureGenerationInfo>(PrototypController.Instance.SpawnStructureGeneration[climate]);
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    Tile curr = GetTileAt(x, y);
                    foreach (SpawnStructureGenerationInfo sps in spstrs) {
                        if (sps.IsWorldClaimed) //is not thread 100% safe - will be checked again if it is being placed
                            continue;
                        if (sps.requiredTile != null && sps.requiredTile.Length > 0
                            && Array.Exists(sps.requiredTile, t => t == curr.Type) == false) {
                            continue;
                        }
                        if (sps.genType == GenerationType.Random) {
                            if(random.Range(0,1f) < sps.perTileChance) {
                                SpawnStructure(curr, sps, spstrs);
                            }
                            continue;
                        }
                        if (sps.genType == GenerationType.Noise) {
                            if (Mathf.Abs(valueNoise.GetValueFractal(x, y)) > sps.valueFractal) {
                                SpawnStructure(curr, sps, spstrs);

                            }
                            continue;
                        }
                        if (sps.genType == GenerationType.GroupedNoise) {
                            if (Mathf.Abs(cubicNoise.GetCubic(x, y)) + Mathf.Abs(cubicNoise.GetCubicFractal(x, y)) > sps.cubic2Fractal
                            || Mathf.Abs(valueNoise.GetValueFractal(x, y)) > sps.valueFractal) {
                                SpawnStructure(curr, sps, spstrs);
                            }
                        }
                    }
                }
            }
        }

        private void SpawnStructure(Tile curr, SpawnStructureGenerationInfo sps, List<SpawnStructureGenerationInfo> spstrs) {
            Structure s = PrototypController.Instance.GetStructureCopy(sps.ID);
            if(s.TileWidth > 1 || s.TileHeight > 1) {
                if (s.CheckForCorrectSpot(s.GetBuildingTiles(curr)).Values.Contains(false)) {
                    return;
                }
            }
            if(sps.worldUnique) {
                //try to claim this -- locks it and blocks it for others
                if(sps.ClaimWorldUnique() == false) {
                    return;
                }
            }
            if(sps.islandUnique) {
                spstrs.Remove(sps); // we can just remove it here
            }
            if (s is GrowableStructure gs) {
                gs.currentStage = random.Range(0, gs.AgeStages);
            }
            s.AddTimes90ToRotate(random.Range(0, 4));
            tileToStructure.Add(curr, s);
        }

        private void IslandOceanFloodFill(float[,] heights, out HashSet<Tile> ocean) {
            fillMatrix2(heights, 0, 0, true, out ocean);
            //float[,] heightsCopy = new float[Width, Height];
            //Array.Copy(heights, 0, heightsCopy, 0, heights.Length);
            //Debug.Log(heights[0, 0] + " " + heightsCopy[0, 0]);
            //for (int x = 0; x < Width; x++) {
            //    for (int y = 0; y < Height; y++) {
            //        if (heightsCopy[x, y] < landThreshold)
            //            continue;
            //        fillMatrix2(heightsCopy, x, y, false, out HashSet <Tile> temp);
            //        if (island.Count < temp.Count) {
            //            island = temp;
            //        }
            //    }
            //}
        }

        private void fillMatrix2(float[,] heights, int row, int col, bool ocean, out HashSet<Tile> filled) {
            Stack<Point> fillStack = new Stack<Point>();
            filled = new HashSet<Tile>();
            bool[,] alreadyChecked = new bool[Width, Height];
            fillStack.Push(new Point(row, col));
            while (fillStack.Count > 0) {
                Point cords = fillStack.Pop();
                if (cords.X < 0 || cords.X > Width - 1 || cords.Y < 0 || cords.Y > Height - 1)
                    continue;
                if (heights[cords.X, cords.Y] == 0)
                    continue;
                if (ocean) {
                    if (heights[cords.X, cords.Y] > landThreshold)
                        continue;
                }
                else {
                    if (heights[cords.X, cords.Y] < landThreshold)
                        continue;
                }
                heights[cords.X, cords.Y] = 0;
                filled.Add(GetTileAt(cords.X, cords.Y));
                alreadyChecked[cords.X, cords.Y] = true;
                fillStack.Push(new Point(cords.X + 1, cords.Y));
                fillStack.Push(new Point(cords.X - 1, cords.Y));
                fillStack.Push(new Point(cords.X, cords.Y + 1));
                fillStack.Push(new Point(cords.X, cords.Y - 1));
            }
        }

        private string GetRandomSprite(Tile t) {
            if (t.Type == TileType.Ocean)
                return null;
            if (t.Type == TileType.Volcano)
                return t.SpriteName;
            string s = Tile.GetSpriteAddonForTile(t, GetNeighbours(t));
            List<string> all = TileSpriteController.GetSpriteNamesForType(t.Type, climate, s);
            if (all == null || all.Count == 0) {
                return "";
            }
            int rand = random.Range(0, all.Count - 1);
            return all[rand];
        }

        private bool RandomShore(float x, float maxX) {
            float multi = 1 / (maxX);
            float hasToBeUnder = Mathf.Pow((multi * x), 3) - 2 / maxX;
            float rand = random.Range(0f, 2f);
            return rand < hasToBeUnder;
        }

        public Tile[] GetNeighbours(Tile t, bool diagOkay = false, int depth = 1) {
            Tile[] ns = new Tile[4];
            if (diagOkay == true)
                ns = new Tile[8];
            Tile n;
            n = GetTileAt(t.X, t.Y + depth);
            //NORTH
            ns[0] = n;  // Could be null, but that's okay.
                        //WEST
            n = GetTileAt(t.X + depth, t.Y);
            ns[1] = n;  // Could be null, but that's okay.
                        //SOUTH
            n = GetTileAt(t.X, t.Y - depth);
            ns[2] = n;  // Could be null, but that's okay.
                        //EAST
            n = GetTileAt(t.X - depth, t.Y);
            ns[3] = n;  // Could be null, but that's okay.

            if (diagOkay == true) {
                n = GetTileAt(t.X + depth, t.Y + depth);
                ns[4] = n;  // Could be null, but that's okay.
                n = GetTileAt(t.X + depth, t.Y - depth);
                ns[5] = n;  // Could be null, but that's okay.
                n = GetTileAt(t.X - depth, t.Y - depth);
                ns[6] = n;  // Could be null, but that's okay.
                n = GetTileAt(t.X - depth, t.Y + depth);
                ns[7] = n;  // Could be null, but that's okay.
            }

            return ns;
        }

        public Tile GetTileNorthOf(Tile t) {
            return World.Current.GetTileAt(t.X, t.Y + 1);
        }

        public Tile GetTileSouthOf(Tile t) {
            return World.Current.GetTileAt(t.X, t.Y - 1);
        }

        public Tile GetTileEastOf(Tile t) {
            return World.Current.GetTileAt(t.X + 1, t.Y);
        }

        public Tile GetsTileWestOf(Tile t) {
            return World.Current.GetTileAt(t.X - 1, t.Y);
        }

        public bool HasNeighbourLand(Tile t, bool diag = false) {
            foreach (Tile tile in GetNeighbours(t, diag)) {
                if (tile != null && tile.Elevation > islandThreshold) {
                    return true;
                }
            }
            return false;
        }

        public bool HasNeighbourOcean(Tile t, bool diag = false) {
            foreach (Tile tile in GetNeighbours(t, diag)) {
                if (tile != null && tile.Elevation == 0) {
                    return true;
                }
            }
            return false;
        }

        public bool HasNeighbourOcean(float[,] heights, Point point) {
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    if (x == 0 && y == 0)
                        continue;
                    if (point.X + x < 0 || point.X + x > Width - 1 || point.Y - y < 0 || point.Y - y > Height - 1)
                        continue;
                    if (heights[point.X + x, point.Y - y] == 0) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool HasNeighbourLand(float[,] heights, Point point, bool diag = false) {
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    if (x == 0 && y == 0)
                        continue;
                    if (diag == false && Mathf.Abs(x) + Mathf.Abs(y) == 2)
                        continue;
                    if (point.X + x < 0 || point.X + x > Width - 1 || point.Y - y < 0 || point.Y - y > Height - 1)
                        continue;
                    if (heights[point.X + x, point.Y - y] > shoreElevation) {
                        return true;
                    }
                }
            }
            return false;
        }

        public void SetupTile() {
            Tiles = new Tile[Width * Height];
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    SetTileAt(x, y, new Tile(x, y));
                }
            }
        }

        public float GetWeightedYFromRandomX(float size, float dist, float randomX) {
            float x1 = 0f;
            float y1 = 0f;
            float x2 = dist;
            float y2 = size;
            float x3 = dist * 2;
            float y3 = 0f;
            float denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
            float A = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
            float B = (x3 * x3 * (y1 - y2) + x2 * x2 * (y3 - y1) + x1 * x1 * (y2 - y3)) / denom;
            float C = (x2 * x3 * (x2 - x3) * y1 + x3 * x1 * (x3 - x1) * y2 + x1 * x2 * (x1 - x2) * y3) / denom;
            //		Debug.Log (A+"x^2 + "+ B +"x +"+C);
            return A * Mathf.Pow(randomX, 2) + B * randomX + C;
        }

        public void SetTileAt(int x, int y, Tile t) {
            if (x >= Width || y >= Height) {
                return;
            }
            if (x < 0 || y < 0) {
                return;
            }
            Tiles[x * Height + y] = t;
        }

        public Tile GetTileAt(int x, int y) {
            if (x >= Width || y >= Height) {
                return null;
            }
            if (x < 0 || y < 0) {
                return null;
            }
            return Tiles[x * Height + y];
        }

        public Tile GetTileAt(float x, float y) {
            return GetTileAt(Mathf.FloorToInt(x), Mathf.FloorToInt(y));
        }

        public Tile GetTileAt(Vector2 v) {
            return GetTileAt(v.x, v.y);
        }

        private Tile[] GetTilesWithinRangeOf(Tile center, float range) {
            List<Tile> tiles = new List<Tile>();
            for (float x = center.X - range; x <= center.X + range; x++) {
                for (float y = center.Y - range; y <= center.Y + range; y++) {
                    tiles.Add(GetTileAt(Mathf.RoundToInt(x), Mathf.RoundToInt(y)));
                }
            }
            return tiles.ToArray();
        }

        public MapGenerator.IslandData GetIslandData() {
            List<Tile> tiles = new List<Tile>(Tiles);
            tiles.RemoveAll(x => x.Type == TileType.Ocean);
            return new MapGenerator.IslandData(Width, Height, tiles.ToArray(), climate, tileToStructure, features);
        }

        public bool MakeShore(float[,] heights) {
            int averageSize = Width + Height;
            averageSize /= 2;
            int numberOfShores = random.Range(Mathf.RoundToInt(averageSize * 0.025f), Mathf.RoundToInt(averageSize * 0.05f) + 1);
            debug_string += ("\nCreate Number of Shores: " + numberOfShores);

            Vector2 pos = new Vector2(Width / 2, Height / 2);
            Vector2 center = new Vector2(Width / 2, Height / 2);
            Vector2 dir = Vector2.up;
            dir.Normalize();
            Vector2 shorePosition = Vector2.zero;
            while (pos.IsInBounds(0, 0, Width, Height) && pos.y >= 0) {
                if (heights[(int)(pos.x + dir.x), (int)(pos.y + dir.y)] == 0) {
                    shorePosition = pos;
                    break;
                }
                pos += dir;
            }
            if (pos.IsInBounds(0, 0, Width, Height) == false && heights[(int)shorePosition.x, (int)shorePosition.y] <= islandThreshold) {
                debug_string += ("\nFindIslandMakeShore failed to find border");
                return false;
            }
            Stack<Point> testPoints = new Stack<Point>();
            List<Point> borderPoints = new List<Point>();
            testPoints.Push(new Point(pos.x, pos.y));
            bool[,] visited = new bool[Width, Height];
            //Point next = pos;
            //bool hasNext = true;
            //while (hasNext) {
            //    borderPoints.Add(next);
            //    hasNext = false;
            //    for (int i = -1; i <= 1; i++) {
            //        for (int j = -1; j <= 1; j++) {
            //            int nx = next.Y + j;
            //            int ny = next.Y + i;
            //            if (visited[nx,ny] == false && HasNeighbourOcean(heights, new Point(nx, ny))) {
            //                next = new Point(nx, ny);
            //                hasNext = true;
            //                visited[nx, ny] = true;
            //                break;
            //            }
            //            visited[nx, ny] = true;
            //        }
            //        if (hasNext)
            //            break;
            //    }
            //}

            while (testPoints.Count > 0) {
                Point cords = testPoints.Pop();
                if (cords.IsInBounds(0, 0, Width, Height) == false)
                    continue;
                if (visited[cords.X, cords.Y])
                    continue;
                if (heights[cords.X, cords.Y] == 0)
                    continue;
                if (HasNeighbourOcean(heights, cords) == false)
                    continue;
                visited[cords.X, cords.Y] = true;
                if (borderPoints.Count == 0 || borderPoints[borderPoints.Count - 1].Distance(cords) < 2)
                    borderPoints.Add(cords);
                else {
                    int index = borderPoints.FindIndex(fp => fp.Distance(cords) == 1 || fp.Distance(cords) < 2);
                    borderPoints.Insert(index, cords);
                }
                testPoints.Push(new Point(cords.X - 1, cords.Y));
                testPoints.Push(new Point(cords.X, cords.Y - 1));
                testPoints.Push(new Point(cords.X, cords.Y + 1));
                testPoints.Push(new Point(cords.X + 1, cords.Y));
            }

            borderPoints = Point.OrderByDistance(borderPoints, Width, Height);
            //for (int y = 0; y < Height; y++) {
            //    for (int x = 0; x < Width; x++) {
            //        if (heights[x,y]>0&&HasNeighbourOcean(heights, new Point(x, y))) {
            //            borderPoints.Add(new Point(x, y));
            //        }
            //    }
            //}
            if (borderPoints.Count == 0) {
                Log.GENERATION_ERROR("NOT FOUND A SINGLE ISLAND BORDER!");
                return false;
            }
            List<ShoreGen> shores = new List<ShoreGen>();
            for (int i = 0; i < numberOfShores; i++) {
                int x = 0;
                int y = 0;
                Direction direction = (Direction)random.Range(1, 5); // 1=N -> 4=W
                int length = random.Range(Mathf.RoundToInt(averageSize * 0.14f), Mathf.RoundToInt(averageSize * 0.20f));
                int depth = random.Range(Mathf.CeilToInt(averageSize * 0.02f), Mathf.CeilToInt(averageSize * 0.03f));
                debug_string += ("\nShore direction " + direction + " with length:" + length);
                if (direction == Direction.S) {
                    x = random.Range(0, Width);
                    y = 0;
                }
                if (direction == Direction.N) {
                    x = random.Range(0, Width);
                    y = Height - 1;
                }
                if (direction == Direction.W) {
                    x = 0;
                    y = random.Range(0, Height);
                }
                if (direction == Direction.E) {
                    x = Width - 1;
                    y = random.Range(0, Height);
                }
                Point point = new Point(x, y);
                int pointIndex = -1;
                float distance = float.MaxValue;
                shores.Add(new ShoreGen(point, length, distance, pointIndex));
            }
            for (int b = 0; b < borderPoints.Count; b++) {
                foreach (ShoreGen gen in shores) {
                    float temp = borderPoints[b].Distance(gen.direction);
                    if (gen.currDistance > temp) {
                        gen.index = b;
                        gen.currDistance = temp;
                    }
                }
            }
            HashSet<Point> shorePoints = new HashSet<Point>();
            foreach (ShoreGen gen in shores) {
                //foreach (ShoreGen other in shores) {
                //    if(gen.index > other.index && gen.index<= other.index + other.length) {
                //    }
                //    if (gen.index + gen.length > other.index && gen.index + gen.length <= other.index + other.length) {
                //    }
                //}
                for (int p = gen.index; p < gen.index + gen.length; p++) {
                    Point c = borderPoints[p % borderPoints.Count];
                    if (heights[c.X, c.Y] == shoreElevation) {
                        gen.length++;
                        if (gen.length == borderPoints.Count)
                            break;
                    }
                    heights[c.X, c.Y] = 0;
                    shorePoints.Add(c);
                }
            }
            foreach (Point s in shorePoints.ToArray()) {
                shorePoints.Remove(s);
                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++) {
                        Point n = s + new Point(x, y);
                        if (n.X < 0 || n.X > Width - 1 || n.Y < 0 || n.Y > Height - 1)
                            continue;
                        if (heights[n.X, n.Y] > 0) {
                            shorePoints.Add(n);
                        }
                    }
                }
            }
            foreach (Point s in shorePoints.ToArray()) {
                if (NeigbhourCheck(heights, s) == false) {
                    heights[s.X, s.Y] = 0;
                    shorePoints.Remove(s);
                    for (int y = -1; y <= 1; y++) {
                        for (int x = -1; x <= 1; x++) {
                            if (Mathf.Abs(x) + Mathf.Abs(y) == 0)
                                continue;
                            Point n = s + new Point(x, y);
                            if (n.X < 0 || n.X > Width - 1 || n.Y < 0 || n.Y > Height - 1)
                                continue;
                            if (heights[n.X, n.Y] == 0) {
                                continue;
                            }
                            shorePoints.Add(n);
                        }
                    }
                }
            }
            foreach (Point s in shorePoints.ToArray()) {
                heights[s.X, s.Y] = shoreElevation;
            }
            foreach (Point s in shorePoints.ToArray()) {
                if (HasNeighbourLand(heights, s, true) == false)
                    heights[s.X, s.Y] = 0;
            }

            //Point prev = borderPoints[0];
            //for (int p = 0; p < borderPoints.Count; p++) {
            //    Point c = borderPoints[p % borderPoints.Count];
            //    heights[c.X, c.Y] = p % 2 == 0 ? mountainElevation : shoreElevation;
            //    prev = borderPoints[p % borderPoints.Count];
            //}

            //for (int i = 0; i < 10; i++) {
            //    Queue<Tile> toBeSmoothedCopy = new Queue<Tile>(toBeSmoothed);
            //    while (toBeSmoothedCopy.Count > 0) {
            //        Tile curr = toBeSmoothedCopy.Dequeue();
            //        Tile[] neigh = GetNeighbours(curr, true);
            //        float heightvalue = 0;
            //        foreach (Tile nt in neigh) {
            //            if (nt != null)
            //                heightvalue += nt.Elevation;
            //        }
            //        curr.Elevation += heightvalue / neigh.Length;
            //        curr.Elevation /= 2;

            //    }
            //}

            debug_string += ("\nMade coast: ");
            return true;
        }

        private void SmoothPoint(float[,] heights, Point c) {
            if (c.X < 0 || c.X > Width - 1 || c.Y < 0 || c.Y > Height - 1)
                return;
            float height = 0;
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    if (c.X + x < 0 || c.X + x > Width - 1 || c.Y - y < 0 || c.Y - y > Height - 1)
                        continue;
                    height += heights[c.X + x, c.Y + y];
                }
            }
            heights[c.X, c.Y] = height / 9f;
        }

        private bool NeigbhourCheck(float[,] heights, Point c) {
            if (c.X < 0 || c.X > Width - 1 || c.Y < 0 || c.Y > Height - 1)
                return false;
            int land = 0;
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    if (Mathf.Abs(x) + Mathf.Abs(y) == 0 || Mathf.Abs(x + y) == 2)
                        continue;
                    if (c.X + x < 0 || c.X + x > Width - 1 || c.Y - y < 0 || c.Y - y > Height - 1)
                        continue;
                    if (heights[c.X + x, c.Y + y] > 0)
                        land++;
                }
            }
            if (land < 2) {
                return false;
            }
            return true;
        }

        private class ShoreGen {
            public Point direction;
            public float currDistance;
            public int index;
            public int length;

            public ShoreGen(Point direction, int length, float currDistance, int index) {
                this.direction = direction;
                this.currDistance = currDistance;
                this.index = index;
                this.length = length;
            }
        }
    }
}