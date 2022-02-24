using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.AI {
    public class CityGrid {
        public Block[,] CityBlocks;
        public List<Block> ValidBlocks = new List<Block>();
        public CityGrid(Island island, City city) {
            var home = PrototypController.Instance.BuildableHomeStructure;
            int width = home.TileWidth * 2 + 2;
            int height = home.TileHeight * 2 + 2;
            CityBlocks = new Block[Mathf.CeilToInt((float)island.Width / width), Mathf.CeilToInt((float)island.Height / height)];
            for (int x = 0; x < CityBlocks.GetLength(0); x ++) {
                for (int y = 0; y < CityBlocks.GetLength(1); y ++) {
                    Tile[][] tiles = new Tile[height][];
                    //int wY = (y) * height;
                    for (int i = 0; i < height; i++) {
                        tiles[i] = new Tile[width];
                        for (int j = 0; j < width; j++) {
                            tiles[i][j] = World.Current.GetTileAt(x * width + island.Minimum.x + j, 
                                                                  y * height + island.Minimum.y + i);
                        }
                        //int wX = (x+i) * width;
                        //Array.Copy(World.Current.Tiles,
                        //    ((int)island.Minimum.x + wX) * World.Current.Height + ((int)island.Minimum.y + wY), 
                        //    tiles[i], 
                        //    0, 
                        //    width
                        //);
                    }
                    CityBlocks[x, y] = new Block(tiles, width, height);
                    if (CityBlocks[x, y].Valid)
                        ValidBlocks.Add(CityBlocks[x, y]);
                }
            }
            for (int x = 0; x < CityBlocks.GetLength(0); x++) {
                for (int y = 0; y < CityBlocks.GetLength(1); y++) {
                    if (CityBlocks[x, y].Valid == false)
                        continue;
                    for (int nx = -1; nx < 2; nx++) {
                        for (int ny = -1; ny < 2; ny++) {
                            if (nx == 0 && ny == 0 || x+nx<0 || y+ny<0 || x+nx>=CityBlocks.GetLength(0) || y+ny>=CityBlocks.GetLength(1))
                                continue;
                            if(CityBlocks[x+nx, y+ny].Valid)
                                CityBlocks[x, y].Value++;
                        }
                    } 
                }
            }
            for (int x = 0; x < CityBlocks.GetLength(0); x++) {
                for (int y = 0; y < CityBlocks.GetLength(1); y++) {
                    if (CityBlocks[x, y].Valid == false)
                        continue;
                    for (int nx = -1; nx < 2; nx++) {
                        for (int ny = -1; ny < 2; ny++) {
                            if (nx == 0 && ny == 0 || x + nx < 0 || y + ny < 0 || x + nx >= CityBlocks.GetLength(0) || y + ny >= CityBlocks.GetLength(1))
                                continue;
                            if (CityBlocks[x + nx, y + ny].Value >= 8)
                                CityBlocks[x, y].Value++;
                        }
                    }
                }
            }
        }
    }

    public class Block {
        public readonly int WIDTH;
        public readonly int HEIGHT;

        public int Value;
        Tile[][] Roads;
        public Plot[] Plots;
        public bool RoadPossible;
        public int ValidPlots;
        public bool Valid => ValidPlots > 0;
        public Block(Tile[][] tiles, int width, int height) {
            WIDTH = width;
            HEIGHT = height;

            Roads = new Tile[width][];
            Plots = new Plot[4];
            for (int i = 0; i < 4; i++) {
                Plots[i] = new Plot((width - 1) / 2, (height - 1) / 2);
            }
            for (int x = 0; x < tiles.Length; x++) {
                Roads[x] = new Tile[height];
                for (int y = 0; y < tiles[x].Length; y++) {
                    if (x == 0 || x == tiles.Length - 1) {
                        if (x < width / 2f && y < height / 2f) {
                            Plots[0].WidthRoad[y] = tiles[x][y];
                        }
                        if (x >= width / 2f && y < height / 2f) {
                            Plots[1].WidthRoad[y] = tiles[x][y];
                        }
                        if (x < width / 2f && y >= height / 2f) {
                            Plots[2].WidthRoad[y - 3] = tiles[x][y];
                        }
                        if (x >= width / 2f && y >= height / 2f) {
                            Plots[3].WidthRoad[y - 3] = tiles[x][y];
                        }
                    }
                    if (y == 0 || y == tiles[x].Length - 1) {
                        if (x < width / 2f && y < height / 2f) {
                            Plots[0].HeightRoad[x] = tiles[x][y];
                        }
                        if (x >= width / 2f && y < height / 2f) {
                            Plots[1].HeightRoad[x - 3] = tiles[x][y];
                        }
                        if (x < width / 2f && y >= height / 2f) {
                            Plots[2].HeightRoad[x] = tiles[x][y];
                        }
                        if (x >= width / 2f && y >= height / 2f) {
                            Plots[3].HeightRoad[x - 3] = tiles[x][y];
                        }
                    }
                    if (x == 0 || x == tiles.Length - 1 || y == 0 || y == tiles[x].Length - 1) {
                        Roads[x][y] = tiles[x][y];
                        continue;
                    }
                    int nx = x - 1;
                    int ny = y - 1;
                    
                    // 1-2 2-2
                    // 1-1 2-1
                    if (x < width / 2f && y < height / 2f) {
                        Plots[0].Tiles[nx, ny] = tiles[x][y];
                        continue;
                    }

                    // 3-2 4-2
                    // 3-1 4-1
                    if (x >= width / 2f && y < height / 2f) {
                        Plots[1].Tiles[nx - 2, ny] = tiles[x][y];
                        continue;
                    }

                    // 1-4 2-4
                    // 1-3 2-3
                    if (x < width / 2f && y >= height / 2f) {
                        Plots[2].Tiles[nx, ny - 2] = tiles[x][y];
                        continue;
                    }

                    // 3-4 4-4
                    // 3-3 4-3
                    if (x >= width / 2f && y >= height / 2f) {
                        Plots[3].Tiles[nx - 2, ny - 2] = tiles[x][y];
                        continue;
                    }
                }
            }
            foreach (Plot item in Plots) {
                item.Check();
                if (item.Valid) {
                    ValidPlots++;
                }
            }
            RoadPossible = Roads.Count(x => x != null) == 20;
        }
    }

   public class Plot {
        public readonly int WIDTH;
        public readonly int HEIGHT;

        public bool Valid;
        public Tile[,] Tiles;
        public Tile[] WidthRoad;
        public Tile[] HeightRoad;
        public Plot(int width, int height) {
            WIDTH = width;
            HEIGHT = height;
            Tiles = new Tile[width, height];
            WidthRoad = new Tile[width + 1];
            HeightRoad = new Tile[height + 1];
        }

        public void Check() {
            for (int x = 0; x < WIDTH; x++) {
                for (int y = 0; y < HEIGHT; y++) {
                    if(Tiles[x,y].CheckTile() == false) {
                        Valid = false;
                        return;
                    }
                }
            }
            //TODO: Road checks
            Valid = true;
        }
    }

}


