using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class SaveLoadUIScript : MonoBehaviour {
    public SaveDetails saveInfo;

    public GameObject listPrefab;
	public GameObject canvasGO;
	string selected;
	public InputField saveGameInput;
	GameObject selectedGO;
    Dictionary<string, FileInfo> nameToFile;

	// Use this for initialization
	void OnEnable () {
        nameToFile = new Dictionary<string, FileInfo>();
		foreach (Transform item in canvasGO.transform) {
			GameObject.Destroy (item.gameObject);
		}
		string directoryPath="";
		if (EditorController.IsEditor == false) {
			directoryPath = SaveController.Instance.GetSaveGamesPath ();
		} else {
			directoryPath = EditorController.Instance.GetSaveGamesPath ();
		}
		
		DirectoryInfo saveDir = new DirectoryInfo( directoryPath );
		if(saveDir.Exists==false){
			saveDir.Create ();
		}
		FileInfo[] saveGames;
		if(EditorController.IsEditor == false){
			saveGames = saveDir.GetFiles("*.sav").OrderBy( f => f.CreationTime ).ToArray();
		} else {
			saveGames = saveDir.GetFiles("*.isl",SearchOption.AllDirectories).OrderBy( f => f.CreationTime ).ToArray();
		}

		// Build file list by instantiating fileListItemPrefab
		for(int i = saveGames.Length-1; i>=0 ; i-- ) {
			GameObject go = (GameObject)GameObject.Instantiate(listPrefab);

			// Make sure this gameobject is a child of our list box
			go.transform.SetParent( canvasGO.transform );

			EventTrigger trigger = go.GetComponent<EventTrigger> ();
            EventTrigger.Entry entry = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            string name = Path.GetFileNameWithoutExtension( saveGames[i].FullName);
			entry.callback.AddListener ((data)=>{OnSaveGameSelect (name,go);});
			trigger.triggers.Add( entry );
			string date = saveGames [i].CreationTime.ToString ("dd-MM-yyyy");
			go.GetComponentInChildren<Text>().text = Path.GetFileNameWithoutExtension( saveGames[i].FullName) +" ["+ date +"]";

            nameToFile.Add(name, saveGames[i]);
		}
		if(saveGameInput!=null){
			saveGameInput.onValueChanged.AddListener ((data)=>OnInputChange());			
		}

	}
	public void OnInputChange(){
		if (selectedGO != null)
			selectedGO.GetComponent<SelectableScript> ().OnDeselectCall ();	
		selectedGO = null;
	}
	public void OnSaveGameSelect(string fi,GameObject go){
		if (go == selectedGO)
			return;
		selected = fi;
		if(saveGameInput!=null && saveGameInput.IsActive()){
			saveGameInput.text = fi;
		}
        if(saveInfo!=null)
            saveInfo.ShowDetails(nameToFile[fi]);

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

		if(EditorController.IsEditor == false){
			GameLoad ();
		} else {
			EditorLoad ();
		}

	}
	public void OnSavePressed(){
		string name = "";
		if(selected!=null&&(saveGameInput.text==null||saveGameInput.text=="")){
			name = selected; //SaveController.Instance.SaveGameState (selected); // overwrite
			//TODO ask if you want to overwrite
		} else {
			name = saveGameInput.text;
		}

		if(EditorController.IsEditor == false){
			GameSave (name);
		} else {
			EditorSave (name);
		}


	}

	private void GameSave(string name){
		SaveController.Instance.SaveGameState (name);
	}
	private void EditorSave(string name){
		EditorController.Instance.SaveIslandState (name);
	}
	private void GameLoad(){
		GameDataHolder.Instance.loadsavegame = selected;
		if (WorldController.Instance != null)
			WorldController.Instance.LoadWorld ();
		else
			GameObject.FindObjectOfType<MenuController> ().ChangeToGameStateLoadScreen ();
	}
	private void EditorLoad(){
		EditorController.Instance.LoadIsland (nameToFile[selected]);
	}
}
