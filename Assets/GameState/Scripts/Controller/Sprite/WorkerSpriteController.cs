using UnityEngine;
using System.Collections.Generic;

public class WorkerSpriteController : MonoBehaviour {
	private Dictionary<string, Sprite> unitSprites;
	private Dictionary<Worker, GameObject> workerToGO;

	// Use this for initialization
	void Start () {
		workerToGO = new Dictionary<Worker, GameObject> ();
		LoadSprites ();
		WorldController.Instance.world.RegisterWorkerCreated (OnWorkerCreated);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	private void OnWorkerCreated(Worker w) {
		// Create a visual GameObject linked to this data.
		GameObject char_go = new GameObject();

		// Add our tile/GO pair to the dictionary.
		workerToGO.Add(w, char_go);

		char_go.name = w.myHome.name + " - Worker";
		char_go.transform.position = new Vector3(w.X,w.Y,0);
		Vector3 v = char_go.transform.rotation.eulerAngles;
		char_go.transform.rotation.eulerAngles.Set(v.x,v.y,w.Z);
		char_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = unitSprites["worker"];
        sr.sortingLayerName = "Persons";

		// Register our callback so that our GameObject gets updated whenever
		// the object's into changes.
		w.RegisterOnChangedCallback(OnWorkerChanged);
		w.RegisterOnDestroyCallback(OnWorkerDestroy);

	}
	void OnWorkerChanged(Worker w) {
		if (workerToGO.ContainsKey(w) == false) {
			Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
			return;
		}
		GameObject char_go = workerToGO[w];
		char_go.transform.position = new Vector3( w.X, w.Y, 0);
	}
	void OnWorkerDestroy(Worker w) {
		if (workerToGO.ContainsKey(w) == false) {
			Debug.LogError("OnWorkerDestroy.");
			return;
		}
		GameObject.Destroy (workerToGO [w]);
		workerToGO.Remove (w);
	}
	void LoadSprites() {
		unitSprites = new Dictionary<string, Sprite>();
		Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Worker/");
		foreach (Sprite s in sprites) {
			unitSprites[s.name] = s;
		}
	}
}
