using System.Collections.Generic;
using UnityEngine;

public static class Util {
    static Texture2D _whiteTexture;
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
    /// <summary>
    /// World Tiles ONLY!
    /// Center Tiles will be empty!
    /// </summary>
    /// <param name="range"></param>
    /// <param name="centerWidth"></param>
    /// <param name="centerHeight"></param>
    /// <returns></returns>
    public static List<Tile> CalculateCircleTiles(int range, int centerWidth, int centerHeight, int offset_x = 0, int offset_y = 0) {
        List<Tile> CircleTiles = new List<Tile>();
        if (range == 0) {
            return CircleTiles;
        }
        CircleTiles = new List<Tile>(CalculateMidPointCircle(range, centerWidth, centerHeight, offset_x, offset_y));
        return CircleTiles;
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
        if (Width == 0||Height == 0) {
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
    public static HashSet<Tile> CalculateMidPointCircle(int radius, int centerWidth, int centerHeight, int offset_x = 0, int offset_y = 0) {
        HashSet<Tile> tiles = new HashSet<Tile>();
        int center_x = radius;
        int center_y = radius;
        int P = (5 - radius * 4) / 4;
        int circle_x = 0;
        int circle_y = radius;
        if(offset_x > 0)
            offset_x -= radius;
        if (offset_y > 0)
            offset_y -= radius;
        do {
            //Fill the circle 
            for (int actual_x = center_x - circle_x; actual_x <= center_x + circle_x; actual_x++) {
                //-----
                int actual_y = center_y + circle_y;
                if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
                //-----
                actual_y = center_y - circle_y;
                if (CircleCheck(radius, centerWidth, actual_x) || CircleCheck(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x + offset_x, actual_y + offset_y));
            }
            for (int actual_x = center_x - circle_y; actual_x <= center_x + circle_y; actual_x++) {
                //-----
                int actual_y = center_y + circle_x;
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
    /// <summary>
    /// Circle at offset and center will be not in it!
    /// </summary>
    /// <param name="radius"></param>
    /// <param name="centerWidth"></param>
    /// <param name="centerHeight"></param>
    /// <param name="offset_x"></param>
    /// <param name="offset_y"></param>
    /// <returns></returns>
    public static HashSet<Vector2> CalculateMidPointCircleVector2(int radius, int centerWidth, int centerHeight, int offset_x, int offset_y) {
        HashSet<Vector2> circleVectors = new HashSet<Vector2>();
        int center_x = radius;
        int center_y = radius;
        int P = (5 - radius * 4) / 4;
        int circle_x = 0;
        int circle_y = radius;
        Vector2 offset = new Vector2(offset_x, offset_y);
        if (offset.sqrMagnitude > 0 ) {
            offset -= new Vector2(radius, radius);
        }
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
    private static bool CircleCheck(int radius, int centerHeight, int actual_y) {
        return (centerHeight > 0 && actual_y >= radius && actual_y < radius + centerHeight) == false;
    }
    public static List<Tile> CalculateMidPointEllipse(float radius_x, float radius_y, float center_x, float center_y) {
        List<Tile> tiles = new List<Tile>();
        HashSet<Vector2> vecs = CalculateMidPointEllipseVector2(radius_x, radius_y, center_x, center_y);
        foreach(Vector2 v in vecs) {
            tiles.Add(World.Current.GetTileAt(v));
        }
        return tiles;
    }
    public static HashSet<Vector2> CalculateMidPointEllipseVector2(float radius_x, float radius_y, float center_x, float center_y) {
        HashSet<Vector2> ellipseVectors = new HashSet<Vector2>();
        float dx, dy, d1, d2; 
        float e_x = 0;
        float e_y = radius_y;

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
    public static List<Tile> CalculateMidPointEllipseFill(int radius_x, int radius_y, int center_x, int center_y) {
        List<Tile> tiles = new List<Tile>();
        HashSet<Vector2> vecs = CalculateMidPointEllipseFillVector2(radius_x, radius_y, center_x, center_y);
        foreach (Vector2 v in vecs) {
            tiles.Add(World.Current.GetTileAt(v));
        }
        tiles.RemoveAll(x => x == null);
        return tiles;
    }
    public static HashSet<Vector2> CalculateMidPointEllipseFillVector2(int width, int height, int center_x, int center_y) {
        HashSet<Vector2> ellipseVectors = new HashSet<Vector2>();
        //INEFFICIENT ALGORITHMUS
        //TODO: make it with midpoint algo
        int hh = height * height;
        int ww = width * width;
        int hhww = hh * ww;
        int x0 = width;
        int dx = 0;

        // do the horizontal diameter
        for (int x = -width; x <= width; x++)
            ellipseVectors.Add(new Vector2(center_x + x, center_y));

        // now do both halves at the same time, away from the diameter
        for (int y = 1; y <= height; y++) {
            int x1 = x0 - (dx - 1);  // try slopes of dx - 1 or more
            for (; x1 > 0; x1--)
                if (x1 * x1 * hh + y * y * ww <= hhww)
                    break;
            dx = x0 - x1;  // current approximation of the slope
            x0 = x1;

            for (int x = -x0; x <= x0; x++) {
                ellipseVectors.Add(new Vector2(center_x + x, center_y - y));
                ellipseVectors.Add(new Vector2(center_x + x, center_y + y));
            }
        }

        return ellipseVectors;
    }

}
