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
    private void OnWorkerCreated(Worker worker) {
        // Register our callback so that our GameObject gets updated whenever
        // the object's into changes.
        worker.RegisterOnChangedCallback(OnWorkerChanged);
        worker.RegisterOnDestroyCallback(OnWorkerDestroy);

        GameObject go = new GameObject();
        go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        workerToGO.Add(worker, go);
        go.name = " - Worker";
        go.transform.position = new Vector3(worker.X, worker.Y, 0);
        Quaternion q = go.transform.rotation;
        q.eulerAngles = new Vector3(0, 0, worker.Rotation);
        go.transform.rotation = q;
        go.transform.SetParent(this.transform, true);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = workerSprites["worker"];
        sr.sortingLayerName = "Persons";
        //SOUND PART -- IMPORTANT
        SoundController.Instance.OnWorkerCreated(worker, go);
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
