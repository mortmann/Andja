using Andja.Model;
using Andja.Controller;
using Andja.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;
using Andja.Model.Generator;

namespace Andja {
    public enum GameType { Endless, Campaign, Szenario, Island, Editor }

    public enum Difficulty { Easy, Medium, Hard, VeryHard }

    public enum Size { VerySmall, Small, Medium, Large, VeryLarge, Other }
    /// <summary>
    /// Contains Settings for the active SaveGame
    /// eg.: which events etc are enabled / Pirate / Loadout
    /// </summary>
    public class GameData : MonoBehaviour {
        public static GameData Instance;

        //NEVER CHANGES between games
        public static int WorldNumber = -1;
        public static int PirateNumber = -2;
        public static int FlyingTraderNumber = -3;
        public static string DataLocation => "Data";
        /// <summary>
        /// This is currently only used for predictive aim for projectiles.
        /// It is 0 for the moment because it currently should have no influence on the projectiles,
        ///  because 2d top down doesnt make much sense to calculate 3d aim.
        /// </summary>
        public static float Gravity = 0;
        public static float DemandChangeTime = 30f;

        //if nothing is set take that what is set by the editor in unity
        //if there is nothing set it is null so new world
        //only load the set value if not using ingame loading method
        public string LoadSaveGame => Application.isEditor && (loadSaveGameName == null || loadSaveGameName.Length == 0) ?
                                                editorloadsavegame : loadSaveGameName;

        public static bool StartGameWithBuildWarehouseDirectly = false;

        public static float PirateCheckRespawnShipCount = 60f;

        public string editorloadsavegame; // set through editor to make testing in editor faster & easier
        public static string loadSaveGameName; // set the to load save game through code

        public GameType saveFileType;

        public Difficulty difficulty; //should be calculated
        //TODO: make a way to set this either from world width/height or user
        public static Size WorldSize = Size.Medium;
        public static int Height = 700;
        public static int Width = 700;
        public int MapSeed = 10;
        public static List<MapGenerator.IslandGenInfo> islandGenInfos;
        public string[] usedIslands; // this has to be changed to some generation from the random code or smth
        public bool RandomSeed;

        public StartingLoadout Loadout;
        public static float ReturnResourcesPercentage = 0.3f;
        public static bool ReturnResources = false;
        public static float PirateAggroRange = 22.5f;
        public static float UnitAggroRange = 10f;
        public static float EffectTickTime = 1f;
        public static float nonCityTilesPercentage = 0.5f;


        //Pirate Data -- get set by difficulty
        public static float PirateCooldown = 5f*60f;
        public static int PirateShipCount = 2;

        public static FogOfWarStyle FogOfWarStyle = FogOfWarStyle.Off;
        public static int bots; // this is far from being in anykind relevant so
        public static int playerCount = 2;
        public static bool flyingTraders = true;
        public static bool pirates = true;

        public float playTime;

        public void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(this.gameObject);
                return;
            }
            Instance = this;
            if (transform.parent == null)
                DontDestroyOnLoad(this.gameObject);
        }

        private void Update() {
            if (WorldController.Instance == null)
                return;
            if (WorldController.Instance.IsPaused)
                return;
            playTime += WorldController.Instance.DeltaTime;
        }

        public void GenerateMap() {
            if (RandomSeed) {
                MapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
            Dictionary<MapGenerator.IslandGenInfo, Range> dict = new Dictionary<MapGenerator.IslandGenInfo, Range> {
            //Temporary fill this list, later generate this from selected/number of player, map size, difficulty, and other
            {new MapGenerator.IslandGenInfo(new Range(100,100), new Range(100, 100), Climate.Middle,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(100, 100), new Range(100, 100), Climate.Cold,true),new Range(1,2)
            },
            {new MapGenerator.IslandGenInfo(new Range(60, 60), new Range(60, 60), Climate.Warm,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(100, 100), new Range(100, 100), Climate.Warm,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(150, 150), new Range(150, 150), Climate.Middle,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(150, 150), new Range(150, 150), Climate.Middle,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(150, 150), new Range(150, 150), Climate.Middle,true),new Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new Range(150, 150), new Range(150, 150), Climate.Middle,true),new Range(1,1)
            }
        };
            if (SaveController.IsLoadingSave == false) {
                MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, dict, null);
            }
            else {
                MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, dict, new List<string>(usedIslands));
            }
            Loadout = PrototypController.Instance.StartingLoadouts[0];
        }

        public void SetMapSeed(int seed) {
            MapSeed = seed;
            GenerateMap();
        }

        public GameDataSave GetSaveGameData() {
            return new GameDataSave(MapSeed, Width, Height, usedIslands);
        }

        public void LoadGameData(GameDataSave data) {
            Width = data.Width;
            Height = data.Height;
            usedIslands = data.usedIslands;
            SetMapSeed(data.MapSeed);
        }

        private void OnDestroy() {
            if(Instance == this)
                Instance = null;
        }
    }

    [Serializable]
    public class GameDataSave : BaseSaveData {
        public int MapSeed;
        public int Height = 100;
        public int Width = 100;
        public string[] usedIslands; // this has to be changed to some generation from the random code or smth

        public GameDataSave() {
        }

        public GameDataSave(int mapSeed, int width, int height, string[] usedIslands) {
            Height = height;
            Width = width;
            MapSeed = mapSeed;
            this.usedIslands = usedIslands;
        }
    }

    [Serializable]
    public class StartingLoadout {
        public Item[] Items;
        public Unit[] Units;
        public Structure[] Structures;
        public int Money;

        internal Item[] GetItemsCopy() {
            return Array.ConvertAll(Items, a => a.CloneWithCount());
        }
    }
}