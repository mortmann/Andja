using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class GameDataHolder : MonoBehaviour {

	public static GameDataHolder Instance;

	public int height=100;
	public int width=100;
	public string loadsavegame;
	public string mapname; //for future when tiles are in a diffrent file
	public int[] bots; // this is for from being in anykind relevant so 
	public int playerCount=1;
	public bool pirates=true;
	public bool fire=true;
	public bool[] catastrophics;
    public List<MapGenerator.IslandGenInfo> islandGenInfos;


    public void Awake() {
        if (Instance != null) {
            Destroy(this.gameObject);
            return;
        }
        Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> dict = new Dictionary<MapGenerator.IslandGenInfo, MapGenerator.Range> { 
        //Temporary fill this list, later generate this from selected/number of player, map size, difficulty, and other
        {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100,100), new MapGenerator.Range(100, 100), Climate.Middle),new MapGenerator.Range(1,1)
        },
        {new MapGenerator.IslandGenInfo(new MapGenerator.Range(100, 100), new MapGenerator.Range(100, 100), Climate.Cold),new MapGenerator.Range(1,1)
        },
        {new MapGenerator.IslandGenInfo(new MapGenerator.Range(60, 60), new MapGenerator.Range(60, 60), Climate.Warm),new MapGenerator.Range(1,1)
        }};
        GameObject.FindObjectOfType<MapGenerator>().DefineParameters(10,width,height,dict,null);
        GameObject.FindObjectOfType<MapGenerator>().Generate();
        Instance = this;
		DontDestroyOnLoad (this);
	}
	public void SetHeight(Text go){
		height = int.Parse (go.text);
	}
	public void SetWidht(Text go){
		width = int.Parse (go.text);
	}

	public GameData GetSaveGameData(){
		return new GameData();
	}
	public void LoadGameData(GameData data){
	}
}

[Serializable]
public class GameData {
}