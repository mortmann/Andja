using Andja.Model;
using Andja.Model.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using static Andja.Controller.AIController;

namespace Andja.Controller {

    public class AIController : MonoBehaviour {
        public static AIController Instance { get; protected set; }
        /// <summary>
        /// Active means currently calculating things.
        /// </summary>
        public static bool ActiveAI = true;
        public static int AIOperationsPerFrame = 1;
        /// <summary>
        /// Shutdown means it ai player will stop the threads
        /// </summary>
        public static bool ShutdownAI = false;

        private readonly ConcurrentQueue<Operation> _allOperations = new ConcurrentQueue<Operation>();
        public static PerPopulationLevelData[] PerPopulationLevelDatas {
            get {
                if (_perPopulationLevelDatas == null)
                    Calculate();
                return _perPopulationLevelDatas;
            }
            protected set => _perPopulationLevelDatas = value;
        }
        /// <summary>
        /// This is the original values without any structures
        /// </summary>
        public static Dictionary<IIsland, Dictionary<Tile, TileValue>> IslandToMapSpaceValuedTiles {
            get {
                if (_islandToMapSpaceValuedTiles == null)
                    CalculateIslandTileValues();
                return _islandToMapSpaceValuedTiles;
            }
            set => _islandToMapSpaceValuedTiles = value;
        }

        /// <summary>
        /// This is the current values with any structures
        /// </summary>
        public static Dictionary<IIsland, Dictionary<Tile, TileValue>> IslandsTileToValue {
            get {
                if (_islandsTileToValue == null)
                    CalculateIslandTileValues();
                return _islandsTileToValue;
            }
            set => _islandsTileToValue = value;
        }
        private static Dictionary<IIsland, Dictionary<Tile, TileValue>> _islandToMapSpaceValuedTiles;
        public static ConcurrentDictionary<ICity, ConcurrentDictionary<Tile, TileValue>> _cityToCurrentSpaceValueTiles;
        private static Dictionary<IIsland, Dictionary<Tile, TileValue>> _islandsTileToValue;
        private static PerPopulationLevelData[] _perPopulationLevelDatas;
        private AIPlayer _test;
        private List<AIPlayer> _aiPlayers;
        private static Thread[] _threads;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two AIController.");
            }
            Instance = this;
        }

        public void Start() {
            if (PlayerController.Instance.PlayerCount == 1) {
                Destroy(this);
                return;
            }
            AIOperationsPerFrame = PlayerController.Instance.PlayerCount - 1;
            _cityToCurrentSpaceValueTiles = new ConcurrentDictionary<ICity, ConcurrentDictionary<Tile, TileValue>>();
            BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
            BuildController.Instance.RegisterStructureDestroyed(OnStructureDestroyed);
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
            BuildController.Instance.RegisterAnyCityDestroyed(OnCityDestroy);

            _test = new AIPlayer(PlayerController.Instance.GetPlayer(1));
            _test.CalculatePlayersCombatValue();
            _test.CalculateIslandScores();
            _aiPlayers = new List<AIPlayer>();
            foreach (Player player in PlayerController.Instance.Players) {
                if (player.IsHuman)
                    continue;
                AIPlayer ai = new AIPlayer(player);
                player.AI = ai;
                _aiPlayers.Add(ai);
            }
            ShutdownAI = false;
            ActiveAI = true;
            foreach (var city in World.Current.Islands.SelectMany(i => i.Cities)) {
                OnCityCreated(city);
            }
            _threads = new Thread[_aiPlayers.Count];
            
            for (int i = 0; i < _aiPlayers.Count; i++) {
                AIPlayer ai = _aiPlayers[i];
                _threads[i] = new Thread(() => { ai.Loop(); Debug.Log("Shutdown AI " + ai.Player.Name); });
                _threads[i].Start();
            }
        }

        private void OnCityDestroy(ICity obj) {
            _cityToCurrentSpaceValueTiles.TryRemove(obj, out _);
        }

        private void OnCityCreated(ICity city) {
            _cityToCurrentSpaceValueTiles.TryAdd(city, TileValue.CalculateStartingValues(city.Island, city, true));
            city.RegisterTileAdded(OnCityTileAdded);
            city.RegisterTileRemove(OnCityTileRemoved);
        }

        private void OnCityTileRemoved(ICity c, Tile t) {
            _cityToCurrentSpaceValueTiles[c].TryRemove(t, out _);
        }

        private void OnCityTileAdded(ICity c, Tile t) {
            if(_cityToCurrentSpaceValueTiles[c].ContainsKey(t) == false)
                _cityToCurrentSpaceValueTiles[c].TryAdd(t, new TileValue(t, 1, 1, 1, 1));
            if (HasToDoCheck(c, t, t.East(), Direction.E)) ChangeTileValue(t.West(), t, Direction.W);
            if (HasToDoCheck(c, t, t.North(), Direction.N)) ChangeTileValue(t.South(), t, Direction.S);
            if (HasToDoCheck(c, t, t.South(), Direction.S)) ChangeTileValue(t.North(), t, Direction.N);
            if (HasToDoCheck(c, t, t.West(), Direction.W)) ChangeTileValue(t.East(), t, Direction.E);
        }
        private bool HasToDoCheck(ICity c, Tile t, Tile o, Direction dir) {
            return o.City != c || _cityToCurrentSpaceValueTiles[c][t].HasToDoCheck(_cityToCurrentSpaceValueTiles[c][o], dir);
        }
        public void Update() {
            for (int i = 0; i < AIOperationsPerFrame; i++) {
                if(_allOperations.TryDequeue(out Operation op) == false) {
                    continue;
                }
                op.Status = op.Do() == false ? OperationStatus.Failure : OperationStatus.Success;
            }
            foreach (AIPlayer item in _aiPlayers) {
                item.Update(WorldController.Instance.DeltaTime);
            }
        }

        public void OnDrawGizmos() {
#if UNITY_EDITOR
            if (_test == null) return;
            foreach (var item in _test.islandScores) {
                UnityEditor.Handles.Label(item.Island.Center, "Score: " + item.EndScore);
            }
#endif
        }

        public void OnDestroy() {
            Instance = null;
            _islandToMapSpaceValuedTiles = null;
            _cityToCurrentSpaceValueTiles = null;
            _islandsTileToValue = null;
            PerPopulationLevelDatas = null;
            ActiveAI = false;
            ShutdownAI = true;
        }

        internal string GetTileValue(Tile tile) {
            if (tile.Type == TileType.Ocean)
                return "";
            return _cityToCurrentSpaceValueTiles[tile.City].ContainsKey(tile) == false ?
                "ERROR" :
                _cityToCurrentSpaceValueTiles[tile.City][tile].ToString();
        }

        internal Operation AddOperation(Operation operation) {
            _allOperations.Enqueue(operation);
            return operation;
        }

        private static void OnStructureDestroyed(Structure structure, IWarfare iwarfare) {
            for (int x = 0; x < structure.TileWidth; x++) {
                for (int y = 0; y < structure.TileHeight; y++) {
                    Tile t = structure.Tiles[y + x * structure.TileHeight];
                    if (y == 0) {
                        ChangeTileValue(t.East(), t, Direction.E);
                    }
                    if (y < structure.TileWidth) {
                        ChangeTileValue(t.North(), t, Direction.N);
                    }
                    if (y == structure.TileWidth - 1) {
                        ChangeTileValue(t.West(), t, Direction.W);
                    }
                    if (x == structure.TileHeight - 1) {
                        ChangeTileValue(t.South(), t, Direction.S);
                    }
                }
            }
        }

        private static void OnStructureCreated(Structure structure, bool load) {
            if (structure.CanBeBuildOver)
                return;
            if (IslandsTileToValue == null)
                CalculateIslandTileValues();
            IIsland island = structure.BuildTile.Island;
            if (island == null)
                return;
            Dictionary<Tile, TileValue> tileValue = IslandsTileToValue[island];
            lock(tileValue) {
                for (int x = 0; x < structure.TileWidth; x++) {
                    for (int y = 0; y < structure.TileHeight; y++) {
                        Tile t = structure.Tiles[x * structure.TileHeight + y];
                        tileValue[t].SetValuesToZero();
                        _cityToCurrentSpaceValueTiles[t.City][t].SetValuesToZero();
                        if (x == 0) {
                            ChangeTileValue(t.West(), t, Direction.W);
                        }
                        if (y == structure.TileHeight - 1) {
                            ChangeTileValue(t.North(), t, Direction.N);
                        }
                        if (x == structure.TileWidth - 1) {
                            ChangeTileValue(t.East(), t, Direction.E);
                        }
                        if (y == 0) {
                            ChangeTileValue(t.South(), t, Direction.S);
                        }
                    }
                }
            }
        }

        private static void ChangeTileValue(Tile current, Tile previous, Direction direction) {
            if (current.Type == TileType.Ocean) {
                return;
            }
            if (current.Structure != null && current.Structure.ShouldAICountTileAsFree() == false) {
                _cityToCurrentSpaceValueTiles[current.City][current].SetValuesToZero();
                IslandsTileToValue[current.Island][current].SetValuesToZero();
            } else {
                if (previous.Type != current.Type) {
                    _cityToCurrentSpaceValueTiles[current.City][current].SetValue(direction, 1);
                    IslandsTileToValue[current.Island][current].SetValue(direction, 1);
                }
                else {
                    if (_cityToCurrentSpaceValueTiles[current.City].ContainsKey(previous)) {
                        _cityToCurrentSpaceValueTiles[current.City][current].SetValuePlusOne(direction, _cityToCurrentSpaceValueTiles[current.City][previous]);
                    } 
                    IslandsTileToValue[current.Island][current].SetValuePlusOne(direction, IslandsTileToValue[current.Island][previous]);
                }
            }
            ChangeTileValue(current.GetDirectionTile(direction), current, direction);
        }
        
        public static bool BuildStructure(AIPlayer player, Structure structure, List<Tile> tiles, Unit buildUnit = null, bool onStart = false) {
            return BuildController.Instance.BuildOnTile(structure, tiles, player.PlayerNumber, false, false, buildUnit, false, onStart);
        }
        public static bool BuildStructure(AIPlayer player, Structure structure, Tile tile, Unit buildUnit = null, bool onStart = false) {
            return BuildController.Instance.BuildOnTile(structure, structure.GetBuildingTiles(tile),
                                                player.PlayerNumber, false, false, buildUnit, false, onStart);
        }
        public static void CalculateIslandTileValues() {
            _islandToMapSpaceValuedTiles = new Dictionary<IIsland, Dictionary<Tile, TileValue>>();
            _islandsTileToValue = new Dictionary<IIsland, Dictionary<Tile, TileValue>>();

            foreach (Island island in World.Current.Islands) {
                 _islandToMapSpaceValuedTiles[island] = TileValue.CalculateStartingValues(island).ToDictionary(entry => entry.Key,
                                                       entry => entry.Value);
                //copy them
                IslandsTileToValue[island] = TileValue.CalculateStartingValues(island, null, true).ToDictionary(entry => entry.Key,
                                                       entry => entry.Value);
            }
        }
        /// <summary>
        /// Different Chains *CAN* have completly different required Fertilities and resources.
        /// So we would have to calculate the island values based on each possible combination of supply chains.
        /// How would the one structure that all?
        /// </summary>
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
                        HashSet<Fertility> fertilities = new HashSet<Fertility>();
                        List<List<Fertility>> optionalFertilities = new List<List<Fertility>>();
                        foreach (Produce prod in n.Data.produceForPeople.Keys) {
                            var temp = prod.GetNeededFertilities(i);
                            if (fertilities.Count == 0)
                                fertilities = temp;
                            else
                                fertilities.IntersectWith(temp);
                            foreach (SupplyChain sc in prod.SupplyChains) {
                                if (sc.cost.TotalItemCost != null)
                                    ppd.buildMaterialRequired.Concat(sc.cost.TotalItemCost).GroupBy(x=> x.ID , x => x.count)
                                        .Select(g => new Item(g.Key, g.Sum()));
                                optionalFertilities.Add(sc.cost.requiredFertilites?.Except(fertilities).ToList());
                            }
                        }
                        ppd.optionalFertilities.Add(optionalFertilities);
                        ppd.requiredFertilities.Concat(fertilities);
                    }
                }
                ppd.atleastRequiredHomes = Mathf.CeilToInt((float)ppd.atleastRequiredPeople / (float)levelData[i].HomeStructure.People);
            }
            foreach (Item item in PrototypController.Instance.MineableItems) {
                if(item.Data.UnlockLevel < 0)
                    Debug.LogError(item.ID + " Unlock Level is lower than 0 " + item.Data.UnlockLevel);
                if (item.Data.UnlockLevel < PrototypController.Instance.NumberOfPopulationLevels)
                    PerPopulationLevelDatas[item.Data.UnlockLevel].newResources.Add(item.ID);
                else
                    Debug.LogError(item.ID + " Unlock Level is higher than levels " + item.Data.UnlockLevel);
            }
        }

        public static void UpdateCityCurrentSpaceValue(City city, Tile tile) {
            if (_cityToCurrentSpaceValueTiles != null && _cityToCurrentSpaceValueTiles[city].ContainsKey(tile) == false)
                _cityToCurrentSpaceValueTiles[city].TryAdd(tile, new TileValue(tile, 1, 1, 1, 1));
        }
    }

    public class PerPopulationLevelData {
        public int atleastRequiredPeople;
        public int atleastRequiredHomes;
        public List<string> newResources = new List<string>();
        public List<Need> itemNeeds = new List<Need>();
        public List<Need> structureNeeds = new List<Need>();
        public HashSet<Fertility> requiredFertilities = new HashSet<Fertility>();
        public List<List<List<Fertility>>> optionalFertilities = new List<List<List<Fertility>>>();

        public List<Item> buildMaterialRequired = new List<Item>();
    }
}