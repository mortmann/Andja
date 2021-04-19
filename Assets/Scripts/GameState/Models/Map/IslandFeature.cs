using Andja.Controller;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Model {

    public enum FeatureType { River, Volcano, }

    public enum FitType { Exact, Bigger, Smaller }

    public class IslandFeaturePrototypeData : LanguageVariables {
        public string ID;
        public FeatureType type;
        public Func<int, float> generateProbability;
        public GameEvent[] events;
        public FitType fitType;
        public TileType requiredTile;
        public Vector2Int requiredSpace;
        public Effect[] effects;

        public static IslandFeaturePrototypeData[] TempSetUp() {
            List<IslandFeaturePrototypeData> features = new List<IslandFeaturePrototypeData> {
            new IslandFeaturePrototypeData {
                ID = "vulcano",
                type = FeatureType.Volcano,
                generateProbability = (i) => { return 0.011f / i - (i) / 2000f; },
                effects = new Effect[1] { new Effect("volcanicearth") },
                events = new GameEvent[1] { new GameEvent("volcanic_eruption") },
                requiredTile = TileType.Mountain,
                requiredSpace = new Vector2Int(7,7),
                fitType = FitType.Bigger
            },
            new IslandFeaturePrototypeData {
                ID = "river",
                type = FeatureType.River,
                generateProbability = (i) => { return 0.025f / i; },
                requiredTile = TileType.Mountain,
                requiredSpace = new Vector2Int(1,1),
                fitType = FitType.Exact
            }
        };
            return features.ToArray();
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class IslandFeature {
        protected IslandFeaturePrototypeData _prototypData;

        public IslandFeaturePrototypeData Data {
            get {
                if (_prototypData == null) {
                    _prototypData = PrototypController.Instance.GetIslandFeaturePrototypeDataForID(ID);
                }
                return _prototypData;
            }
        }

        [JsonPropertyAttribute] public string ID;
        [JsonPropertyAttribute] public SeriaziableVector2 position;

        public IslandFeature(string iD) {
            ID = iD;
        }

        public FeatureType type => Data.type;
        public Func<int, float> GenerateProbability => Data.generateProbability;
        public GameEvent[] Events => Data.events;
        public FitType fitType => Data.fitType;
        public TileType RequiredTile => Data.requiredTile;
        public Vector2Int RequiredSpace => Data.requiredSpace;

        internal void Generate(IslandGenerator islandGenerator, int x, int y) {
            position = new SeriaziableVector2(x, y);
            switch (type) {
                case FeatureType.River:
                    MakeRiver(islandGenerator, islandGenerator.GetTileAt(x, y));
                    break;

                case FeatureType.Volcano:
                    MakeVolcano(islandGenerator, islandGenerator.GetTileAt(x, y));
                    break;
            }
        }

        private void MakeVolcano(IslandGenerator islandGenerator, Tile start) {
            if (start.Type == TileType.Volcano)
                return;
            for (int y = 0; y < 4; y++) {
                for (int x = 0; x < 4; x++) {
                    Tile tile = islandGenerator.GetTileAt(start.X + x, start.Y + y);
                    tile.Type = TileType.Volcano;
                    tile.SpriteName = TileSpriteController.GetSpriteForSpecial(TileType.Volcano, x, y);
                }
            }
            position = new SeriaziableVector2(start.X + 2, start.Y + 2);
        }

        private void MakeRiver(IslandGenerator islandGenerator, Tile start) {
            Stack<Tile> riverTiles = new Stack<Tile>();
            Tile current = start;
            bool[,] visited = new bool[islandGenerator.Width, islandGenerator.Height];
            Stack<Tile> toCheck = new Stack<Tile>();
            bool[,] marked = new bool[islandGenerator.Width, islandGenerator.Height];
            while (current.Type != TileType.Ocean) {
                Tile[] tiles = islandGenerator.GetNeighbours(current).OrderByDescending(x => x.Elevation).ToArray();
                foreach (Tile n in tiles) {
                    if (visited[n.X, n.Y])
                        continue;
                    toCheck.Push(n);
                }
                current = toCheck.Pop();
                visited[current.X, current.Y] = true;
                if (current.Type == TileType.Mountain || current.Type == TileType.Water)
                    continue;
                for (int y = -1; y <= 1; y++) {
                    for (int x = -1; x <= 1; x++) {
                        if (Mathf.Abs(x) + Mathf.Abs(y) == 2)
                            continue;
                        if (current.X + x < 0 || current.X + x > islandGenerator.Width - 1 || current.Y - y < 0 || current.Y - y > islandGenerator.Height - 1)
                            continue;
                        marked[current.X + x, current.Y - y] = true;
                    }
                }
                riverTiles.Push(current);
            }
            while (riverTiles.Count > 0) {
                current = riverTiles.Pop();
                //foreach (Tile n in GetNeighbours(current)) {
                //    bool hasNonMarked = false;
                //    foreach (Tile nn in GetNeighbours(n)) {
                //        if (marked[nn.X, nn.Y])
                //            continue;
                //        hasNonMarked = true;
                //        break;
                //    }
                //    marked[n.X, n.Y] = true;
                //    if (hasNonMarked == false) {
                //        n.Type = TileType.Water;
                //    }
                //}
                current.Moisture = 1;
                current.Type = TileType.Water;
                float currMoisture = current.Moisture;
                for (int y = -2; y <= 2; y++) {
                    for (int x = -2; x <= 2; x++) {
                        if (current.X + x < 0 || current.X + x > islandGenerator.Width - 1 || current.Y + y < 0 || current.Y + y > islandGenerator.Height - 1)
                            continue;
                        Tile currT = islandGenerator.GetTileAt(current.X + x, current.Y + y);
                        if (currT.Type == TileType.Water || currT.Type == TileType.Ocean)
                            continue;
                        currT.Moisture = currMoisture / 2 + currT.Moisture / 2;
                        currMoisture = currT.Moisture;
                    }
                }
                //IncreaseMoisture(current.Vector2, Vector2.up, 2);
                //IncreaseMoisture(current.Vector2, Vector2.down, 2);
                //IncreaseMoisture(current.Vector2, Vector2.left, 2);
                //IncreaseMoisture(current.Vector2, Vector2.right, 2);
            }
        }
    }
}