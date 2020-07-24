using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public enum GameType { Endless, Campaign, Szenario, Island }
public enum Difficulty { Easy, Medium, Hard, VeryHard }
public enum Size { VerySmall, Small, Medium, Large, VeryLarge, Other }

public class GameData : MonoBehaviour {
    public static GameData Instance;
    //TODO: make a way to set this either from world width/height or user
    public static Size WorldSize = Size.Medium;
    public Difficulty difficulty; //should be calculated
    public GameType saveFileType;
    public static int Height = 500;
    public static int Width = 500;
    public int MapSeed = 10;
    public StartingLoadout Loadout;
    //if nothing is set take that what is set by the editor in unity
    //if there is nothing set it is null so new world
    //only load the set value if not using ingame loading method
    public string Loadsavegame => Application.isEditor && (setloadsavegame == null || setloadsavegame.Length==0)? editorloadsavegame : setloadsavegame;
    public string editorloadsavegame;
    public static string setloadsavegame;

    public bool RandomSeed;
    public static int bots; // this is far from being in anykind relevant so 
    public static int playerCount = 1;
    public static float nonCityTilesPercantage = 0.5f;
    public static bool flyingTraders = true;
    public static bool pirates = true;
    public static bool[] disasters;
    public static List<MapGenerator.IslandGenInfo> islandGenInfos;
    public string[] usedIslands; // this has to be changed to some generation from the random code or smth

    public float playTime;

    public void Awake() {
        if (Instance != null) {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        if(transform.parent == null)
            DontDestroyOnLoad(this);
    }
    private void Update() {
        if (WorldController.Instance == null)
            return;
        if (WorldController.Instance.IsPaused)
            return;
        playTime += WorldController.Instance.DeltaTime;
    }
    public void GenerateMap() {
        if(RandomSeed) {
            MapSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }
        Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> dict = new Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> { 
            //Temporary fill this list, later generate this from selected/number of player, map size, difficulty, and other
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100,100), new MapGenerator.Range(100, 100), Climate.Middle,false),new MapGenerator.Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100, 100), new MapGenerator.Range(100, 100), Climate.Cold,false),new MapGenerator.Range(1,2)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(60, 60), new MapGenerator.Range(60, 60), Climate.Warm,false),new MapGenerator.Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100, 100), new MapGenerator.Range(100, 100), Climate.Warm,true),new MapGenerator.Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(150, 150), new MapGenerator.Range(150, 150), Climate.Middle,true),new MapGenerator.Range(1,1)
            }
        };
        if (SaveController.IsLoadingSave == false) {
            MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, dict, null);
        }
        else {
            MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, dict, new List<string>(usedIslands));
        }
        Loadout = PrototypController.Instance.StartingLoadouts[0];
        //Loadout = new StartingLoadout {
        //    Items = new Item[] { new Item("wood") { count = 100 }, new Item("tools") { count = 66 }, new Item("fish") { count = 25 } },
        //    Structures = new Structure[] { PrototypController.Instance.GetStructure("warehouse1") },
        //    Units = new Unit[] { PrototypController.Instance.GetUnitForID("ship") },
        //    Money = 50005,
        //};
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