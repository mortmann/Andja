using Andja.Model;
using Andja.Utility;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller {

    public class WorkerSpriteController : MonoBehaviour {
        private Dictionary<string, Sprite> workerSprites;
        public Dictionary<Worker, GameObject> workerToGO;
        private CameraController cc;
        public List<Worker> loadedWorker;

        // Use this for initialization
        private void Start() {
            workerToGO = new Dictionary<Worker, GameObject>();
            LoadSprites();
            cc = CameraController.Instance;
            loadedWorker = SaveController.GetLoadWorker();
            if (loadedWorker != null) {
                foreach (Worker item in loadedWorker) {
                    if (item.Home.IsDestroyed)
                        continue;
                    OnWorkerCreated(item);
                }
            }
            World.Current.RegisterWorkerCreated(OnWorkerCreated);
        }

        // Update is called once per frame
        private void Update() {
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
            go.name = worker.Home + " - " + worker.ID;
            go.transform.position = new Vector3(worker.X, worker.Y, 0);
            Quaternion q = go.transform.rotation;
            q.eulerAngles = new Vector3(0, 0, worker.Rotation);
            go.transform.rotation = q;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = workerSprites[worker.ToWorkSprites];
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
            if (workerToGO.ContainsKey(w) == false) {
                if (cc.CameraViewRange.Contains(new Vector2(w.X, w.Y))) {
                    OnWorkerCreated(w);
                }
                //			Debug.LogError("OnCharacterChanged -- trying to change visuals for character not in our map.");
                return;
            }
            GameObject char_go = workerToGO[w];
            if (w.IsFull) {
                char_go.GetComponent<SpriteRenderer>().sprite = workerSprites[w.FromWorkSprites];
            }
            char_go.transform.position = new Vector3(w.X, w.Y, 0);
            Quaternion q = char_go.transform.rotation;
            q.eulerAngles = new Vector3(0, 0, w.Rotation);
            char_go.transform.rotation = q;
        }

        private void OnWorkerDestroy(Worker w) {
            if (workerToGO.ContainsKey(w) == false) {
                //Debug.LogError("OnWorkerDestroy.");
                return;
            }
            GameObject.Destroy(workerToGO[w]);
            workerToGO.Remove(w);
        }

        private void LoadSprites() {
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

        private void OnDestroy() {
            World.Current.UnregisterWorkerCreated(OnWorkerCreated);
        }
    }
}