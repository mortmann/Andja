using UnityEngine;
using System.Collections;
using UnityEngine.UI;
public class GameDataHolder : MonoBehaviour {

	public int height=100;
	public int width=100;
	public void Start(){
		DontDestroyOnLoad (this);
	}
	public void SetHeight(Text go){Debug.Log (go.text); 
		height = int.Parse (go.text);
	}
	public void SetWidht(Text go){
		width = int.Parse (go.text);
	}
}
