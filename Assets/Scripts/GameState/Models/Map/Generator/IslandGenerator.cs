using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class IslandGenerator {
    public float Progress;
    public readonly int seed;

    static readonly float shoreElevation = 0.29f;
    static readonly float cliffElevation = 0.37f;
    static readonly float dirtElevation = 0.43f;
    static readonly float mountainElevation = 1.2f;
    static readonly float landThreshold = cliffElevation;
    static readonly float islandThreshold = dirtElevation;
    ThreadRandom random;
    public int Width;
    public int Height;
    public Tile[] Tiles { get; protected set; }
    public Climate climate;
    public Dictionary<Tile, Structure> tileToStructure;
    // Use this for initialization
    public IslandGenerator(int Width, int Height, int seed, int splats, Climate climate) {
        this.climate = climate;
        this.Width = Width;
        this.Height = Height;
        this.seed = seed;
        //this.seed = 10;
        //this.seed = 444448387;
        random = new ThreadRandom(this.seed);
        tileToStructure = new Dictionary<Tile, Structure>();
        Progress = 0.01f;
    }
    public void Start() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        SetupTile();
        //Make some kind of raised area
        int numSplats = random.Range(6, 12);
        int size = Mathf.Min(Height, Width);
        for (int i = 0; i < numSplats; i++) {
            int range = random.Range(size / 10, size / 5);
            int x = random.Range(range, Width - 2 * range);
            int y = random.Range(range, Height - 2 * range);

            float centerHeight = (float)(Height - y + Width - x) / (float)(Height + Width);
            ElevateCircleArea(x, y, range, centerHeight * 0.3f);
        }
        Progress += 0.05f;

        int numOfSquares = random.Range(2, 6);
        for (int i = 0; i < numOfSquares; i++) {
            float maxXRange = ((Width) / (1.75f * numOfSquares));
            float minXRange = ((Width) / (3f * numOfSquares));
            float maxYRange = ((Height) / (1.75f * numOfSquares));
            float minYRange = ((Height) / (3f * numOfSquares));

            int rangeX = Mathf.RoundToInt(random.RangeFloat(minXRange, maxXRange));
            int rangeY = Mathf.RoundToInt(random.RangeFloat(minYRange, maxYRange));

            int cx = random.Range(rangeX, Width - rangeX);
            int cy = random.Range(rangeY, Height - rangeY);

            Rect rect = new Rect(cx, cy, rangeX, rangeY);

            for (int x = Mathf.RoundToInt(rect.xMin); x < Mathf.RoundToInt(rect.xMax); x++) {
                for (int y = Mathf.RoundToInt(rect.yMin); y < Mathf.RoundToInt(rect.yMax); y++) {
                    Tile t = GetTileAt(x, y);
                    if (t == null) {
                        continue;
                    }
                    t.Elevation += GetSquareElevation(t, new Vector2(cx, cy), new Vector2(rangeX, rangeY)) * random.RangeFloat(0.2f, 0.3f);
                }
            }

        }
        Progress += 0.1f;
        
        FastNoise cubic = new FastNoise(random.Range(0, int.MaxValue));
        cubic.SetFractalGain(0.6f);
        cubic.SetFractalOctaves(5);
        cubic.SetFractalLacunarity(2f);
        cubic.SetFrequency(0.01f);
        cubic.SetNoiseType(FastNoise.NoiseType.Cubic);
        cubic.SetFractalType(FastNoise.FractalType.FBM);
        cubic.SetSeed(random.Integer());

        FastNoise cellular = new FastNoise(random.Range(0, int.MaxValue));
        cellular.SetFractalGain(0.2f);
        cellular.SetFrequency(0.25f);
        cellular.SetCellularJitter(0.45f);
        cellular.SetNoiseType(FastNoise.NoiseType.Cellular);
        cellular.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Manhattan);
        cellular.SetGradientPerturbAmp(1);
        cellular.SetSeed(random.Integer());

        foreach (Tile t in Tiles) {
            t.Elevation += GetSquareElevation(t, new Vector2(Width / 2, Height / 2), new Vector2(Width, Height)) * 0.3f;// * random.RangeFloat (0.8f, 1f);
            t.Elevation += GetOvalDistanceToCenter(t) * 0.3f;
            t.Elevation += cubic.GetCubicFractal(t.X, t.Y) *0.8f;
            t.Elevation *= 1 + 0.1f * cellular.GetCellular(t.X, t.Y);
        }
        Progress += 0.2f;

        //for (int i = 0; i < 5; i++) {
        //    //make the it more even spread
        //    foreach (Tile t in tiles) {
        //        Tile[] neigh = GetNeighbours(t);
        //        float height = 0;
        //        foreach (Tile nt in neigh) {
        //            if (nt != null)
        //                height += nt.Elevation;
        //        }
        //        t.Elevation += height / neigh.Length;
        //        t.Elevation /= 2;
        //    }
        //}

        //Make some kind of raised area
        for (int i = 0; i < numSplats; i++) {
            int range = random.Range(size / 20, size / 10);
            int x = random.Range(range, (int)(Width - 0.5f * range));
            int y = random.Range(range, (int)(Height - 0.5f * range));

            float centerHeight = (float)(Height - y + Width - x) / (float)(Height + Width);
            ElevateCircleArea(x, y, range, centerHeight * 0.8f, true);
        }
        Progress += 0.05f;

        //for (int i = 0; i < 2; i++) {
        //    //make the it more even spread
        //    foreach (Tile t in Tiles) {
        //        Tile[] neigh = GetNeighbours(t);
        //        float height = 0;
        //        foreach (Tile nt in neigh) {
        //            if (nt != null)
        //                height += nt.Elevation;
        //        }
        //        t.Elevation += height / neigh.Length;
        //        t.Elevation /= 2;
        //    }
        //}
        for (int i = 0; i < 3; i++) {
            for (int y = Height-1; y > Height/2; y--) {
                for (int x = 0; x < Width/2; x++) {
                    MakeTileEven(GetTileAt(x, y));
                }
                for (int x = Width-1; x > Width / 2; x--) {
                    MakeTileEven(GetTileAt(x, y));
                }
            }
            for (int y = 0; y < Height / 2; y++) {
                for (int x = 0; x < Width / 2; x++) {
                    MakeTileEven(GetTileAt(x, y));
                }
                for (int x = Width - 1; x > Width / 2; x--) {
                    MakeTileEven(GetTileAt(x, y));
                }
            }
        }
        Progress += 0.04f;

        //FastNoise fn = new FastNoise(random.Range(0, int.MaxValue));
        //fn.SetFractalGain(0.3f);
        //fn.SetFractalOctaves(5);
        //fn.SetFractalLacunarity(2f);
        //fn.SetFrequency(0.05f);
        //fn.SetNoiseType(FastNoise.NoiseType.Simplex);

        //foreach (Tile t in Tiles){
        //    if (t.Elevation > islandThreshold) {
        //        continue;
        //    }
        //    t.Elevation += fn.GetValue(t.X, t.Y) * 0.09f;
        //}

        //Debug.Log ("FloodFillLands");

        IslandOceanFloodFill(out HashSet<Tile> island, out HashSet<Tile> ocean);
        Progress += 0.15f;
        int averageSize = Width + Height;
        averageSize /= 2;
        int numberOfShores = random.Range(Mathf.RoundToInt(averageSize * 0.025f), Mathf.RoundToInt(averageSize * 0.05f) + 1);
        Debug.Log("Create Number of Shores: " + numberOfShores);
        for (int ns = 0; ns < numberOfShores; ns++) {
            MakeShore(averageSize);
        }
        Progress += 0.1f;
        //Debug.Log ("FloodFillOcean");
        IslandOceanFloodFill(out island, out ocean,true);

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                Tile t = GetTileAt(x, y);
                if (t.Elevation >= shoreElevation) {
                    t = new LandTile(x, y, t);
                    SetTileAt(x, y, t);
                }
                if (t.Elevation >= mountainElevation) {
                    t.Type = TileType.Mountain;
                }
                else if (t.Elevation >= dirtElevation) {
                    t.Type = TileType.Dirt;
                }
                //				else if(t.Elevation > cliffElevation){
                //					t.Type = TileType.Dirt;
                //					foreach(Tile nt in GetNeighbours(t)){
                //						if(nt == null){
                //							continue;
                //						}
                //						if(nt.Elevation<islandThreshold){
                ////							t.Type = TileType.Cliff;
                //							break;
                //						}
                //					}
                //				}
                else if (t.Elevation >= shoreElevation && HasNeighbourOcean(t, true)) {
                    t.Type = TileType.Shore;
                }
                else
                if (t.Elevation < shoreElevation && ocean.Contains(t) == false || t.Elevation >= shoreElevation) {
                    t = new LandTile(x, y, t);
                    SetTileAt(x, y, t);
                    t.Type = TileType.Water;
                }
            }
        }
        Progress += 0.1f;

        //We need to give it a random tilesprite
        //giving sprite needs to be done somewhere else?
        //some depend on already set types
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                Tile t = GetTileAt(x, y);
                t.SpriteName = GetRandomSprite(t);
            }
        }
        Progress += 0.1f;
        PlaceStructures();
        Progress += 0.1f;
        sw.Stop();
        Debug.Log("Generated island "+ seed +" with size " + Width + ":" + Height + " in " + sw.ElapsedMilliseconds + "ms (" + sw.Elapsed.TotalSeconds + "s)! ");
    }

    private void PlaceStructures() {
        FastNoise cubicNoise = new FastNoise();
        cubicNoise.SetFractalGain(0.5f);
        cubicNoise.SetFractalOctaves(5);
        cubicNoise.SetFractalLacunarity(2f);
        cubicNoise.SetFrequency(0.05f);
        cubicNoise.SetFractalType(FastNoise.FractalType.Billow);
        cubicNoise.SetNoiseType(FastNoise.NoiseType.CubicFractal);
        cubicNoise.SetSeed(random.Integer());
        FastNoise valueNoise = new FastNoise();
        valueNoise.SetFractalGain(1f);
        valueNoise.SetFractalOctaves(5);
        valueNoise.SetFractalLacunarity(2f);
        valueNoise.SetFrequency(0.75f);
        valueNoise.SetNoiseType(FastNoise.NoiseType.Value);
        valueNoise.SetSeed(random.Integer());

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                Tile curr = GetTileAt(x, y);
                if (Tile.IsBuildType(curr.Type)) {
                    if (Mathf.Abs(cubicNoise.GetCubicFractal(x, y))>0.75f || valueNoise.GetValue(x,y)>.87f) {
                        GrowableStructure gs = PrototypController.Instance.GetStructureCopy("tree") as GrowableStructure;
                        gs.currentStage = random.Range(0, gs.AgeStages);
                        tileToStructure.Add(curr, gs) ;
                    }
                }
            }
        }

    }

    private void IslandOceanFloodFill(out HashSet<Tile> island, out HashSet<Tile> ocean, bool includeShore = false) {
        island = FloodFillLands(includeShore);
        //List<Tile> all = new List<Tile>(Tiles);
        //all.ForEach(x => {
        //    if (island.Contains(x) == false) {
        //        x.Elevation = 0f;
        //    }
        //}
        //);
        ocean = new HashSet<Tile>(FloodFillOcean(island));
        foreach (Tile t in ocean) {
            if (island.Contains(t) == false) {
                t.Elevation = 0f;
            }
        }
        island = FloodFillLands(includeShore);

    }

    private string GetRandomSprite(Tile t) {
        List<string> all = TileSpriteController.GetSpriteNamesForType(t.Type, climate, Tile.GetSpriteAddonForTile(t, GetNeighbours(t)));
        if (all == null) {
            Debug.Log(t.Type);
            return "";
        }
        int rand = random.Range(0, all.Count - 1);

        return all[rand];
    }

    //Returns biggest land mass as list
    protected HashSet<Tile> FloodFillLands(bool includeShore = false) {
        List<Tile> allTiles = new List<Tile>(Tiles);
        HashSet<Tile> currIslandTiles = new HashSet<Tile>();
        allTiles.RemoveAll(t => t.Elevation < islandThreshold);
        while (allTiles.Count > currIslandTiles.Count) {
            Tile tile = allTiles[0];
            allTiles.RemoveAt(0);
            Queue<Tile> tilesToCheck = new Queue<Tile>();
            HashSet<Tile> islandTiles = new HashSet<Tile>();
            tilesToCheck.Enqueue(tile);
            while (tilesToCheck.Count > 0) {
                Tile t = tilesToCheck.Dequeue();
                if (t == null || islandTiles.Contains(t)) {
                    continue;
                }
                if (includeShore==false && t.Elevation < islandThreshold) {
                    continue;
                }
                if (includeShore && t.Elevation < shoreElevation) {
                    continue;
                }
                if(HasNeighbourLand(t)==false) {
                    continue;
                }
                islandTiles.Add(t);
                allTiles.Remove(t);
                if (t.Elevation < islandThreshold)
                    continue;
                Tile[] ns = GetNeighbours(t);
                foreach (Tile t2 in ns) {
                    if (t2 != null)
                        tilesToCheck.Enqueue(t2);
                }
            }
            if (currIslandTiles.Count < islandTiles.Count) {
                currIslandTiles = islandTiles;
            }
        }
        return currIslandTiles;
    }
    protected HashSet<Tile> FloodFillOcean(HashSet<Tile> island) {
        HashSet<Tile> ocean = new HashSet<Tile>();
        Queue<Tile> tilesToCheck = new Queue<Tile>();
        tilesToCheck.Enqueue(Tiles[0]);
        while (tilesToCheck.Count > 0) {
            Tile t = tilesToCheck.Dequeue();
            if (t == null || ocean.Contains(t) || island.Contains(t)) {
                continue;
            }
            Tile[] neighbours = GetNeighbours(t);
            t.Elevation = 0;
            ocean.Add(t);
            foreach (Tile neigh in neighbours) {
                tilesToCheck.Enqueue(neigh);
            }
        }
        return ocean;
    }

    void MakeTileEven(Tile t) {
        Tile[] neigh = GetNeighbours(t);
        float height = 0;
        foreach (Tile nt in neigh) {
            if (nt != null)
                height += nt.Elevation;
        }
        t.Elevation += height / neigh.Length;
        t.Elevation /= 2;
    }

    bool RandomShore(float x, float maxX) {
        float multi = 1 / (maxX);
        float hasToBeUnder = Mathf.Pow((multi * x), 3) - 2 / maxX;
        float rand = random.RangeFloat(0f, 2f);
        return rand < hasToBeUnder;
    }

    public Tile[] GetNeighbours(Tile t, bool diagOkay = false, int depth=1) {
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
            if (tile!=null && tile.Elevation > islandThreshold) {
                return true;
            }
        }
        return false;
    }
    public bool HasNeighbourOcean(Tile t,bool diag = false) {
        foreach (Tile tile in GetNeighbours(t,diag)) {
            if (tile != null && tile.Elevation == 0) {
                return true;
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
    public float GetSquareElevation(Tile tile, Vector2 center, Vector2 dimension) {
        Vector2 vec = center - tile.Vector2;
        vec = new Vector2(Mathf.Abs(vec.x), Mathf.Abs(vec.y));
        vec /= center;
        return 1 - (vec.x + vec.y)/2;
            
        //    Mathf.Min(
        //    (float)tile.Y / center.y + (float)(dimension.y - tile.Y) / center.y,
        //    (float)tile.X / center.x + (float)(dimension.y - tile.Y) / center.x
        //);
    }

    public float GetOvalDistanceToCenter(Tile tile) {
        float centerX = (float)(Width / 2); //- random.RangeFloat(-5f, 5f);
        float centerY = (float)(Height / 2);// -random.RangeFloat(-5f, 5f);
        if (Mathf.Pow(tile.X - centerX, 2) / Mathf.Pow(Width, 2) + Mathf.Pow(tile.Y - centerY, 2) / Mathf.Pow(Height, 2) > 1) {
            return 0;
        }
        float dist = (float)Mathf.Abs(centerX - tile.X) / centerX + Mathf.Abs(centerY - tile.Y) / centerY;
        return 1 - dist;
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
    void ElevateCircleArea(int x, int y, int range, float centerHeight = .8f, bool hasToBeLand = false) {
        Tile centerTile = GetTileAt(x, y);

        HashSet<Vector2> areaTiles = Util.CalculateMidPointCircleVector2(range,0,0,x,y);

        foreach (Vector2 vec in areaTiles) {
            Tile h = GetTileAt(vec.x, vec.y);
            if (h==null || h.Elevation < dirtElevation && hasToBeLand == true) {
                continue;
            }
            h.Elevation += centerHeight * Mathf.Lerp(1f, 0.25f, Mathf.Pow(centerTile.DistanceFromVector(h.Vector) / range, 2f));
        }
    }
    Tile[] GetTilesWithinRangeOf(Tile center, float range) {
        List<Tile> tiles = new List<Tile>();
        for (float x = center.X - range; x <= center.X + range; x++) {
            for (float y = center.Y - range; y <= center.Y + range; y++) {
                tiles.Add(GetTileAt(Mathf.RoundToInt(x), Mathf.RoundToInt(y)));
            }
        }
        return tiles.ToArray();
    }

    public MapGenerator.IslandStruct GetIslandStruct() {
        List<Tile> tiles = new List<Tile>(Tiles);
        tiles.RemoveAll(x => x.Type == TileType.Ocean);
        return new MapGenerator.IslandStruct(Width, Height, tiles.ToArray(), climate,
            MapGenerator.Instance.GetFertilitiesForClimate(climate, 3/*TODO:nonstatic*/), new Dictionary<string, int>(), tileToStructure);
    }

    public bool MakeShore(float averageSize) {
        int x = 0;
        int y = 0;
        Direction direction = (Direction)random.Range(1, 5); // 1=N -> 4=W
        int length = random.Range(Mathf.RoundToInt(averageSize * 0.14f), Mathf.RoundToInt(averageSize * 0.20f));
        int depth = random.Range(Mathf.CeilToInt(averageSize * 0.02f), Mathf.CeilToInt(averageSize * 0.03f));
        int width = length;
        int height = depth;
        Debug.Log("Shore direction " + direction +  " with length:" + length);
        if (direction == Direction.S) { // Bottom
            //width = length;
            //height = depth;
            x = random.Range(0, Width);
            y = 0;
        }
        if (direction == Direction.N) { // Top
            //width = length;
            //height = depth;
            x = random.Range(0, Width);
            y = Height - 1;
        }
        if (direction == Direction.W) { //left
            //width = depth;
            //height = length;
            x = 0;
            y = random.Range(0, Height);
        }
        if (direction == Direction.E) { // right
            //width = depth;
            //height = length;
            x = Width - 1;
            y = random.Range(0, Height);
        }

        Vector2 pos = new Vector2(x, y);
        Vector2 center = new Vector2(Width / 2, Height / 2) + random.RangeFloat(-0.1f,0.1f) * new Vector2(Width / 2, Height / 2);
        Vector2 dir = center - pos;
        dir.Normalize();
        //dir += random.RangeFloat(0, 0.05f) * dir;
        Tile current = GetTileAt(x, y);
        while (current != null && current.Elevation <= islandThreshold) {
            pos += dir;
            current = GetTileAt(pos.x, pos.y);
        }
        if (current == null) {
            Debug.Log("FindIslandMakeShore failed to find middle");
            return false;
        }
        HashSet<Tile> border = new HashSet<Tile> {
            current
        };
        //int coast_x = current.X - width / 2;
        //int coast_y = current.Y - height / 2;
        //HashSet<Vector2> vec2s = Util.CalculateMidPointEllipseVector2(width, height, coast_x, coast_y);
        //foreach(Vector2 v in vec2s) {
        //    Tile t = GetTileAt(v.x, v.y);
        //    if (t==null || t.Elevation < landThreshold || HasNeighbourLand(t)==false) {
        //        continue;
        //    }
        //    t.Elevation = shoreElevation + 0.01f;
        //}
        //vec2s = Util.CalculateMidPointEllipseFillVector2(width-1, height, coast_x, coast_y);
        //foreach (Vector2 v in vec2s) {
        //    Tile t = GetTileAt(v.x, v.y);
        //    t.Elevation = 0f;
        //}


        Tile last = current;
        for (int i = 1; i < width; i++) {
            Tile next = null;
            Tile[] neighbours = GetNeighbours(last, false);
            foreach (Tile t in neighbours) {
                if (HasNeighbourLand(t, false) == false) {
                    t.Elevation = 0;
                    continue;
                }
                if (t == null || t.Elevation < islandThreshold)
                    continue;
                if (border.Contains(t))
                    continue;
                if (HasNeighbourOcean(t, true) == false)
                    continue;
                next = t;
            }
            if (next == null && current == last) {
                break;
            }
            else if (next == null) {
                next = current;
            }
            border.Add(next);
            //next.Elevation = shoreElevation + 0.01f;
            last = next;
        }

        //HashSet<Tile> coast = new HashSet<Tile>();
        HashSet<Tile> allreadyInQueue = new HashSet<Tile>();
        Queue<Tile> toBeSmoothed = new Queue<Tile>();
        foreach (Tile cTile in border) {
            //coast.Add(cTile);
            cTile.Elevation = 0;
            //cTile.Elevation = shoreElevation + 0.01f;
            for (int i = 1; i < 5; i++) {
                Tile[] neighs = GetNeighbours(cTile, true);
                foreach (Tile t in neighs) {
                    if (allreadyInQueue.Contains(t)==false)
                        toBeSmoothed.Enqueue(t);
                    allreadyInQueue.Add(t);
                    if (i == 4)
                        t.Elevation = shoreElevation + 0.01f;
                    else
                        t.Elevation = 0f;
                }
            }
            //cTile.Elevation = shoreElevation + 0.01f;
        }

        for (int i = 0; i < 10; i++) {
            Queue<Tile> toBeSmoothedCopy = new Queue<Tile>(toBeSmoothed);
            while (toBeSmoothedCopy.Count > 0) {
                Tile curr = toBeSmoothedCopy.Dequeue();
                Tile[] neigh = GetNeighbours(curr, true);
                float heightvalue = 0;
                foreach (Tile nt in neigh) {
                    if (nt != null)
                        heightvalue += nt.Elevation;
                }
                curr.Elevation += heightvalue / neigh.Length;
                curr.Elevation /= 2;

            }
        }
        //foreach (Tile t in border) {
        //    t.Elevation = 10;
        //}

        //coast.RemoveWhere(v => v == null);
        //Tile[] orderedCoast = coast.OrderBy(b=>b.X).ThenBy(q=>q.Y).ToArray();
        //Tile firstTile = orderedCoast[0];
        //Tile lastTile = orderedCoast[orderedCoast.Length-1];
        //for (int cx = firstTile.X; cx <= lastTile.X; cx++) {
        //    for (int cy = firstTile.Y; cy <= lastTile.Y; cy++) {
        //        Tile curr = GetTileAt(cx, cy);
        //        Tile[] neigh = GetNeighbours(curr);
        //        float heightvalue = 0;
        //        foreach (Tile nt in neigh) {
        //            if (nt != null)
        //                heightvalue += nt.Elevation;
        //        }
        //        curr.Elevation += heightvalue / neigh.Length;
        //        curr.Elevation /= 2;
        //    }
        //}
        Debug.Log("Made coast: " + border.Count);
        return true;
    }

}
