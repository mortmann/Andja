using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RessourcesSetter : MonoBehaviour {

    public Transform Content;
    public GameObject ResourceSet;

    // Use this for initialization
    void Start () {
		foreach(Item item in PrototypController.Instance.MineableItems) {
            GameObject go = Instantiate(ResourceSet);
            go.transform.SetParent(Content);
            go.GetComponentInChildren<Text>().text = item.Name;
            InputField amountField = go.GetComponentInChildren<InputField>();
            amountField.onEndEdit.AddListener(x => {
                if (x.Length == 0)
                    amountField.text = "0";
                int amount = 0;
                int.TryParse(x, out amount);
                EditorController.Instance.OnRessourceChange(item.ID, amount);
            });
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
