using Andja.Model;
using Andja.Model.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    public class AIController : MonoBehaviour {
        public static AIController Instance { get; protected set; }

        public static PerPopulationLevelData[] PerPopulationLevelDatas {
            get {
                if (_perPopulationLevelDatas == null)
                    Calculate();
                return _perPopulationLevelDatas;
            }
            protected set => _perPopulationLevelDatas = value;
        }

        public static Dictionary<Island, Dictionary<Tile, TileValue>> IslandsTileToValue {
            get {
                if (_islandsTileToValue == null)
                    CalculateIslandTileValues();
                return _islandsTileToValue;
            }
            set => _islandsTileToValue = value;
        }

        public static Dictionary<Island, List<TileValue>> IslandToMapSpaceValuedTiles {
            get {
                if (_islandToMapSpaceValuedTiles == null)
                    CalculateIslandTileValues();
                return _islandToMapSpaceValuedTiles;
            }
            set => _islandToMapSpaceValuedTiles = value;
        }

        public static Dictionary<Island, List<TileValue>> IslandToCurrentSpaceValuedTiles {
            get {
                if (_islandToCurrentSpaceValuedTiles == null)
                    CalculateIslandTileValues();
                return _islandToCurrentSpaceValuedTiles;
            }
            set => _islandToCurrentSpaceValuedTiles = value;
        }

        private static Dictionary<Island, List<TileValue>> _islandToMapSpaceValuedTiles;
        private static Dictionary<Island, List<TileValue>> _islandToCurrentSpaceValuedTiles;
        private static Dictionary<Island, Dictionary<Tile, TileValue>> _islandsTileToValue;
        private static PerPopulationLevelData[] _perPopulationLevelDatas;

        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two AIController.");
            }
            Instance = this;
        }
        AIPlayer test;
        private void Start() {
            BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
            BuildController.Instance.RegisterStructureDestroyed(OnStructureDestroyed);
            test = new AIPlayer(PlayerController.GetPlayer(1));
            test.CalculatePlayersCombatValue();
            test.CalculateIslandScores();
        }
        private void OnDrawGizmos() {
#if UNITY_EDITOR
            if(test != null)
                foreach (var item in test.islandScores) {
                    UnityEditor.Handles.Label(item.Island.Center, "Score: " + item.EndScore);
                }
#endif
        }

        private void OnDestroy() {
            Instance = null;
            _islandToMapSpaceValuedTiles = null;
            _islandToCurrentSpaceValuedTiles = null;
            _islandsTileToValue = null;
            PerPopulationLevelDatas = null;
        }

        internal string GetTileValue(Tile tile) {
            if (tile.Type == TileType.Ocean)
                return "";
            if (IslandsTileToValue[tile.Island].ContainsKey(tile) == false) {
                return "ERROR";
            }
            return IslandsTileToValue[tile.Island][tile].ToString();
        }

        private static void OnStructureDestroyed(Structure structure, IWarfare iwarfare) {
            for (int x = 0; x < structure.TileWidth; x++) {
                for (int y = 0; y < structure.TileHeight; y++) {
                    Tile t = structure.Tiles[y + x * structure.TileHeight];
                    if (y == 0) {
                        ChangeTileValue(t, t.West(), Direction.E);
                    }
                    if (y < structure.TileWidth) {
                        ChangeTileValue(t, t.South(), Direction.N);
                    }
                    if (y == structure.TileWidth - 1) {
                        ChangeTileValue(t, t.East(), Direction.W);
                    }
                    if (x == structure.TileHeight - 1) {
                        ChangeTileValue(t, t.North(), Direction.S);
                    }
                }
            }
        }

        private static void OnStructureCreated(Structure structure, bool load) {
            if (structure.CanBeBuildOver)
                return;
            if (IslandsTileToValue == null)
                CalculateIslandTileValues();
            Island island = structure.BuildTile.Island;
            if (island == null)
                return;
            Dictionary<Tile, TileValue> tileValue = IslandsTileToValue[island];
            for (int y = 0; y < structure.TileHeight; y++) {
                for (int x = 0; x < structure.TileWidth; x++) {
                    Tile t = structure.Tiles[x * structure.TileHeight + y];
                    tileValue[t].neValue = Vector2.zero;
                    tileValue[t].swValue = Vector2.zero;
                    if (x == 0) {
                        ChangeTileValue(t.East(), t, Direction.E);
                    }
                    if (y == structure.TileHeight - 1) {
                        ChangeTileValue(t.North(), t, Direction.N);
                    }
                    if (x == structure.TileWidth - 1) {
                        ChangeTileValue(t.West(), t, Direction.W);
                    }
                    if (y == 0) {
                        ChangeTileValue(t.South(), t, Direction.S);
                    }
                }
            }
        }

        private static void ChangeTileValue(Tile t, Tile tValue, Direction direction) {
            if (t.Type == TileType.Ocean) {
                return;
            }
            Dictionary<Tile, TileValue> tileValue = IslandsTileToValue[t.Island];
            if (t.IsGenericBuildType() != tValue.IsGenericBuildType()) {
                if (t.Type != tValue.Type)
                    return;
            }
            if (tileValue.ContainsKey(t))
                return;
            switch (direction) {
                case Direction.N:
                    tileValue[t].swValue.y = tileValue[t.South()].swValue.y + 1;
                    if (t.North().Structure != null)
                        return;
                    ChangeTileValue(t.North(), t, Direction.N);
                    break;

                case Direction.W:
                    tileValue[t].neValue.x = tileValue[t.East()].neValue.x + 1;
                    if (t.West().Structure != null)
                        return;
                    ChangeTileValue(t.West(), t, Direction.W);
                    break;

                case Direction.S:
                    tileValue[t].neValue.y = tileValue[t.North()].neValue.y + 1;
                    if (t.South().Structure != null)
                        return;
                    ChangeTileValue(t.South(), t, Direction.S);
                    break;

                case Direction.E:
                    tileValue[t].swValue.x = tileValue[t.West()].swValue.x + 1;
                    if (t.East().Structure != null)
                        return;
                    ChangeTileValue(t.East(), t, Direction.E);
                    break;
            }
        }

        public static bool PlaceStructure(AIPlayer player, Structure structure, List<Tile> tiles, Unit buildUnit = null, bool onStart = false) {
            BuildController.Instance.BuildOnTile(structure, tiles, player.PlayerNummer, false, false, buildUnit, false, onStart);
            return true;
        }

        public static void CalculateIslandTileValues() {
            _islandToMapSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
            _islandToCurrentSpaceValuedTiles = new Dictionary<Island, List<TileValue>>();
            _islandsTileToValue = new Dictionary<Island, Dictionary<Tile, TileValue>>();

            World world = World.Current;
            foreach (Island island in World.Current.Islands) {
                IslandToMapSpaceValuedTiles[island] = TileValue.CalculateStartingValues(island);
                IslandToCurrentSpaceValuedTiles[island] = new List<TileValue>(from TileValue in IslandToMapSpaceValuedTiles[island]
                                                                              select new TileValue(TileValue)); //copy them
                IslandsTileToValue[island] = new Dictionary<Tile, TileValue>();
                foreach (TileValue tv in IslandToCurrentSpaceValuedTiles[island]) {
                    IslandsTileToValue[island][tv.tile] = tv;
                }
            }
        }

        public static void Calculate() {
            PerPopulationLevelDatas = new PerPopulationLevelData[PrototypController.Instance.NumberOfPopulationLevels];
            IReadOnlyDictionary<int, PopulationLevelPrototypData> levelData = PrototypController.Instance.PopulationLevelDatas;
            for (int i = 0; i < PrototypController.Instance.NumberOfPopulationLevels; i++) {
                PerPopulationLevelData ppd = new PerPopulationLevelData();
                PerPopulationLevelDatas[i] = ppd;
                IReadOnlyDictionary<int, Unlocks> unlocks = PrototypController.Instance.LevelCountToUnlocks[i];
                foreach (int count in unlocks.Keys) {
                    if (ppd.atleastRequiredPeople < count)
                        ppd.atleastRequiredPeople = count;
                    foreach (Need n in unlocks[count].needs) {
                        if (n.IsItemNeed()) {
                            ppd.itemNeeds.Add(n);
                        }
                        else {
                            ppd.structureNeeds.Add(n);
                        }
                        if (n.Data.produceForPeople == null)
                            continue;
                        foreach (Produce prod in n.Data.produceForPeople.Keys) {
                            foreach (SupplyChain sc in prod.SupplyChains) {
                                if (sc.cost.requiredFertilites != null)
                                    ppd.possibleFertilities.UnionWith(sc.cost.requiredFertilites);
                                if (sc.cost.TotalItemCost != null)
                                    ppd.buildMaterialRequired.Union(sc.cost.TotalItemCost, new GenericCompare<Item>(x => x.ID));
                            }
                        }
                    }
                }
                ppd.atleastRequiredHomes = Mathf.CeilToInt((float)ppd.atleastRequiredPeople / (float)levelData[i].HomeStructure.people);
            }
            foreach (Item item in PrototypController.Instance.MineableItems) {
                PerPopulationLevelDatas[item.Data.UnlockLevel].newResources.Add(item.ID);
            }
        }
    }

    public class PerPopulationLevelData {
        public int atleastRequiredPeople;
        public int atleastRequiredHomes;
        public List<string> newResources = new List<string>();
        public List<Need> itemNeeds = new List<Need>();
        public List<Need> structureNeeds = new List<Need>();
        public HashSet<Fertility> possibleFertilities = new HashSet<Fertility>();
        public List<Item> buildMaterialRequired = new List<Item>();
    }
}