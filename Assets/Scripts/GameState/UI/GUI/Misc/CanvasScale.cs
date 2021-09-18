using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CanvasScale : MonoBehaviour {
        private static Vector2 referenceResolution;
        public static float Width => Screen.currentResolution.width / referenceResolution.x;
        public static float Height => Screen.currentResolution.height / referenceResolution.y;
        public static Vector2 Vector => new Vector2(Width, Height);

        private void Start() {
            referenceResolution = GetComponent<CanvasScaler>().referenceResolution;
        }
    }
}