using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public enum GameType { Endless, Campaign, Szenario, Island }
public enum Difficulty { Easy, Medium, Hard, VeryHard }
public enum Size { VerySmall, Small, Medium, Large, VeryLarge, Other }

public class GameDataHolder : MonoBehaviour {
    public static GameDataHolder Instance;
    //TODO: make a way to set this either from world width/height or user
    public static Size WorldSize = Size.Medium;
    public Difficulty difficulty; //should be calculated
    public GameType saveFileType;
    public float playTime;
    public static int Height = 500;
    public static int Width = 500;
    //if nothing is set take that what is set by the editor in unity
    //if there is nothing set it is null so new world
    //only load the set value if not using ingame loading method
    public string Loadsavegame => Application.isEditor && (setloadsavegame == null || setloadsavegame.Length==0)? editorloadsavegame : setloadsavegame;
    public string editorloadsavegame;
    public static string setloadsavegame;

    public static int MapSeed=10;

    public static int bots; // this is far from being in anykind relevant so 
    public static int playerCount = 1;
    public static bool pirates = true;
    public static bool fire = true;
    public static bool[] disasters;
    public static List<MapGenerator.IslandGenInfo> islandGenInfos;
    public string[] usedIslands; // this has to be changed to some generation from the random code or smth

    public void Start() {
        if (Instance != null) {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

        //MapSeed = UnityEngine.Random.Range(0, int.MaxValue);

    }
    private void Update() {
        if (WorldController.Instance == null)
            return;
        if (WorldController.Instance.IsPaused)
            return;
        playTime += WorldController.Instance.DeltaTime;
    }
    public void GenerateMap() {
        if (SaveController.IsLoadingSave == false) {
            Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> dict = new Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> { 
            //Temporary fill this list, later generate this from selected/number of player, map size, difficulty, and other
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100,100), new MapGenerator.Range(100, 100), Climate.Middle),new MapGenerator.Range(1,1)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100, 100), new MapGenerator.Range(100, 100), Climate.Cold),new MapGenerator.Range(1,2)
            },
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(60, 60), new MapGenerator.Range(60, 60), Climate.Warm),new MapGenerator.Range(1,1)
            }};
            MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, dict, null);
        }
        else {
            MapGenerator.Instance.DefineParameters(MapSeed, Width, Height, null, new List<string>(usedIslands));
        }

    }
    public void SetMapSeed(int seed) {
        MapSeed = seed;
        GenerateMap();
    }
    public void SetHeight(Text go) {
        Height = int.Parse(go.text);
    }
    public void SetWidht(Text go) {
        Width = int.Parse(go.text);
    }

    public GameData GetSaveGameData() {
        return new GameData(MapSeed, Width, Height, usedIslands);
    }
    public void LoadGameData(GameData data) {
        Width = data.Width;
        Height = data.Height;
        usedIslands = data.usedIslands;
        SetMapSeed(data.MapSeed);
    }
}

[Serializable]
public class GameData : BaseSaveData {
    public int MapSeed;
    public int Height = 100;
    public int Width = 100;
    public string[] usedIslands; // this has to be changed to some generation from the random code or smth
    public GameData() {
    }
    public GameData(int mapSeed, int width, int height, string[] usedIslands) {
        Height = height;
        Width = width;
        MapSeed = mapSeed;
        this.usedIslands = usedIslands;
    }
}