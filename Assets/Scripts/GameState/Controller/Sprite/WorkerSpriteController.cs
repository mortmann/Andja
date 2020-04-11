using UnityEngine;
using System.Collections.Generic;

public class WorkerSpriteController : MonoBehaviour {
    private Dictionary<string, Sprite> workerSprites;
    public Dictionary<Worker, GameObject> workerToGO;
    CameraController cc;
    public List<Worker> loadedWorker;
    // Use this for initialization
    void Start() {
        workerToGO = new Dictionary<Worker, GameObject>();
        LoadSprites();
        cc = CameraController.Instance;
        loadedWorker = SaveController.GetLoadWorker();
        if (loadedWorker != null) {
            foreach (Worker item in loadedWorker) {
                OnWorkerCreated(item);
            }
        }
        World.Current.RegisterWorkerCreated(OnWorkerCreated);

    }
    // Update is called once per frame
    void Update() {
        //if worker change they gonna be created if they dont exist
        //maybe they should be created if NOT updated AND they are on screen
        //TODO rethink this
    }
    private void OnWorkerCreated(Worker w) {
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        w.RegisterOnChangedCallback(OnWorkerChanged);
        w.RegisterOnDestroyCallback(OnWorkerDestroy);
        //		if (cc.CameraViewRange.Contains (new Vector2 (w.X, w.Y))==false){
        //			return;
        //		}

        // Create a visual GameObject linked to this data.
        GameObject char_go = new GameObject();
        char_go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        // Add our tile/GO pair to the dictionary.
        workerToGO.Add(w, char_go);
        char_go.name = " - Worker";
        char_go.transform.position = new Vector3(w.X, w.Y, 0);
        Quaternion q = char_go.transform.rotation;
        q.eulerAngles = new Vector3(0, 0, w.Rotation);
        char_go.transform.rotation = q;
        char_go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = char_go.AddComponent<SpriteRenderer>();
        sr.sprite = workerSprites["worker"];
        sr.sortingLayerName = "Persons";
    }
    void OnWorkerChanged(Worker w) {
        if (workerToGO.ContainsKey(w) == false) {
            if (cc.CameraViewRange.Contains(new Vector2(w.X, w.Y))) {
                OnWorkerCreated(w);
            }
            //			Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
            return;
        }
        GameObject char_go = workerToGO[w];
        char_go.transform.position = new Vector3(w.X, w.Y, 0);
        Quaternion q = char_go.transform.rotation;
        q.eulerAngles = new Vector3(0, 0, w.Rotation);
        char_go.transform.rotation = q;
    }
    void OnWorkerDestroy(Worker w) {
        if (workerToGO.ContainsKey(w) == false) {
            //Debug.LogError("OnWorkerDestroy.");
            return;
        }
        GameObject.Destroy(workerToGO[w]);
        workerToGO.Remove(w);
    }
    void LoadSprites() {
        workerSprites = new Dictionary<string, Sprite>();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Worker/");
        foreach (Sprite s in sprites) {
            workerSprites[s.name] = s;
        }
        Sprite[] custom = ModLoader.LoadSprites(SpriteType.Worker);
        if (custom == null)
            return;
        foreach (Sprite s in custom) {
            workerSprites[s.name] = s;
        }
    }
    void OnDestroy() {
        World.Current.UnregisterWorkerCreated(OnWorkerCreated);
    }
}
