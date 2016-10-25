using UnityEngine;
using System.Collections.Generic;

public class KeyInputSettings : MonoBehaviour {

	public Transform contentTransform;
	public GameObject keyPrefab;
	Dictionary<string,KeyInputSingle> buttonNameToGO;
	string selectedButton;
	bool primaryChange;
	// Use this for initialization
	void Start () {
		new InputHandler ();
		buttonNameToGO = new Dictionary<string, KeyInputSingle> ();
		foreach (Transform item in contentTransform) {
			Destroy (item.gameObject);
		}
		foreach (string item in InputHandler.GetPrimaryBinds ().Keys) {
			GameObject g = Instantiate (keyPrefab);

			if(InputHandler.GetSecondaryBinds ().ContainsKey (item)==false){
				g.GetComponent<KeyInputSingle>().SetUp (item,""+InputHandler.GetPrimaryBinds ()[item],"-",OnClickButton);
			} else {
				g.GetComponent<KeyInputSingle>().SetUp (item,""+InputHandler.GetPrimaryBinds ()[item],""+InputHandler.GetSecondaryBinds ()[item],OnClickButton);
			}
			g.transform.SetParent (contentTransform);
			buttonNameToGO.Add (item,g.GetComponent<KeyInputSingle>());
		}
	
	}
	public void OnClickButton(string name, bool primary){
		selectedButton = name;
		primaryChange = primary;
		Cursor.visible = false;
	}
	// Update is called once per frame
	void Update () {
		
	}
	public void OnGUI(){
		if (Input.GetKeyDown (KeyCode.Escape)) {
			Cursor.visible = true;
		} else
			if (Event.current != null && (Event.current.isKey)) {
				if(selectedButton==null || selectedButton==""){
					return;
				}
				KeyCode s = Event.current.keyCode;
				buttonNameToGO[selectedButton].ChangeButtonText (primaryChange,""+s);
				if(primaryChange){
					InputHandler.ChangePrimaryNameToKey (selectedButton,s);
				} else {
					InputHandler.ChangeSecondaryNameToKey (selectedButton,s);
				}
				Cursor.visible = true;
				selectedButton = null;
				InputHandler.SaveInputSchema (Application.dataPath.Replace ("/Assets",""));
			}
	}
}
