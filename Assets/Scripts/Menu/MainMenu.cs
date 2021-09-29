using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace Andja.UI.Menu {
    public class MainMenu : MonoBehaviour {
        public static MainMenu Instance;
        public static bool IsMainMenu => Instance != null;
        public static bool JustOpenedGame = true;
        static PlayerData data;
        public string LastPlayedSavefile => data.lastPlayedSavefile;
        public bool LastIsEditorSave => data.lastIsEditorSave;

        void Start() {
            Instance = this;
            if(File.Exists(GetPlayerDataPath())) {
                data = JsonConvert.DeserializeObject<PlayerData>(File.ReadAllText(GetPlayerDataPath()));
            } else {
                data = new PlayerData();
            }
        }

        private void OnDestroy() {
            Save();
            JustOpenedGame = false;
            Instance = null;
        }

        public static void SetLastPlayed(string name, bool isEditorSave = false) {
            if (data == null)
                return;
            data.lastIsEditorSave = isEditorSave;
            data.lastPlayedSavefile = name;
            Save();
        }
        static void Save() {
            string save = JsonConvert.SerializeObject(data);
            File.WriteAllText(GetPlayerDataPath(), save);
        }
        public static string GetPlayerDataPath() {
            //TODO FIXME change this to documentspath
            return Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace("/Assets", ""), "player.data");
        }
        /// <summary>
        /// This could be all saved inside of playerprefs
        /// But i do not want to have it carry over between different Builds!
        /// </summary>
        [JsonObject]
        class PlayerData {
            public bool lastIsEditorSave;
            public string lastPlayedSavefile;
        }
    }

}
