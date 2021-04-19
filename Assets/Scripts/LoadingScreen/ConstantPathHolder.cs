using UnityEngine;

namespace Andja {

    public class ConstantPathHolder : MonoBehaviour {
        public static string ApplicationDataPath;

        public static string StreamingAssets { get; internal set; }

        // Use this for initialization
        private void Awake() {
            ApplicationDataPath = Application.dataPath;
            StreamingAssets = Application.streamingAssetsPath;
        }
    }
}