using Andja.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model.Generator {

    public class HeightGenerator {

        public static float[,] Generate(int Width, int Height, ThreadRandom Random, float maxProgress, ref float Progress) {
            FastNoise noise = new FastNoise(Random.Range(0, int.MaxValue));
            noise.SetFrequency(0.01f);
            noise.SetFractalGain(1);
            FastNoise noise2 = new FastNoise(Random.Range(0, int.MaxValue));
            noise2.SetFrequency(0.05f);
            noise2.SetCellularDistanceFunction(FastNoise.CellularDistanceFunction.Natural); ;
            noise2.SetCellularReturnType(FastNoise.CellularReturnType.Distance);
            noise2.SetCellularNoiseLookup(noise);
            FastNoise noise3 = new FastNoise(Random.Range(0, int.MaxValue));
            noise3.SetFrequency(0.01f);
            noise3.SetFractalGain(1);
            FastNoise noise4 = new FastNoise(noise3.GetSeed());
            noise4.SetFrequency(1);
            noise4.SetFractalGain(1);
            Progress += 0.1f * maxProgress;
            //Width = Random.Range(100, 500);
            //Height = Random.Range(Width/2, Width + Width/2);

            //fractal.SetFractalType(FastNoise.FractalType.FBM);
            float[,] values = new float[Width, Height];
            float tileWidth = (Width * 0.05f);
            float tileHeight = (Height * 0.05f);
            float centerX = (float)(Width / 2); //- random.RangeFloat(-5f, 5f);
            float centerY = (float)(Height / 2);// -random.RangeFloat(-5f, 5f);
            float innerHeight = Height * 0.9f;
            float innerWidth = Width * 0.9f;
            float innerX = Height * 0.2f;
            float innerY = Width * 0.2f;
            float maxRWidth = Width * 0.05f;
            float maxRHeight = Height * 0.05f;
            Progress += 0.05f * maxProgress;
            List<RandomPoint> randomPoints = new List<RandomPoint>();
            for (int i = 0; i < Random.Range(0, 11); i++) {
                float x = Random.Range(innerX, innerWidth);
                float y = Random.Range(innerY, innerHeight);
                float rWidth = Random.Range(Width * 0.01f, Mathf.Clamp(innerWidth - x, 0, maxRWidth));
                float rHeight = Random.Range(Height * 0.01f, Mathf.Clamp(innerHeight - y, 0, maxRHeight));
                RandomPoint point = new RandomPoint() {
                    x = x,
                    y = y,
                    Width = rWidth,
                    Height = rHeight
                };
                randomPoints.Add(point);
            }
            Progress += 0.05f * maxProgress;
            int squaresX = Mathf.RoundToInt(Width / tileWidth);
            int squaresY = Mathf.RoundToInt(Height / tileHeight);
            int innerSquares = (squaresX - 1) * (squaresY - 1);
            float[,] squares = new float[squaresX, squaresY];
            int sx = Random.Range((squaresX / 2) - 1, (squaresX / 2) + 1);
            int sy = Random.Range((squaresY / 2) - 1, (squaresY / 2) + 1);
            squares[sx, sy] = 1f;
            int scount = 1;
            for (int c = 0; c < scount; c++) {
                for (int x = 1; x < squaresX - 1; x++) {
                    for (int y = 1; y < squaresY - 1; y++) {
                        if (Random.Range(0, 1f) > 0.8f - (x / (squaresX / 2) + y / (squaresY / 2))) {
                            squares[x, y] = ((x / (squaresX / 2) + y / (squaresY / 2)) / scount);
                        }
                    }
                }
            }
            for (int x = 0; x < squaresX; x++) {
                for (int y = 0; y < squaresY; y++) {
                    float count = 0;
                    for (int i = -1; i <= 1; i++) {
                        for (int j = -1; j <= 1; j++) {
                            int nx = x + j;
                            int ny = y + i;
                            if (nx < 0 || ny < 0)
                                continue;
                            if (nx >= squaresX || ny >= squaresY)
                                continue;
                            if (squares[ny, nx] > 0) {
                                count++;
                            }
                        }
                    }
                    if (Random.Range(0, 1f) > count / 8f) {
                        squares[y, x] = 0;
                    }
                }
            }
            Progress += 0.05f * maxProgress;
            float wPow = Mathf.Pow(centerX, 2);
            float hPow = Mathf.Pow(centerY, 2);
            for (float x = 0; x < Width; x++) {
                for (float y = 0; y < Height; y++) {
                    int ssx = Mathf.FloorToInt(x / tileWidth);
                    int ssy = Mathf.FloorToInt(y / tileHeight);
                    float val = squares[ssx, ssy];

                    float ny = y / Height - 0.5f;
                    float nx = x / Width - 0.5f;

                    float cel = 0.4f * (noise2.GetCellular(x, y) + 1f);
                    float e = (noise.GetPerlin(x, y) + 1f) / 2 + cel / 2;
                    e = e / 2;
                    values[(int)x, (int)y] = (float)Mathf.Pow(e, 0.45f);
                    float maxSquareDistance = 1f + Random.Range(-0.05f, 0.05f);
                    float d = Mathf.Clamp(
                        (1 - (2 * (x / Width) - 1) * (2 * (x / Width) - 1)) * (1 - (2 * (y / Height) - 1) * (2 * (y / Height) - 1)), 0.1f, maxSquareDistance);
                    d += 1 - 2 * Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                    //d += 1 - (Mathf.Pow(x - centerX, 2) / wPow + Mathf.Pow(y - centerY, 2) / hPow);

                    d += val * 1f * Mathf.Clamp(noise3.GetValue(x, y), -0.5f, 1);
                    d /= 2;
                    float local = 0;
                    foreach (RandomPoint rp in randomPoints) {
                        float distanceX = Mathf.Abs(rp.x - x);
                        float distanceY = Mathf.Abs(rp.y - y);
                        if (distanceX > rp.Width || distanceY > rp.Height)
                            continue;
                        local += 1 - ((Mathf.Clamp01(Mathf.Pow(x - rp.x, 2) / Mathf.Pow(rp.Width, 2) + Mathf.Pow(y - rp.y, 2) / Mathf.Pow(rp.Height, 2)))
                                        + (noise.GetValueFractal(x, y) + 1f) / 2) / 2;
                    }
                    if (local > 0) {
                        local = Mathf.Pow(local, 0.7f);
                        d += local;
                    }
                    d = Mathf.Pow(d, 0.3f);

                    values[(int)x, (int)y] = (1 - values[(int)x, (int)y] + d) / 2f;
                }
            }
            Progress += 0.2f * maxProgress;
            for (int c = 0; c < 2; c++) {
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        float val = values[x, y];
                        float count = 0;
                        for (int i = -1; i <= 1; i++) {
                            for (int j = -1; j <= 1; j++) {
                                int nx = x + j;
                                int ny = y + i;
                                if (nx < 0 || ny < 0)
                                    continue;
                                if (nx >= Width || ny >= Height)
                                    continue;
                                if (values[nx, ny] > 0.5f) {
                                    count++;
                                }
                            }
                        }
                        if (Random.Range(0, 1f) > count / 8f) {
                            values[x, y] = 0;
                            for (int i = -1; i <= 1; i++) {
                                for (int j = -1; j <= 1; j++) {
                                    int nx = x + j;
                                    int ny = y + i;
                                    if (nx < 0 || ny < 0)
                                        continue;
                                    if (nx >= Width || ny >= Height)
                                        continue;
                                    values[nx, ny] -= 0.02f;
                                }
                            }
                        }
                    }
                }
            }
            Progress += 0.2f * maxProgress;
            for (int c = 0; c < 2; c++) {
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        float sum = 0;
                        for (int i = -1; i <= 1; i++) {
                            for (int j = -1; j <= 1; j++) {
                                int nx = x + j;
                                int ny = y + i;
                                if (nx < 0 || ny < 0)
                                    continue;
                                if (nx >= Width || ny >= Height)
                                    continue;
                                sum += values[nx, ny];
                            }
                        }
                        sum /= 9;
                        values[x, y] = sum;
                    }
                }
            }
            int landCount = 0;
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    if (values[x, y] > IslandGenerator.landThreshold) {
                        landCount++;
                    }
                }
            }
            if ((float)landCount / ((float)Height * Width) < 0.25f) {
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        if (values[x, y] < 0 || values[x, y] > IslandGenerator.landThreshold)
                            continue;
                        values[x, y] = Mathf.Abs(values[x, y]) * 1.15f;
                    }
                }
            }
            int tlandCount = 0;
            for (int y = 0; y < Height; y++) {
                for (int x = 0; x < Width; x++) {
                    if (values[x, y] > IslandGenerator.landThreshold) {
                        tlandCount++;
                        if (values[x, y] < IslandGenerator.mountainElevation) {
                            values[x, y] = Mathf.Clamp(values[x, y] * (1 + noise3.GetWhiteNoise(x, y) * 0.0075f), IslandGenerator.landThreshold, 1);
                        }
                    }
                }
            }
            //if((float)tlandCount / landCount > 1)
            //    Debug.Log("More land " + ((float)tlandCount / landCount) + " " + tlandCount + " " + landCount);
            Progress += 0.125f * maxProgress;
            IslandOceanFloodFill(Width, Height, values);
            Progress += 0.1f * maxProgress;
            MakeShore(Width, Height, Random, values);
            Progress += 0.125f * maxProgress;
            return values;
        }

        private static void IslandOceanFloodFill(int Width, int Height, float[,] heights) {
            fillMatrix2(Width, Height, heights, 0, 0);
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

        private static void fillMatrix2(int Width, int Height, float[,] heights, int row, int col) {
            Stack<Point> fillStack = new Stack<Point>();
            bool[,] alreadyChecked = new bool[Width, Height];
            fillStack.Push(new Point(row, col));
            while (fillStack.Count > 0) {
                Point cords = fillStack.Pop();
                if (cords.X < 0 || cords.X > Width - 1 || cords.Y < 0 || cords.Y > Height - 1)
                    continue;
                if (heights[cords.X, cords.Y] == 0)
                    continue;
                if (heights[cords.X, cords.Y] > IslandGenerator.landThreshold)
                    continue;
                heights[cords.X, cords.Y] = 0;
                alreadyChecked[cords.X, cords.Y] = true;
                fillStack.Push(new Point(cords.X + 1, cords.Y));
                fillStack.Push(new Point(cords.X - 1, cords.Y));
                fillStack.Push(new Point(cords.X, cords.Y + 1));
                fillStack.Push(new Point(cords.X, cords.Y - 1));
            }
        }

        public static bool MakeShore(int Width, int Height, ThreadRandom Random, float[,] heights) {
            int averageSize = Width + Height;
            averageSize /= 2;
            int numberOfShores = Random.Range(Mathf.RoundToInt(averageSize * 0.025f), Mathf.RoundToInt(averageSize * 0.05f) + 1);

            Stack<Point> testPoints = new Stack<Point>();
            List<Point> borderPoints = new List<Point>();
            Point f = GetBorderPoint(Width, Height, heights, Vector2.up);
            testPoints.Push(f);
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
                if (HasNeighbourOcean(Width, Height, heights, cords) == false)
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
                Texture2D texture = new Texture2D(Width, Height);
                for (int y = 0; y < Height; y++) {
                    for (int x = 0; x < Width; x++) {
                        float val = heights[x, y];
                        if (val > IslandGenerator.mountainElevation)
                            texture.SetPixel(x, y, new Color(val, 0, 0, 1));
                        else
                        if (val > IslandGenerator.landThreshold)
                            texture.SetPixel(x, y, new Color(val, val, val, 1));
                        else
                        if (val >= IslandGenerator.shoreElevation)
                            texture.SetPixel(x, y, Color.yellow);
                        else
                            texture.SetPixel(x, y, Color.black);
                        //texture.SetPixel(x, y, new Color(1 * val, 1 * val, 1 * val, 0));
                    }
                }
                texture.filterMode = FilterMode.Point;
                texture.Apply();
                byte[] bytes = texture.EncodeToPNG();
                var dirPath = "F:/SaveImages/ShoreMissing/";
                if (!System.IO.Directory.Exists(dirPath)) {
                    System.IO.Directory.CreateDirectory(dirPath);
                }
                System.IO.File.WriteAllBytes(dirPath + "Image_" + Random.Seed + ".png", bytes);
                Debug.LogError("NOT FOUND A SINGLE ISLAND BORDER! ");
                return false;
            }
            List<ShoreGen> shores = new List<ShoreGen>();
            for (int i = 0; i < numberOfShores; i++) {
                int x = 0;
                int y = 0;
                Direction direction = (Direction)Random.Range(1, 5); // 1=N -> 4=W
                int length = Random.Range(Mathf.RoundToInt(averageSize * 0.14f), Mathf.RoundToInt(averageSize * 0.20f));
                int depth = Random.Range(Mathf.CeilToInt(averageSize * 0.02f), Mathf.CeilToInt(averageSize * 0.03f));
                if (direction == Direction.S) {
                    x = Random.Range(0, Width);
                    y = 0;
                }
                if (direction == Direction.N) {
                    x = Random.Range(0, Width);
                    y = Height - 1;
                }
                if (direction == Direction.W) {
                    x = 0;
                    y = Random.Range(0, Height);
                }
                if (direction == Direction.E) {
                    x = Width - 1;
                    y = Random.Range(0, Height);
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
                    if (heights[c.X, c.Y] == IslandGenerator.shoreElevation) {
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
                if (NeigbhourCheck(Width, Height, heights, s) == false) {
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
                heights[s.X, s.Y] = IslandGenerator.shoreElevation;
            }
            foreach (Point s in shorePoints.ToArray()) {
                if (HasNeighbourLand(Width, Height, heights, s, true) == false)
                    heights[s.X, s.Y] = 0;
            }
            return true;
        }

        private static Point GetBorderPoint(int Width, int Height, float[,] heights, Vector2 dir) {
            Vector2 pos = new Vector2(Width / 2, Height / 2);
            Vector2 center = new Vector2(Width / 2, Height / 2);
            dir.Normalize();
            while ((pos + dir).IsInBounds(0, 0, Width, Height) && pos.y >= 0) {
                if (heights[(int)(pos.x + dir.x), (int)(pos.y + dir.y)] == 0 && heights[(int)(pos.x), (int)(pos.y)] > IslandGenerator.islandThreshold) {
                    break;
                }
                pos += dir;
            }
            if (heights[(int)pos.x, (int)pos.y] < IslandGenerator.islandThreshold) {
                if (dir == Vector2.up)
                    return GetBorderPoint(Width, Height, heights, Vector2.left);
                if (dir == Vector2.left)
                    return GetBorderPoint(Width, Height, heights, Vector2.down);
                if (dir == Vector2.down)
                    return GetBorderPoint(Width, Height, heights, Vector2.right);
                if (dir == Vector2.right)
                    return GetBorderPoint(Width, Height, heights, dir.Rotate(45));
                else
                if (dir == Vector2.right.Rotate(315)) {
                    Debug.Log("NO ISLAND FOUND!");
                    return new Point(Width / 2, Height / 2);
                }
            }
            if (pos == center)
                Debug.Log("stop");
            return pos;
        }

        public static bool HasNeighbourLand(int Width, int Height, float[,] heights, Point point, bool diag = false) {
            for (int y = -1; y <= 1; y++) {
                for (int x = -1; x <= 1; x++) {
                    if (x == 0 && y == 0)
                        continue;
                    if (diag == false && Mathf.Abs(x) + Mathf.Abs(y) == 2)
                        continue;
                    if (point.X + x < 0 || point.X + x > Width - 1 || point.Y - y < 0 || point.Y - y > Height - 1)
                        continue;
                    if (heights[point.X + x, point.Y - y] > IslandGenerator.shoreElevation) {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool HasNeighbourOcean(int Width, int Height, float[,] heights, Point point) {
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

        private static bool NeigbhourCheck(int Width, int Height, float[,] heights, Point c) {
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