using UnityEngine;
using System.Collections;
using UnityEngine.UI ;
using System;
public class KeyInputSingle : MonoBehaviour {

	public Text keyName;
	public Button primaryButton;
	public Button secondaryButton;
	Action<string,bool> OnClickButton;

	public void SetUp(string buttonName, string primary, string secondary, Action<string,bool> OnClickButton){
		keyName.text = buttonName;
		primaryButton.GetComponentInChildren<Text> ().text = primary;
		secondaryButton.GetComponentInChildren<Text> ().text = secondary;
		primaryButton.onClick.AddListener (delegate {
			OnClick (true);
		});
		secondaryButton.onClick.AddListener (delegate {
			OnClick (false);
		});
		this.OnClickButton = OnClickButton;

	}
	
	public void OnClick(bool primary){
		OnClickButton (keyName.text,primary);
	}

	public void ChangeButtonText(bool primary, string text){
		if(primary){
			primaryButton.GetComponentInChildren<Text> ().text = text;
		} else {
			secondaryButton.GetComponentInChildren<Text> ().text = text;
		}
	}

}
