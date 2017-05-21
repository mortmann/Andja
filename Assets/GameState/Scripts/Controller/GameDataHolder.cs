using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class GameDataHolder : MonoBehaviour {

	public static GameDataHolder Instance;

	public int height=100;
	public int width=100;
	public string loadsavegame;
	public string mapname;//for future when tiles are in a diffrent file
	public int[] bots; // this is for so 
	public int playerCount=1;
	public bool pirates=true;
	public bool fire=true;
	public bool[] catastrophics;
	

	public void Awake(){
		if(Instance!=null){
			Destroy (this.gameObject);
			return;
		} 
		Instance = this;
		DontDestroyOnLoad (this);
	}
	public void SetHeight(Text go){
		height = int.Parse (go.text);
	}
	public void SetWidht(Text go){
		width = int.Parse (go.text);
	}

	public string GetSaveGameData(){
		return "GDH";
	}
	public void LoadGameData(string data){
		Debug.Log (data);
	}
}
