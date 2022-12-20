using Andja.Editor;
using Andja.Model;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
namespace Andja.Controller {
    [Serializable]
    public class SaveMetaData {
        public string saveName;
        public string safefileversion;
        public DateTime saveTime;
        public Difficulty? difficulty;
        public GameType? saveFileType;
        public float playTime;
        public Size? size;
        public Climate? climate;
        public bool isInDebugMode;
        public List<string> usedMods;
        protected SaveMetaData() { }
        internal static SaveMetaData GetFromFile(string finalMetaStatePath) {
            return JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(finalMetaStatePath), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
        }
        public string Serialize() {
            return JsonConvert.SerializeObject(this, Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                    }
            );
        }

        internal static SaveMetaData GetFromFileZip(string file) {
            return JsonConvert.DeserializeObject<SaveMetaData>(FileUtil.Unzip(File.ReadAllBytes(file)), 
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }
            );
        }
        public static SaveMetaData[] GetMetaFiles(bool editor) {
            string path = editor ? SaveController.GetIslandSavePath() : SaveController.GetSaveGamesPath();
            string metaEnding = editor ? SaveController.IslandMetaFileEnding : SaveController.SaveMetaFileEnding;
            if (Directory.Exists(path) == false) {
                Directory.CreateDirectory(path);
            }
            List<SaveMetaData> saveMetaDatas = new List<SaveMetaData>();
            foreach (string file in Directory.GetFiles(path, "*" + metaEnding, SearchOption.AllDirectories)) {
                SaveMetaData metaData = null;
                try {
                    metaData = SaveMetaData.GetFromFile(file);
                }
                catch {
                    metaData = SaveMetaData.GetFromFileZip(file);
                }
                if (metaData == null)
                    continue;
                saveMetaDatas.Add(metaData);
            }
            if (editor) {
                saveMetaDatas.RemoveAll(x => x.safefileversion != SaveController.IslandSaveFileVersion);
            }
            else {
                saveMetaDatas.RemoveAll(x => x.safefileversion != SaveController.SaveFileVersion);
            }
            return saveMetaDatas.ToArray();
        }

        internal static SaveMetaData CreateGameData() {
            return new SaveMetaData {
                safefileversion = SaveController.SaveFileVersion,
                saveName = SaveController.SaveName,
                saveTime = DateTime.Now,
                saveFileType = GameData.Instance.saveFileType,
                playTime = GameData.Instance.playTime,
                difficulty = GameData.Instance.difficulty,
                isInDebugMode = SaveController.DebugModeSave,
                usedMods = ModLoader.GetActiveMods()
            };
        }

        internal static SaveMetaData CreateIslandData() {
            return new SaveMetaData {
                safefileversion = SaveController.IslandSaveFileVersion,
                saveName = SaveController.SaveName,
                saveTime = DateTime.Now,
                climate = EditorController.climate,
                size = EditorController.Instance.IslandSize
            };
        }
    }
}