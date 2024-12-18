﻿using Andja.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Utility {

    public static class Util {
        private static Texture2D _whiteTexture;

        public static Texture2D WhiteTexture {
            get {
                if (_whiteTexture == null) {
                    _whiteTexture = new Texture2D(1, 1);
                    _whiteTexture.SetPixel(0, 0, Color.white);
                    _whiteTexture.Apply();
                }

                return _whiteTexture;
            }
        }

        public static void DrawScreenRect(Rect rect, Color color) {
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTexture);
            GUI.color = Color.white;
        }

        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color) {
            // Top
            Util.DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            // Left
            Util.DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            // Right
            Util.DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
            // Bottom
            Util.DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        }

        public static List<Tile> CalculateRangeTiles(int range, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            offset_x += (float)centerWidth / 2f;
            offset_y += (float)centerHeight / 2f;
            if (centerWidth != centerHeight)
                return CalculateMidPointEllipseFill(range, range, centerWidth, centerHeight, offset_x, offset_y);
            else
                return CalculateCircleTiles(range, centerWidth, centerHeight, offset_x, offset_y);
        }

        public static List<Vector2> CalculateRangeTilesVector2(int range, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            offset_x += (float)centerWidth / 2f;
            offset_y += (float)centerHeight / 2f;
            if (centerWidth != centerHeight)
                return new List<Vector2>(CalculateMidPointEllipseFillVector2(range, range, centerWidth, centerHeight, offset_x, offset_y));
            else
                return CalculateCircleTilesVector2(range, centerWidth, centerHeight, offset_x, offset_y);
        }

        /// <summary>
        /// World Tiles ONLY!
        /// Center Tiles will be empty!
        /// </summary>
        /// <param name="range"></param>
        /// <param name="centerWidth"></param>
        /// <param name="centerHeight"></param>
        /// <returns></returns>
        public static List<Tile> CalculateCircleTiles(int range, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            if (range == 0) {
                return new List<Tile>();
            }
            if (centerWidth % 2 == 0 && centerHeight % 2 == 0) {
                return new List<Tile>(CalculateEvenCirclesTile(range, centerWidth, centerHeight, offset_x, offset_y));
            }
            return new List<Tile>(CalculateMidPointCircle(range, centerWidth, centerHeight, offset_x, offset_y));
        }

        public static List<Vector2> CalculateCircleTilesVector2(int range, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            if (range == 0) {
                return new List<Vector2>();
            }
            if (centerWidth % 2 == 0 && centerHeight % 2 == 0) {
                return new List<Vector2>(CalculateEvenCirclesTileVector2(range, centerWidth, centerHeight, offset_x, offset_y));
            }
            return new List<Vector2>(CalculateMidPointCircleVector2(range, centerWidth, centerHeight, offset_x, offset_y));
        }

        public static List<Tile> CalculateSquareTiles(int range, int centerWidth = 0, int centerHeight = 0) {
            return CalculateRectangleTiles(range, range, centerWidth, centerHeight);
        }

        public static List<Tile> CalculateRectangleTiles(int Width, int Height,
                                        int centerWidth = 0, int centerHeight = 0, int offset_x = 0, int offset_y = 0) {
            List<Tile> RectangleTiles = new List<Tile>();
            if (offset_x > 0)
                offset_x -= Width / 2;
            if (offset_y > 0)
                offset_y -= Height / 2;
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (x > 0 && y > 0
                        && y >= Mathf.CeilToInt(Height / 2 - centerHeight / 2)
                        && y < Mathf.CeilToInt(Height / 2 + centerHeight / 2)
                        && x >= Mathf.CeilToInt(Width / 2 - centerWidth / 2)
                        && x < Mathf.CeilToInt(Width / 2 + centerWidth / 2))
                        continue;
                    RectangleTiles.Add(World.Current.GetTileAt(x + offset_x, y + offset_y));
                }
            }
            return RectangleTiles;
        }

        public static List<Vector2> CalculateRectangleTilesVector2(int Width, int Height, int centerWidth = 0,
                                                            int centerHeight = 0, int offset_x = 0, int offset_y = 0) {
            if (Width == 0 || Height == 0) {
                Debug.LogWarning("Calculate Rectangle Vectors with no size!");
            }
            List<Vector2> RectangleTiles = new List<Vector2>();
            Vector2 offset = new Vector2(offset_x, offset_y);
            if (offset.sqrMagnitude > 0) {
                offset -= new Vector2(Width, Height) / 2;
            }
            if (Width < 0) {
                offset_x += Width;
                Width = Mathf.Abs(Width);
            }
            if (Height < 0) {
                offset_y += Height;
                Height = Mathf.Abs(Height);
            }
            for (int x = 0; x < Width; x++) {
                for (int y = 0; y < Height; y++) {
                    if (x > 0 && y > 0
                        && y >= Mathf.CeilToInt(Height / 2 - centerHeight / 2)
                        && y < Mathf.CeilToInt(Height / 2 + centerHeight / 2)
                        && x >= Mathf.CeilToInt(Width / 2 - centerWidth / 2)
                        && x < Mathf.CeilToInt(Width / 2 + centerWidth / 2))
                        continue;
                    RectangleTiles.Add(new Vector2(x, y) + offset);
                }
            }
            return RectangleTiles;
        }

        /// <summary>
        /// WARNING! It only works when World Tiles ARE populated AND are the Tiles that you want.
        /// Offset will be the position instead of starting 0:0
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="centerWidth"></param>
        /// <param name="centerHeight"></param>
        /// <returns></returns>
        public static HashSet<Tile> CalculateMidPointCircle(float radius, float centerWidth, float centerHeight, float offset_x = 0, float offset_y = 0) {
            HashSet<Tile> tiles = new HashSet<Tile>();
            float center_x = radius;
            float center_y = radius;
            float P = (5 - radius * 4) / 4;
            float circle_x = 0;
            float circle_y = radius;
            do {
                //Fill the circle
                for (float actual_x = center_x - circle_x; actual_x <= center_x + circle_x; actual_x++) {
                    //-----
                    float actual_y = center_y + circle_y;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
                    //-----
                    actual_y = center_y - circle_y;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
                }
                for (float actual_x = center_x - circle_y; actual_x <= center_x + circle_y; actual_x++) {
                    //-----
                    float actual_y = center_y + circle_x;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
                    //-----
                    actual_y = center_y - circle_x;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
                }
                if (P < 0) {
                    P += 2 * circle_x + 1;
                }
                else {
                    P += 2 * (circle_x - circle_y) + 1;
                    circle_y--;
                }
                circle_x++;
            } while (circle_x <= circle_y);
            return tiles;
        }

        public static HashSet<Tile> CalculateEvenCirclesTile(float radius, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            HashSet<Tile> tiles = new HashSet<Tile>();
            for (float x = -radius + 0.5f; x <= radius - 0.5f; x++) {
                for (float y = -radius + 0.5f; y <= radius - 0.5f; y++) {
                    if (-centerWidth / 2 <= x && centerWidth / 2 >= x && -centerHeight / 2 <= y && centerHeight / 2 >= y) {
                        continue;
                    }
                    if (CircleDistance(x, y, 1) <= radius - 1f) {
                        tiles.Add(World.Current.GetTileAt(x + offset_x + radius, y + offset_y + radius));
                    }
                }
            }
            return tiles;
        }

        public static HashSet<Vector2> CalculateEvenCirclesTileVector2(float radius, int centerWidth, int centerHeight, float offset_x = 0, float offset_y = 0) {
            HashSet<Vector2> vectors = new HashSet<Vector2>();
            for (float x = -radius + 0.5f; x <= radius - 0.5f; x++) {
                for (float y = -radius + 0.5f; y <= radius - 0.5f; y++) {
                    if (-centerWidth / 2 <= x && centerWidth / 2 >= x && -centerHeight / 2 <= y && centerHeight / 2 >= y) {
                        continue;
                    }
                    if (CircleDistance(x, y, 1) <= radius - 1f) {
                        vectors.Add(new Vector2(x + offset_x, y + offset_y));
                    }
                }
            }
            return vectors;
        }

        private static float CircleDistance(float x, float y, float ratio) {
            return Mathf.FloorToInt(Mathf.Sqrt((Mathf.Pow(y * ratio, 2)) + Mathf.Pow(x, 2)));
        }

        /// <summary>
        /// Circle at offset and center will be not in it!
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="centerWidth"></param>
        /// <param name="centerHeight"></param>
        /// <param name="offset_x"></param>
        /// <param name="offset_y"></param>
        /// <returns></returns>
        public static HashSet<Vector2> CalculateMidPointCircleVector2(int radius, int centerWidth, int centerHeight, float offset_x, float offset_y) {
            HashSet<Vector2> circleVectors = new HashSet<Vector2>();
            int center_x = radius;
            int center_y = radius;
            int P = (5 - radius * 4) / 4;
            int circle_x = 0;
            int circle_y = radius;
            Vector2 offset = new Vector2(offset_x, offset_y);
            //if (offset.sqrMagnitude > 0) {
            //    offset -= new Vector2(radius, radius);
            //}
            do {
                //Fill the circle
                for (int actual_x = center_x - circle_x; actual_x <= center_x + circle_x; actual_x++) {
                    //-----
                    int actual_y = center_y + circle_y;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        circleVectors.Add(new Vector2(actual_x, actual_y) + offset);
                    //-----
                    actual_y = center_y - circle_y;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        circleVectors.Add(new Vector2(actual_x, actual_y) + offset);
                }
                for (int actual_x = center_x - circle_y; actual_x <= center_x + circle_y; actual_x++) {
                    //-----
                    int actual_y = center_y + circle_x;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        circleVectors.Add(new Vector2(actual_x, actual_y) + offset);
                    //-----
                    actual_y = center_y - circle_x;
                    if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                        circleVectors.Add(new Vector2(actual_x, actual_y) + offset);
                }
                if (P < 0) {
                    P += 2 * circle_x + 1;
                }
                else {
                    P += 2 * (circle_x - circle_y) + 1;
                    circle_y--;
                }
                circle_x++;
            } while (circle_x <= circle_y);
            return circleVectors;
        }

        //private static bool CircleCheck(int radius, int center, int actual) {
        //    return (center > 0 && actual >= radius - center / 2f && actual < radius + center / 2f) == false;
        //}

        private static bool CircleCheck(float radius, float center, float actual) {
            return (center > 0 && actual >= radius - center / 2 && actual < radius + center / 2) == false;
        }

        public static List<Tile> CalculateMidPointEllipse(float radius_x, float radius_y, float center_x, float center_y) {
            List<Tile> tiles = new List<Tile>();
            HashSet<Vector2> vecs = CalculateMidPointEllipseVector2(radius_x, radius_y, center_x, center_y);
            foreach (Vector2 v in vecs) {
                tiles.Add(World.Current.GetTileAt(v + new Vector2(radius_x, radius_y)));
            }
            return tiles;
        }

        public static HashSet<Vector2> CalculateMidPointEllipseVector2(float radius_x, float radius_y, float center_x, float center_y) {
            HashSet<Vector2> ellipseVectors = new HashSet<Vector2>();
            radius_x -= 0.5f;
            radius_y -= 0.5f;
            float dx, dy, d1, d2;
            float e_x = 0.5f;
            float e_y = radius_y + 0.5f;

            // Initial decision parameter of region 1
            d1 = (radius_y * radius_y) - (radius_x * radius_x * radius_y) + (0.25f * radius_x * radius_x);
            dx = 2 * radius_y * radius_y * e_x;
            dy = 2 * radius_x * radius_x * e_y;

            // For region 1
            while (dx < dy) {
                ellipseVectors.Add(new Vector2(e_x + center_x, e_y + center_y));
                ellipseVectors.Add(new Vector2(-e_x + center_x, e_y + center_y));
                ellipseVectors.Add(new Vector2(e_x + center_x, -e_y + center_y));
                ellipseVectors.Add(new Vector2(-e_x + center_x, -e_y + center_y));

                // Checking and updating value of
                // decision parameter based on algorithm
                if (d1 < 0) {
                    e_x++;
                    dx = dx + (2 * radius_y * radius_y);
                    d1 = d1 + dx + (radius_y * radius_y);
                }
                else {
                    e_x++;
                    e_y--;
                    dx = dx + (2 * radius_y * radius_y);
                    dy = dy - (2 * radius_x * radius_x);
                    d1 = d1 + dx - dy + (radius_y * radius_y);
                }
            }

            // Decision parameter of region 2
            d2 = ((radius_y * radius_y) * ((e_x + 0.5f) * (e_x + 0.5f)))
                + ((radius_x * radius_x) * ((e_y - 1) * (e_y - 1)))
                - (radius_x * radius_x * radius_y * radius_y);

            // Plotting points of region 2
            while (e_y >= 0) {
                ellipseVectors.Add(new Vector2(e_x + center_x, e_y + center_y));
                ellipseVectors.Add(new Vector2(-e_x + center_x, e_y + center_y));
                ellipseVectors.Add(new Vector2(e_x + center_x, -e_y + center_y));
                ellipseVectors.Add(new Vector2(-e_x + center_x, -e_y + center_y));

                // Checking and updating parameter
                // value based on algorithm
                if (d2 > 0) {
                    e_y--;
                    dy = dy - (2 * radius_x * radius_x);
                    d2 = d2 + (radius_x * radius_x) - dy;
                }
                else {
                    e_y--;
                    e_x++;
                    dx = dx + (2 * radius_y * radius_y);
                    dy = dy - (2 * radius_x * radius_x);
                    d2 = d2 + dx - dy + (radius_x * radius_x);
                }
            }
            return ellipseVectors;
        }

        public static List<Tile> CalculateMidPointEllipseFill(float radius_x, float radius_y, float centerWidth, float centerHeight, float center_x, float center_y) {
            List<Tile> tiles = new List<Tile>();
            HashSet<Vector2> vecs = CalculateMidPointEllipseFillVector2(radius_x, radius_y, centerWidth, centerHeight, center_x, center_y);
            foreach (Vector2 v in vecs) {
                tiles.Add(World.Current.GetTileAt(v + new Vector2(radius_x, radius_y)));
            }
            tiles.RemoveAll(x => x == null);
            return tiles;
        }

        public static HashSet<Vector2> CalculateMidPointEllipseFillVector2(float radius_x, float radius_y, float centerWidth, float centerHeight, float offset_x, float offset_y) {
            HashSet<Vector2> ellipseVectors = new HashSet<Vector2>();
            float r_x = centerWidth % 2 == 0 ? 0.5f : 0;
            float r_y = centerHeight % 2 == 0 ? 0.5f : 0;

            float ratio = radius_x / radius_y;
            for (float x = -radius_x + r_x; x <= radius_x - r_x; x++) {
                for (float y = -radius_y + r_y; y <= radius_y - r_y; y++) {
                    if (-centerWidth / 2 <= x && centerWidth / 2 >= x && -centerHeight / 2 <= y && centerHeight / 2 >= y) {
                        continue;
                    }
                    if (CircleDistance(x, y, ratio) <= radius_x - (r_x + r_y)) {
                        ellipseVectors.Add(new Vector2(x + offset_x, y + offset_y));
                    }
                }
            }
            return ellipseVectors;
        }
        /// <summary>
        /// Checks the line between the two points if there is only ocean it will return true
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static bool CheckLine(bool[][] worldTilemap, Vector2 first, Vector2 second) {
            return CheckLine(worldTilemap, Mathf.RoundToInt(first.x), Mathf.RoundToInt(first.y), 
                                  Mathf.RoundToInt(second.x), Mathf.RoundToInt(second.y));
        }
        /// <summary>
        /// Based on Bresenham Line Drawing Algorithm
        /// Checks the line between the two points if there is only ocean it will return true
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x2"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static bool CheckLine(bool[][] worldTilemap, int x, int y, int x2, int y2) {
            if(x < 0 || y < 0 || x2 < 0 || y2 < 0) {
                Debug.LogError("Check Line was lower out of bounds.");
                return false;
            }
            if (x >= worldTilemap.Length || y >= worldTilemap[0].Length || 
                x2 >= worldTilemap.Length || y2 >= worldTilemap[0].Length) {
                Debug.LogError("Check Line was upper out of bounds.");
                return false;
            }
            int w = x2 - x;
            int h = y2 - y;
            int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
            if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
            if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
            if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
            int longest = Mathf.Abs(w);
            int shortest = Mathf.Abs(h);
            if (!(longest > shortest)) {
                longest = Mathf.Abs(h);
                shortest = Mathf.Abs(w);
                if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
                dx2 = 0;
            }
            int numerator = longest >> 1;
            for (int i = 0; i <= longest; i++) {
                if (worldTilemap[x][y] == false || Pathfinding.PathfindingThreadHandler.FindPaths == false)
                    return false;
                if(x > worldTilemap.Length || x < 0) {
                    return false;
                }
                if (y > worldTilemap[0].Length || y < 0) {
                    return false;
                }
                numerator += shortest;
                if (!(numerator < longest)) {
                    numerator -= longest;
                    if (Mathf.Abs(dx1) + Mathf.Abs(dy1) == 2) {
                        if (x + dx1 >= 0 && x + dx1 < worldTilemap.Length) {
                            if (worldTilemap[x + dx1][y] == false) {
                                return false;
                            }
                        }
                        if (y + dy1 >= 0 && y + dy1 < worldTilemap[0].Length) {
                            if (worldTilemap[x][y + dy1] == false) {
                                return false;
                            }
                        }
                    }
                    x += dx1;
                    y += dy1;
                }
                else {
                    if (Mathf.Abs(dx2) + Mathf.Abs(dy2) == 2) {
                        if(x + dx2 >= 0 && x + dx2 < worldTilemap.Length) {
                            if (worldTilemap[x + dx2][y] == false) {
                                return false;
                            }
                        }
                        if (y + dy2 >= 0 && y + dy2 < worldTilemap[0].Length) {
                            if (worldTilemap[x][y + dy2] == false) {
                                return false;
                            }
                        }
                    }
                    x += dx2;
                    y += dy2;
                }
            }
            return true;
        }
        public static Vector2[] FindClosestPoints(IEnumerable<Vector2> seq1, IEnumerable<Vector2> seq2) {
            double closest = double.MaxValue;
            Vector2[] result = null;
            foreach (Vector2 p1 in seq1) {
                foreach (Vector2 p2 in seq2) {
                    float dx = p1.x - p2.x;
                    float dy = p1.y - p2.y;
                    float distance = dx * dx + dy * dy;
                    if (distance >= closest)
                        continue;
                    result = new Vector2[] { p1, p2 };
                    closest = distance;
                }
            }
            return result;
        }
        public static Vector2[] FindClosestPoints(IEnumerable<Vector2> seq1, params Vector2[] seq2) {
            return FindClosestPoints(seq1, (IEnumerable<Vector2>)seq2);
        }

        public static float FindClosestDistancePointCircle(Vector2 point, Vector2 circleCenter, float circleRadius) {
            return (Mathf.Sqrt((Mathf.Pow((point.x - circleCenter.x), 2)) + (Mathf.Pow((point.y - circleCenter.y), 2))) - circleRadius);
        }


    }
}
