using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Andja.Utility;
using System.Drawing;
using Andja.Model.Generator;

namespace Andja.Pathfinding {

    public class WorldSquares {

        public static List<Rect> rects;
        public static List<Rect> islandRects;

        public static void CalculateRects() {
            islandRects = new List<Rect>();
            rects = new List<Rect>();
            Vector2 worldMin = Vector2.zero;
            Vector2 worldMax = new Vector2(World.Current.Width, World.Current.Height);

            List<Island> islands = World.Current.Islands.OrderBy(x=>x.Minimum.x).ThenBy(y=>y.Minimum.y).ToList();
            for (int i = 0; i < islands.Count; i++) {
                islandRects.Add(new Rect(islands[i].Minimum.x, islands[i].Minimum.y, islands[i].Width, islands[i].Height));
            }
            List<DirectionalRect> directionalRects = new List<DirectionalRect>();
            foreach (Rect island in islandRects) {
                Rect Top = new Rect {
                    xMin = worldMin.x,
                    yMin = island.yMax,
                    xMax = worldMax.x,
                    yMax = worldMax.y
                };
                Rect Right = new Rect {
                    xMin = island.xMax,
                    yMin = worldMin.y,
                    xMax = worldMax.x,
                    yMax = worldMax.y
                };
                Rect Bottom = new Rect {
                    xMin = worldMin.x,
                    yMin = worldMin.y,
                    xMax = worldMax.x,
                    yMax = island.yMin
                };
                Rect Left = new Rect {
                    xMin = worldMin.x,
                    yMin = worldMin.y,
                    xMax = island.xMin,
                    yMax = worldMax.y
                };
                DirectionalRect temp = MapGenerator.GetNewRects(islandRects, Top, island, Direction.N);
                directionalRects.Add(temp);

                temp = MapGenerator.GetNewRects(islandRects, Right, island, Direction.E);
                directionalRects.Add(temp);

                temp = MapGenerator.GetNewRects(islandRects, Bottom, island, Direction.S);
                directionalRects.Add(temp);

                temp = MapGenerator.GetNewRects(islandRects, Left, island, Direction.W);
                directionalRects.Add(temp);
            }


            foreach (DirectionalRect dr in directionalRects.OrderBy(x=>x.direction)) {
                foreach (DirectionalRect or in directionalRects) {
                    if (dr == or) continue;
                    if (dr.Overlaps(or.rect))
                        or.UpdateRect(dr.rect);
                }
            }
            foreach (DirectionalRect dr in directionalRects) {
                rects.Add(dr.rect);
            }

            //int rectTiles = 100;
            //for(int y = 0; y < World.Current.Height; y++) {
            //    for (int x = 0; x < World.Current.Width; x++) {
            //        Rect r = new Rect(x, y, World.Current.Width / rectTiles, World.Current.Height / rectTiles);
            //        List<Rect> overlap = islandRects.FindAll(ir => ir.Overlaps(r));
            //        if (overlap.Count>0) {
            //            foreach(Rect o in overlap) {
            //                if(r.xMax > o.xMin && r.xMax < o.xMax) {
            //                    if(r.xMin > o.xMin) {
            //                        Rectangle.
            //                    }                               
            //                }
            //                if (r.xMax > o.xMin && r.xMax < o.xMax) {

            //                }
            //            }
            //        }
            //        rects.Add(r);
            //    }
            //}

            //int currentY = 0;
            //for (int y = 0; y < ys.Count - 1; y++) {
            //    int currentX = 0;
            //    for (int x = 0; x < xs.Count - 1; x++) {
            //        Rect rect = new Rect(currentX, currentY, xs[x + 1] - currentX, ys[y + 1] - currentY);
            //        rects.Add(rect);
            //        currentX = (int)rect.xMax;
            //    }
            //    currentY = (int)rects[0].yMax;
            //}
            //foreach(Rect r in rects.ToArray()) {
            //    foreach (Rect re in islandRects.ToArray())
            //        if(r.Overlaps(re) == false)
            //            rects.Remove(r);
            //    }
        }
    }
}