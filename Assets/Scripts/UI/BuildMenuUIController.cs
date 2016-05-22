using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class BuildMenuUIController : MonoBehaviour {
	public RectTransform buttoncontent;
    public Button buttonPrefab;
	public Dictionary<string,Button> nameToButtonMap;
	public Dictionary<string,int> nameToIDMap;
	BuildController bc;
	Button oldButton;
    // Use this for initialization
    void Start () {
		nameToButtonMap = new Dictionary<string, Button> ();
		nameToIDMap = new Dictionary<string, int> ();
		bc = GameObject.FindObjectOfType<BuildController> ();
		foreach (Structure s in bc.structurePrototypes.Values) {
			Button b = Instantiate(buttonPrefab);
			b.name = s.name;
			b.GetComponentInChildren<Text>().text = s.name;
			b.transform.SetParent(buttoncontent.transform);
			b.GetComponent<Button>().onClick.AddListener(() => {OnClick(b.name);});
			b.GetComponent<Image> ().color = Color.white;
			nameToButtonMap [b.name] = b;
			nameToIDMap[b.name] =s.ID;
		}
    }

	public void OnClick(string name){
		if(nameToButtonMap.ContainsKey (name) == false){
			Debug.LogError ("nameToButtonMap doesnt contain the pressed button");
			return;
		}
		if(oldButton!=null){
			oldButton.GetComponent<Image> ().color = Color.white;
		}
		oldButton = nameToButtonMap [name];
		nameToButtonMap [name].GetComponent<Image> ().color = Color.red;
		bc.OnClick (nameToIDMap[name]);
	}

}
