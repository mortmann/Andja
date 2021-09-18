using UnityEngine;
using UnityEngine.UI;
using Andja.Controller;

namespace Andja.UI.Menu {
    public class VersionText : MonoBehaviour {
            
        public bool SaveFileVersion;
        public bool IslandFileVersion;
        void Start() {
            Text text = GetComponent<Text>();
            text.text = "ALPHA " + Application.version;
            if(SaveFileVersion && IslandFileVersion) {
                text.text += " @(" + SaveController.SaveFileVersion + " | " + SaveController.IslandSaveFileVersion + ")";
            } else {
                if (SaveFileVersion) {
                    text.text += " @(" + SaveController.SaveFileVersion+")";
                }
                if (IslandFileVersion) {
                    text.text += " @(" + SaveController.IslandSaveFileVersion + ")";
                }
            }
        }

    }
}