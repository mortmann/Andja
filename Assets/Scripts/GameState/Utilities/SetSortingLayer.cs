using UnityEngine;

namespace Andja.Utility {

    public class SetSortingLayer : MonoBehaviour {
        public string sortingLayerName = "default";

        // Use this for initialization
        private void Start() {
            GetComponent<Renderer>().sortingLayerName = sortingLayerName;
        }
    }
}