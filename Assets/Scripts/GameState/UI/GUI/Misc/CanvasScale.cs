using System;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class CanvasScale : MonoBehaviour {
        private static Vector2 referenceResolution;
        public static float Width => Screen.width / referenceResolution.x;
        public static float Height => Screen.height / referenceResolution.y;
        public static Vector2 Vector => new Vector2(Width, Height);
        int lastScreenWidth = 0;
        int lastScreenHeight = 0;
        static Action cbResolutionChange;

        private void Start() {
            referenceResolution = GetComponent<CanvasScaler>().referenceResolution;

        }

        internal static void RegisterOnResolutionChange(Action onResolutionChange) {
            cbResolutionChange += onResolutionChange;
        }

        private void Update() {
            if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height) {
                referenceResolution = GetComponentInParent<CanvasScaler>().referenceResolution;
                cbResolutionChange?.Invoke();
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
            }
        }
    }
}