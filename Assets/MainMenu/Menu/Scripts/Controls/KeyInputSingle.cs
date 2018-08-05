using UnityEngine;
using System.Collections;
using UnityEngine.UI ;
using System;
public class KeyInputSingle : MonoBehaviour {

	public Text keyName;
	public Button primaryButton;
	public Button secondaryButton;
	Action<InputName, bool> OnClickButton;
    InputName buttonName;

    public void SetUp(InputName buttonName, string primary, string secondary, Action<InputName, bool> OnClickButton){
        this.buttonName = buttonName;
        keyName.text = buttonName.ToString();
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
		OnClickButton (buttonName, primary);
	}

	public void ChangeButtonText(bool primary, string text){
		if(primary){
			primaryButton.GetComponentInChildren<Text> ().text = text;
		} else {
			secondaryButton.GetComponentInChildren<Text> ().text = text;
		}
	}

}
