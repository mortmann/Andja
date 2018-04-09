using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NewIsland : MonoBehaviour {
	public InputField height;
	public InputField width;
	public Dropdown zone;
	public Button create; 
	// Use this for initialization
	void Start () {
		//TODO create new island after these standards
		create.onClick.AddListener (OnCreateClick);
	}
	
	public void OnCreateClick(){
		int h = int.Parse ( height.text);
		int w = int.Parse ( width.text );
		Climate cli = (Climate)zone.value;
		StartCoroutine( EditorController.Instance.NewIsland (w,h,cli) );
	}
}
