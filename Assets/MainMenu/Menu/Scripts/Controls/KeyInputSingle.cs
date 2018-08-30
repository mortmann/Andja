using UnityEngine;
using System.Collections;
using UnityEngine.UI ;
using System;
public class KeyInputSingle : MonoBehaviour {

	public Text keyName;
	public Button primaryButton;
	public Button secondaryButton;
	Action<InputName, bool> OnClickButton;
    InputHandler.KeyBind item;
    InputName inputName;
    Text primaryText;
    Text secondaryText;

    public void SetUp(InputName inputName, InputHandler.KeyBind item, Action<InputName, bool> OnClickButton){
        this.inputName = inputName;
        keyName.text = inputName.ToString();
        primaryText = primaryButton.GetComponentInChildren<Text>();
        secondaryText = secondaryButton.GetComponentInChildren<Text>();
        primaryText.text = item.GetPrimaryString();
        secondaryText.text = item.GetSecondaryString();
        this.item = item;

        primaryButton.onClick.AddListener (delegate {
			OnClick (true);
		});
		secondaryButton.onClick.AddListener (delegate {
			OnClick (false);
		});
		this.OnClickButton = OnClickButton;
	}
    private void Update() {
        primaryText.text = item.GetPrimaryString();
        secondaryText.text = item.GetSecondaryString();
    }
    public void OnClick(bool primary){
		OnClickButton (inputName, primary);
	}

	public void ChangeButtonText(bool primary, string text){
		if(primary){
			primaryButton.GetComponentInChildren<Text> ().text = text;
		} else {
			secondaryButton.GetComponentInChildren<Text> ().text = text;
		}
	}

}
