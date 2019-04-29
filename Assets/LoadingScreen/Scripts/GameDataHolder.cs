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
    public Size WorldSize {
        get { return Size.Medium; }
    }
    public Difficulty difficulty;
    public GameType saveFileType;
    public float playTime;

    public int Height = 100;
    public int Width = 100;
    //if nothing is set take that what is set by the editor in unity
    //if there is nothing set it is null so new world
    public string Loadsavegame => setloadsavegame ?? editorloadsavegame;
    public string editorloadsavegame;
    public static string setloadsavegame;

    public int MapSeed;

    public int[] bots; // this is for from being in anykind relevant so 
    public int playerCount = 1;
    public bool pirates = true;
    public bool fire = true;
    public bool[] catastrophics;
    public List<MapGenerator.IslandGenInfo> islandGenInfos;
    public string[] usedIslands; // this has to be changed to some generation from the random code or smth

    public void Start() {
        if (Instance != null) {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;

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
            {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100, 100), new MapGenerator.Range(100, 100), Climate.Cold),new MapGenerator.Range(1,1)
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