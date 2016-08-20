using UnityEngine;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class EditorStructureSpriteController : MonoBehaviour {
	public Dictionary<EditorTile, GameObject> structureGameObjectMap;
	public Dictionary<int, Structure> structurePrototypes;
	public Dictionary<string, Sprite> structureSprites = new Dictionary<string, Sprite>();
	public static EditorStructureSpriteController Instance;
	public int growableLevel;
	// Use this for initialization
	void Start () {
		if (Instance != null) {
			Debug.LogError("There should never be two world controllers.");
		}
		Instance = this;
		LoadSprites ();
//		XmlDocument xmlDoc = new XmlDocument();
//		ta = ((TextAsset)Resources.Load("XMLs/growables", typeof(TextAsset)));
//		xmlDoc.LoadXml(ta.text); // load the file.
//		ReadGrowables (xmlDoc);
		structureGameObjectMap = new Dictionary<EditorTile, GameObject> ();
		structurePrototypes = new Dictionary<int, Structure> ();
		structurePrototypes.Add (3, new Growable (3,"tree",null));
		foreach (EditorTile item in EditorController.Instance.editorIsland.structures.Keys) {
			growableLevel = EditorController.Instance.editorIsland.structures [item] [1];
			OnStructureCreated (EditorController.Instance.editorIsland.structures [item] [0],item);
		}

		EditorController.Instance.RegisterOnStructureCreated (OnStructureCreated);
		EditorController.Instance.RegisterOnStructureDestroyed (OnStructureDestroy);
	}
	void OnStructureCreated(int structure, EditorTile t){
		GameObject go = new GameObject ();
		go.transform.position = new Vector3 (t.X,t.Y);
		go.transform.Rotate (Vector3.forward); 
		go.transform.SetParent (this.transform,true);
		go.name = "Structure_" + t.X + "_" + t.Y;
		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sortingLayerName = "Structures";
		sr.sprite = structureSprites[structurePrototypes[structure].name+"_"+growableLevel];
		structureGameObjectMap.Add (t,go); 
	}
	void OnStructureDestroy(EditorTile t){
		if(structureGameObjectMap.ContainsKey (t)==false){
			return;
		}
		GameObject.Destroy (structureGameObjectMap [t]);
		structureGameObjectMap.Remove (t);
	}
	// Update is called once per frame
	void Update () {
	
	}
	private void ReadGrowables(XmlDocument xmlDoc){
		foreach(XmlElement node in xmlDoc.SelectNodes("Buildings/Growable")){
			int ID = int.Parse(node.GetAttribute("ID"));
			string name = node.SelectSingleNode("EN"+ "_Name").InnerText;
			Growable growable = new Growable (ID,name,null,null);
			structurePrototypes [ID] = growable;
		}
	}
	void LoadSprites() {
		structureSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Structures/");
		foreach (Sprite s in sprites) {
			//			Debug.Log (s.name);
			structureSprites[s.name] = s;
		}
	}
	public int GetGrowableStages(int id){
		return ((Growable)structurePrototypes [id]).ageStages;
	}

}
