using Andja.Model;
using Andja.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {

    public class WorkerSpriteController : MonoBehaviour {
        private Dictionary<string, Sprite> _workerSprites;
        public Dictionary<Worker, GameObject> WorkerToGO;
        public List<Worker> loadedWorker;
        public static WorkerSpriteController Instance { get; protected set; }

        // Use this for initialization
        public void Start() {
            Instance = this;
            WorkerToGO = new Dictionary<Worker, GameObject>();
            LoadSprites();
            loadedWorker = SaveController.GetLoadWorker();
            if (loadedWorker != null) {
                foreach (var item in loadedWorker.Where(item => item.Home.IsDestroyed == false)) {
                    OnWorkerCreated(item);
                }
            }
            World.Current.RegisterWorkerCreated(OnWorkerCreated);
        }

        //public void Update() {
            //if worker change they gonna be created if they dont exist
            //maybe they should be created if NOT updated AND they are on screen
            //TODO rethink this
        //}

        private void OnWorkerCreated(Worker worker) {
            // Register our callback so that our GameObject gets updated whenever
            // the object's into changes.
            worker.RegisterOnChangedCallback(OnWorkerChanged);
            worker.RegisterOnDestroyCallback(OnWorkerDestroy);

            GameObject go = new GameObject();
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            WorkerToGO.Add(worker, go);
            go.name = worker.Home + " - " + worker.ID;
            go.transform.position = new Vector3(worker.X, worker.Y, 0);
            Quaternion q = go.transform.rotation;
            q.eulerAngles = new Vector3(0, 0, worker.Rotation);
            go.transform.rotation = q;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _workerSprites[worker.ToWorkSprites];
            sr.sortingLayerName = "Persons";
            if (FogOfWarController.FogOfWarOn) {
                if (FogOfWarController.IsFogOfWarAlways) {
                    sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                }
            }
            //SOUND PART -- IMPORTANT
            SoundController.Instance.OnWorkerCreated(worker, go);
        }

        private void OnWorkerChanged(Worker w) {
            if (WorkerToGO.ContainsKey(w) == false) {
                if (CameraController.Instance.CameraViewRange.Contains(new Vector2(w.X, w.Y))) {
                    OnWorkerCreated(w);
                }
                //			Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
                return;
            }
            GameObject charGo = WorkerToGO[w];
            if (w.IsFull) {
                charGo.GetComponent<SpriteRenderer>().sprite = _workerSprites[w.FromWorkSprites];
            }
            charGo.transform.position = new Vector3(w.X, w.Y, 0);
            Quaternion q = charGo.transform.rotation;
            q.eulerAngles = new Vector3(0, 0, w.Rotation);
            charGo.transform.rotation = q;
        }

        private void OnWorkerDestroy(Worker w) {
            if (WorkerToGO.ContainsKey(w) == false) {
                //Debug.LogError("OnWorkerDestroy.");
                return;
            }
            GameObject.Destroy(WorkerToGO[w]);
            WorkerToGO.Remove(w);
        }

        private void LoadSprites() {
            _workerSprites = new Dictionary<string, Sprite>();
            Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Worker/");
            foreach (Sprite s in sprites) {
                _workerSprites[s.name] = s;
            }
            Sprite[] custom = ModLoader.LoadSprites(SpriteType.Worker);
            if (custom == null)
                return;
            foreach (Sprite s in custom) {
                _workerSprites[s.name] = s;
            }
        }

        public void OnDestroy() {
            Instance = null;
            World.Current.UnregisterWorkerCreated(OnWorkerCreated);
        }
    }
}