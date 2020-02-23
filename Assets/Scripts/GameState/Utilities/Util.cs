using System.Collections.Generic;
using UnityEngine;

public static class Util  {
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
    
    public static List<Tile> CalculateCircleTiles(int range, int centerWidth, int centerHeight) {
        List<Tile> CircleTiles = new List<Tile>();
        if (range == 0) {
            return CircleTiles;
        }
        CircleTiles = new List<Tile>(MidPointCircleCalculation(range,centerWidth,centerHeight));
        //for (int width = 0; width < centerWidth; width++) {
        //    CircleTiles.Remove(World.Current.GetTileAt(range + width, range));
        //    for (int height = 1; height < centerHeight; height++) {
        //        CircleTiles.Remove(World.Current.GetTileAt(range + width, range + height));
        //    }
        //}
        return CircleTiles;
    }
    public static List<Tile> CalculateSquareTiles(int range, int centerWidth, int centerHeight) {
        return CalculateRectangleTiles(range, range, centerWidth, centerHeight);
    }
    public static List<Tile> CalculateRectangleTiles(int Width, int Height, int centerWidth, int centerHeight) {
        List<Tile> RectangleTiles = new List<Tile>();
        Tile firstTile = World.Current.GetTileAt(0, 0);
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                RectangleTiles.Add(World.Current.GetTileAt(x, y));
            }
        }
        for (int width = 0; width < centerWidth; width++) {
            RectangleTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y));
            for (int height = 1; height < centerHeight; height++) {
                RectangleTiles.Remove(World.Current.GetTileAt(firstTile.X + width, firstTile.Y + height));
            }
        }
        return RectangleTiles;
    }
    public static HashSet<Tile> MidPointCircleCalculation(int radius, int centerWidth, int centerHeight) {
        HashSet<Tile> tiles = new HashSet<Tile>();
        int center_x = radius;
        int center_y = radius;
        int P = (5 - radius * 4) / 4;
        int circle_x = 0;
        int circle_y = radius;
        do {
            //Fill the circle 
            for (int actual_x = center_x - circle_x; actual_x <= center_x + circle_x; actual_x++) {
                int actual_y = center_y + circle_y;
                if (CheckX(radius, centerWidth, actual_x) || CheckY(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x, actual_y));

                actual_y = center_y - circle_y;
                if (CheckX(radius, centerWidth, actual_x) || CheckY(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x, actual_y));
            }
            for (int actual_x = center_x - circle_y; actual_x <= center_x + circle_y; actual_x++) {
                int actual_y = center_y + circle_x;
                if (CheckX(radius, centerWidth, actual_x) || CheckY(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x, actual_y));
                actual_y = center_y - circle_x;
                if (CheckX(radius, centerWidth, actual_x) || CheckY(radius, centerHeight, actual_y))
                    tiles.Add(World.Current.GetTileAt(actual_x, actual_y));
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

    private static bool CheckY(int radius, int centerHeight, int actual_y) {
        return (centerHeight > 0 && actual_y >= radius && actual_y < radius + centerHeight) == false;
    }

    private static bool CheckX(int radius, int centerWidth, int actual_x) {
        return (centerWidth > 0 && actual_x >= radius && actual_x < radius + centerWidth) == false;
    }
}
