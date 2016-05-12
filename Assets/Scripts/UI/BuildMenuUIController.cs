using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BuildMenuUIController : MonoBehaviour {
	public RectTransform buttoncontent;
    public Button buttonPrefab;
	BuildController bc;
	Button oldButton;
    // Use this for initialization
    void Start () {
		bc = GameObject.FindObjectOfType<BuildController> ();
		foreach (Structure s in bc.structurePrototypes.Values) {
			Button b = Instantiate(buttonPrefab);
			b.name = s.name;
			b.GetComponentInChildren<Text>().text = s.name;
			b.transform.SetParent(buttoncontent.transform);
			b.GetComponent<Button>().onClick.AddListener(() => {OnClick(b.name,b);});
		}
    }

	public void OnClick(string name, Button button){
		if(oldButton!=null){
			ColorBlock c = oldButton.colors;
			c.normalColor = Color.white;
			button.colors = c;
		}
		oldButton = button;
		ColorBlock cs = button.colors;
		cs.normalColor = Color.red;
		button.colors = cs;
		bc.OnClick (name);
	}

}
