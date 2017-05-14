using UnityEngine;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class SaveLoadUIScript : MonoBehaviour {

	public GameObject listPrefab;
	public GameObject canvasGO;
	string selected;
	public InputField saveGameInput;
	GameObject selectedGO;
	// Use this for initialization
	void OnEnable () {
		foreach (Transform item in canvasGO.transform) {
			GameObject.Destroy (item.gameObject);
		}
		string directoryPath="";
		if (GameDataHolder.Instance != null) {
			directoryPath = GameDataHolder.Instance.GetSaveGamesPath ();
		} else {
			directoryPath = EditorController.Instance.GetSaveGamesPath ();
		}
		
		DirectoryInfo saveDir = new DirectoryInfo( directoryPath );
		if(saveDir.Exists==false){
			saveDir.Create ();
		}
		FileInfo[] saveGames;
		if(EditorController.Instance!=null){
			saveGames = saveDir.GetFiles("*.isl").OrderBy( f => f.CreationTime ).ToArray();
		} else {
			 saveGames = saveDir.GetFiles("*.sav").OrderBy( f => f.CreationTime ).ToArray();
		}

		// Build file list by instantiating fileListItemPrefab

		for(int i = saveGames.Length-1; i>=0 ; i-- ) {
			GameObject go = (GameObject)GameObject.Instantiate(listPrefab);

			// Make sure this gameobject is a child of our list box
			go.transform.SetParent( canvasGO.transform );

			// file contains something like "C:\Users\UserName\......\Project Porcupine\Saves\SomeFileName.sav"
			// Path.GetFileName(file) returns "SomeFileName.sav"
			// Path.GetFileNameWithoutExtension(file) returns "SomeFileName"
			EventTrigger trigger = go.GetComponent<EventTrigger> ();
			EventTrigger.Entry entry = new EventTrigger.Entry( );
			entry.eventID = EventTriggerType.PointerClick;
			string name = Path.GetFileNameWithoutExtension( saveGames[i].FullName);
			entry.callback.AddListener ((data)=>{OnSaveGameSelect (name,go);});
			trigger.triggers.Add( entry );

			go.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension( saveGames[i].FullName) +" ["+ saveGames[i].CreationTime+"]";

		}
		if(saveGameInput!=null){
			saveGameInput.onValueChanged.AddListener ((data)=>OnInputChange());			
		}

	}
	public void OnInputChange(){
		OnSaveGameSelect (null,null);		
	}
	public void OnSaveGameSelect(string fi,GameObject go){
		selected = fi;
		if (selectedGO != null)
			selectedGO.GetComponent<SelectableScript> ().OnDeselectCall ();
		selectedGO = go;
	}
	public void OnLoadPressed(){
		if(selected==null){
			return;
		}
		//TODO ASK IF he wants to load it
		//and warn losing ansaved data
		GameDataHolder.Instance.loadsavegame = selected;
		if (WorldController.Instance != null)
			WorldController.Instance.LoadWorld ();
		else
			GameObject.FindObjectOfType<ChangeScene> ().ChangeToGameStateLoadScreen ();
	}
	public void OnSavePressed(){
		if(selected!=null&&(saveGameInput.text==null||saveGameInput.text=="")){
			SaveController.Instance.SaveGameState (selected); // overwrite
			//TODO ask if you want to overwrite
		} else {
			SaveController.Instance.SaveGameState (saveGameInput.text);
		}
	}
	public void OnIslandLoadPressed(){
		if(selected==null){
			return;
		}
		//TODO ASK IF he wants to load it
		//and warn losing ansaved data

		if (EditorController.Instance != null)
			EditorController.Instance.LoadWorld (selected);
		else
			GameObject.FindObjectOfType<ChangeScene> ().ChangeToGameStateLoadScreen ();
	}
	public void OnIslandSaveClick(){
		if(selected!=null&&(saveGameInput.text==null||saveGameInput.text=="")){
			EditorController.Instance.SaveWorld (selected); // overwrite
			//TODO ask if you want to overwrite
		} else {
			EditorController.Instance.SaveWorld (saveGameInput.text);
		}
	}
}
