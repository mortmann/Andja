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
		foreach (InputHandler.KeyBind item in InputHandler.GetBinds().Values) {
//			InputHandler.KeyBind item = InputHandler.GetBinds () [s];
			GameObject g = Instantiate (keyPrefab);
			g.GetComponent<KeyInputSingle>().SetUp (item.name,item.GetPrimaryString(),item.GetSecondaryString(),OnClickButton);
			g.transform.SetParent (contentTransform);
			buttonNameToGO.Add (item.name,g.GetComponent<KeyInputSingle>());
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
				if(s == InputHandler.KeyBind.notSetCode){
					return;
				}
				buttonNameToGO[selectedButton].ChangeButtonText (primaryChange,""+s);
				if(primaryChange){
					InputHandler.ChangePrimaryNameToKey (selectedButton,s);
				} else {
					InputHandler.ChangeSecondaryNameToKey (selectedButton,s);
				}
				Cursor.visible = true;
				selectedButton = null;
				InputHandler.SaveInputSchema ();
			}
	}
}
