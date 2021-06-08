using Andja.Editor;
using Andja.Model;
using Andja.Model.Generator;
using Andja.Utility;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Controller {
    /// <summary>
    /// Handles Saving and Loading for Game and Editor.
    /// Creates Screenshot, Meta and Savefile.
    /// Saves as JSON.
    /// </summary>
    public class SaveController : MonoBehaviour {
        public static SaveController Instance;

        private string quickSaveName = "quicksave";//TODO CHANGE THIS TO smth not hardcoded
        public static bool IsLoadingSave = false;
        public bool IsDone = false;

        public float loadingPercantage = 0;
        private const string saveMetaFileEnding = ".meta";
        private const string saveFileEnding = ".sav";
        public const string islandMetaFileEnding = ".islmeta"; // TODO: thing of a better extension
        public const string islandFileEnding = ".isl";
        public const string islandImageEnding = ".png";
        public const string saveFileScreenShotEnding = ".jpg";

        private static List<Worker> loadWorker;
        public static bool DebugModeSave = true;
        public static string SaveName = "unsaved";

        private const string SaveFileVersion = "0.1.10";
        private const string islandSaveFileVersion = "i_0.0.3";
        private float timeToAutoSave = AutoSaveInterval;
        private const float AutoSaveInterval = 15 * 60; // every 15 min -- TODO: add game option to change this
        GameData GDH => GameData.Instance;
        WorldController WC => WorldController.Instance;
        EventController EC => EventController.Instance;
        CameraController CC => CameraController.Instance;
        PlayerController PC => PlayerController.Instance;
        UIController UI => UIController.Instance;
        FogOfWarController FW => FogOfWarController.Instance;

        private float lastSaved = -1;
        public byte[] FogOfWarData = null;

        public bool UnsavedProgress => lastSaved != GameData.Instance.playTime;

        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two SaveController.");
            }
            Instance = this;
            lastSaved = -1;
        }

        // Use this for initialization
        private void Start() {
            if (GDH != null && GDH.Loadsavegame != null && GDH.Loadsavegame.Length > 0) {
                Debug.Log("LOADING SAVEGAME " + GDH.Loadsavegame);
                IsLoadingSave = true;
                StartCoroutine(LoadGameState(GDH.Loadsavegame));
                GameData.setloadsavegame = null;
            }
            else {
                IsLoadingSave = false;
                if (EditorController.IsEditor == false)
                    GDH?.GenerateMap();//just generate new map
            }
        }

        internal static string GetLastSaveName() {
            List<SaveMetaData> saveMetaDatas = new List<SaveMetaData>(GetMetaFiles(false));
            if (saveMetaDatas.Count == 0)
                return null;
            saveMetaDatas.Sort((x, y) => x.saveTime.CompareTo(y.saveTime));
            int value = saveMetaDatas.Count - 1;
            while (saveMetaDatas[value].safefileversion != SaveFileVersion) {
                value--;
                if (value < 0)
                    return null;
            }
            return saveMetaDatas[value].saveName;
        }

        internal void QuickSave() {
            SaveGameState(quickSaveName);
        }

        internal void QuickLoad() {
            //TODO: NEEDS CHECK
            LoadWorld(true);
        }

        public void LoadWorld(bool quickload = false) {
            Debug.Log("LoadWorld button was clicked.");
            if (quickload) {
                GameData.setloadsavegame = quickSaveName;
            }
            // set to loadscreen to reset all data (and purge old references)
            SceneManager.LoadScene("GameStateLoadingScreen");
        }

        public void Update() {
            if (timeToAutoSave > 0) {
                timeToAutoSave -= Time.deltaTime;
                return;
            }
            timeToAutoSave = AutoSaveInterval;
            if (EditorController.IsEditor == false)
                SaveGameState();
            else
                SaveIslandState();
        }

        internal bool DoesGameSaveExist(string name) {
            return File.Exists(Path.Combine(GetSaveGamesPath(), name + saveFileEnding));
        }

        internal static bool DoesEditorSaveExist(string name) {
            return File.Exists(Path.Combine(GetIslandSavePath(), name + islandFileEnding));
        }

        public void DeleteSaveGame(string name) {
            string filePath = Path.Combine(GetSaveGamesPath(), name);
            string saveStatePath = filePath + saveFileEnding;
            string finalMetaStatePath = filePath + saveMetaFileEnding;
            string fileImagePath = filePath + saveFileScreenShotEnding;
            try {
                File.Delete(saveStatePath);
            }
            catch (Exception e) {
                Debug.Log("Deleting File cant be deleted. " + e.Message);
            }
            try {
                File.Delete(finalMetaStatePath);
            }
            catch (Exception e) {
                Debug.Log("Deleting File cant be deleted. " + e.Message);
            }
            try {
                File.Delete(fileImagePath);
            }
            catch (Exception e) {
                Debug.Log("Deleting File cant be deleted. " + e.Message);
            }
        }

        public void SaveIslandState(string name = "autosave") {
            string folderPath = GetIslandSavePath(name);
            string islandStatePath = Path.Combine(folderPath, name + islandFileEnding);
            string finalMetaStatePath = Path.Combine(folderPath, name + islandMetaFileEnding);
            SaveName = name;
            SaveMetaData metaData = new SaveMetaData {
                safefileversion = islandSaveFileVersion,
                saveName = name,
                saveTime = DateTime.Now,
                climate = EditorController.climate,
                size = EditorController.Instance.IslandSize
            };

            string metadata = JsonConvert.SerializeObject(metaData, Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                    }
            );

            EditorController.SaveIsland savestate = EditorController.Instance.GetSaveState();
            string save = JsonConvert.SerializeObject(savestate, Application.isEditor ? Formatting.Indented : Formatting.None,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                //PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                }
            );
            if (Directory.Exists(folderPath) == false) {
                Directory.CreateDirectory(folderPath);
            }
            if (Application.isEditor) {
                File.WriteAllText(islandStatePath, save);
            }
            else {
                File.WriteAllBytes(islandStatePath, Zip(save));
            }
            File.WriteAllText(finalMetaStatePath, metadata);
        }

        public EditorController.SaveIsland GetIslandSave(string name) {
            string saveStatePath = Path.Combine(GetIslandSavePath(name), name + islandFileEnding);
            string finalMetaStatePath = Path.Combine(GetIslandSavePath(name), name + islandMetaFileEnding);

            SaveMetaData metaData = JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(finalMetaStatePath), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
            if (islandSaveFileVersion != metaData.safefileversion) {
                Debug.LogError("Mismatch of SaveFile Versions " + metaData.safefileversion + " & " + islandSaveFileVersion);
                return null;
            }
            EditorController.SaveIsland state = null;
            try {
                state = JsonConvert.DeserializeObject<EditorController.SaveIsland>(Unzip(File.ReadAllBytes(saveStatePath)), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
            }
            catch {
                state = JsonConvert.DeserializeObject<EditorController.SaveIsland>(File.ReadAllText(saveStatePath), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                });
            }
            state.Name = name;
            return state;
        }

        public void LoadIsland(string name) {
            EditorController.Instance.LoadIsland(GetIslandSave(name));
        }

        public static string GetIslandSavePath(string name = null, bool userCustom = false) {
            string path = Path.Combine(ConstantPathHolder.StreamingAssets, "Islands");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
            if (name == null)
                return path;
            return Path.Combine(path, name);
        }

        public static byte[] Zip(string str) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        internal static Sprite GetSaveFileScreenShot(string saveName) {
            string filePath = Path.Combine(GetSaveGamesPath(), saveName + saveFileScreenShotEnding);
            byte[] fileData;
            if (File.Exists(filePath) == false) {
                Debug.Log("Missing Thumbnail for savegame " + filePath);
                return null;
            }
            fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        internal static bool IslandImageExists(string name) {
            return File.Exists(Path.Combine(GetIslandSavePath(name), name + islandImageEnding));
        }

        public static Texture2D LoadIslandImage(string name, string nameAddOn = "") {
            if (IslandImageExists(name) == false) {
                return null;
            }
            Texture2D tex = null;
            byte[] fileData;
            string filePath = Path.Combine(GetIslandSavePath(name), name + nameAddOn + islandImageEnding);
            if (File.Exists(filePath) == false)
                return null;
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return tex;
        }

        public static string Unzip(byte[] bytes) {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                    CopyTo(gs, mso);
                }
                return System.Text.Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static void CopyTo(Stream src, Stream dest) {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static Dictionary<KeyValuePair<Climate, Size>, List<string>> GetIslands() {
            Dictionary<KeyValuePair<Climate, Size>, List<string>> islands = new Dictionary<KeyValuePair<Climate, Size>, List<string>>();
            string[] filePaths = Directory.GetFiles(GetIslandSavePath(), "*" + islandMetaFileEnding, SearchOption.AllDirectories);
            foreach (string file in filePaths) {
                SaveMetaData metaData = null;
                try {
                    metaData = JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(file), new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                }
                catch {
                    continue;
                }
                if (metaData.safefileversion != islandSaveFileVersion) {
                    continue;
                }
                if (metaData.climate == null || metaData.size == null)
                    continue;
                KeyValuePair<Climate, Size> key = new KeyValuePair<Climate, Size>((Climate)metaData.climate, (Size)metaData.size);
                if (islands.ContainsKey(key) == false) {
                    islands.Add(key, new List<string>());
                }
                islands[key].Add(metaData.saveName);
            }
            return islands;
        }

        internal static void SaveIslandImage(string name, byte[] imagePNG, string addon = "") {
            string path = Path.Combine(GetIslandSavePath(name), name + addon + islandImageEnding);
            File.WriteAllBytes(path, imagePNG);
        }

        public static SaveMetaData[] GetMetaFiles(bool editor) {
            string path = editor ? GetIslandSavePath() : GetSaveGamesPath();
            string metaEnding = editor ? islandMetaFileEnding : saveMetaFileEnding;
            if (Directory.Exists(path) == false) {
                Directory.CreateDirectory(path);
            }
            List<SaveMetaData> saveMetaDatas = new List<SaveMetaData>();
            foreach (string file in Directory.GetFiles(path, "*" + metaEnding, SearchOption.AllDirectories)) {
                SaveMetaData metaData = null;
                try {
                    metaData = JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(file), new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                }
                catch {
                    metaData = JsonConvert.DeserializeObject<SaveMetaData>(Unzip(File.ReadAllBytes(file)), new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    });
                }
                if (metaData == null)
                    continue;
                saveMetaDatas.Add(metaData);
            }
            if (editor) {
                saveMetaDatas.RemoveAll(x => x.safefileversion != islandSaveFileVersion);
            }
            else {
                saveMetaDatas.RemoveAll(x => x.safefileversion != SaveFileVersion);
            }
            return saveMetaDatas.ToArray();
        }

        private void OnDestroy() {
            Instance = null;
        }

        public static void AddWorkerForLoad(Worker w) {
            if (loadWorker == null) {
                loadWorker = new List<Worker>();
            }
            loadWorker.Add(w);
        }

        public static List<Worker> GetLoadWorker() {
            if (loadWorker == null)
                return null;
            List<Worker> tempLoadWorker = new List<Worker>(loadWorker);
            loadWorker = null;
            return tempLoadWorker;
        }

        public static string GetSaveGamesPath() {
            //TODO FIXME change this to documentspath
            return Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace("/Assets", ""), "Saves");
        }

        public string[] SaveGameState(string name = "autosave", bool returnSaveInstead = false) {
            SaveName = name;
            //first pause the world so nothing changes and we can save an
            bool wasPaused = WC.IsPaused;
            if (wasPaused == false) {
                WC.IsPaused = true;
            }
            string filePath = Path.Combine(GetSaveGamesPath(), name);
            string saveStatePath = filePath + saveFileEnding;
            string finalMetaStatePath = filePath + saveMetaFileEnding;

            SaveState savestate = new SaveState {
                gamedata = GDH.GetSaveGameData().Serialize(false),
                pcs = PC.GetSavePlayerData().Serialize(false),
                world = WC.GetSaveWorldData().Serialize(true),
                ges = EC.GetSaveGameEventData().Serialize(true),
                camera = CC.GetSaveCamera().Serialize(false),
                ui = UI.GetUISaveData().Serialize(false),
                fw = FogOfWarController.FogOfWarOn ? Encoding.ASCII.GetString(FW.GetFogOfWarBytes()) : null,
            };

            string save = "";
            if (DebugModeSave || returnSaveInstead) {
                foreach (System.Reflection.FieldInfo field in typeof(SaveState).GetFields()) {
                    string bsd = field.GetValue(savestate) as String;
                    save += bsd;
                    save += "##" + Environment.NewLine;
                }
            }
            else {
                save = JsonConvert.SerializeObject(savestate, Formatting.Indented,
                        new JsonSerializerSettings {
                            NullValueHandling = NullValueHandling.Ignore,
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                            TypeNameHandling = TypeNameHandling.Auto
                        }
                );
            }

            SaveMetaData metaData = new SaveMetaData {
                safefileversion = SaveFileVersion,
                saveName = name,
                saveTime = DateTime.Now,
                saveFileType = GDH.saveFileType,
                playTime = GDH.playTime,
                difficulty = GDH.difficulty,
                isInDebugMode = DebugModeSave,
                usedMods = ModLoader.GetActiveMods()
            };

            string metadata = JsonConvert.SerializeObject(metaData, Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }
            );

            if(returnSaveInstead) {
                if (wasPaused == false) {
                    WC.IsPaused = false;
                }
                return new string[] { metadata, save };
            }
            File.WriteAllBytes(filePath + saveFileScreenShotEnding, GetSaveGameThumbnail());
            if (Application.isEditor) {
                File.WriteAllText(saveStatePath, save);
            }
            else {
                File.WriteAllBytes(saveStatePath, Zip(save));
            }
            File.WriteAllText(finalMetaStatePath, metadata);
            lastSaved = GDH.playTime;
            if (wasPaused == false) {
                WC.IsPaused = false;
            }
            return null;
        }

        public IEnumerator LoadGameState(string name = "autosave") {
            if (DoesEditorSaveExist(name) == false) {
                yield return null;
            }
            SaveName = name;
            //first pause the world so nothing changes and we can save an
            string finalSaveStatePath = Path.Combine(GetSaveGamesPath(), name + saveFileEnding);
            string finalMetaStatePath = Path.Combine(GetSaveGamesPath(), name + saveMetaFileEnding);
            SaveMetaData metaData = JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(finalMetaStatePath), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
            if (SaveFileVersion != metaData.safefileversion) {
                Debug.LogError("Mismatch of SaveFile Versions " + metaData.safefileversion + " & " + SaveFileVersion);
                yield break;
            }
            List<Mod> mods = ModLoader.AvaibleMods();
            if (mods != null && metaData.usedMods != null) {
                foreach (string mod in metaData.usedMods) {
                    if (mods.Exists((x) => x.name == mod) == false) {
                        Debug.LogError("Missing Mod " + mod);
                        yield break;
                    }
                }
            }

            DebugModeSave = metaData.isInDebugMode;
            SaveState state = null;
            string save = "";
            try {
                save = Unzip(File.ReadAllBytes(finalSaveStatePath));
            }
            catch {
                save = File.ReadAllText(finalSaveStatePath);
            }
            if (DebugModeSave) {
                state = new SaveState();
                string[] lines = save.Split(new string[] { "##" + Environment.NewLine }, StringSplitOptions.None);
                int i = 0;
                foreach (System.Reflection.FieldInfo field in typeof(SaveState).GetFields()) {
                    field.SetValue(state, lines[i]);
                    i++;
                }
            }
            else {
                state = JsonConvert.DeserializeObject<SaveState>(save, new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                });
            }
            PrototypController.Instance.LoadFromXML();

            GDH.LoadGameData(BaseSaveData.Deserialize<GameDataSave>((string)state.gamedata));
            lastSaved = GDH.playTime;

            loadingPercantage += 0.3f;
            PlayerControllerSave pcs = BaseSaveData.Deserialize<PlayerControllerSave>(state.pcs);
            loadingPercantage += 0.05f;
            yield return null;
            WorldSaveState wss = BaseSaveData.Deserialize<WorldSaveState>(state.world);
            loadingPercantage += 0.15f;
            yield return null;
            GameEventSave ges = BaseSaveData.Deserialize<GameEventSave>(state.ges);
            loadingPercantage += 0.1f;
            yield return null;
            CameraSave cs = BaseSaveData.Deserialize<CameraSave>(state.camera);
            loadingPercantage += 0.025f;
            UIControllerSave uics = BaseSaveData.Deserialize<UIControllerSave>(state.ui);
            loadingPercantage += 0.025f;
            yield return null;
            while (MapGenerator.Instance.IsDone == false)
                yield return null;
            PlayerController.Instance.SetPlayerData(pcs);
            loadingPercantage += 0.05f;
            yield return null;
            WorldController.Instance.SetWorldData(wss);
            loadingPercantage += 0.15f;
            yield return null;
            EventController.Instance.SetGameEventData(ges);
            loadingPercantage += 0.1f;
            yield return null;
            CameraController.Instance.SetSaveCameraData(cs);
            if (string.IsNullOrEmpty(state.fw)) {
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
            }
            else {
                FogOfWarData = Encoding.ASCII.GetBytes(state.fw);
            }
            loadingPercantage += 0.025f;
            yield return null;
            UIController.SetSaveUIData(uics);
            loadingPercantage += 0.025f;
            yield return null;
            Debug.Log("LOAD ENDED");
            IsDone = true;
            yield return null;
        }

        public byte[] GetSaveGameThumbnail() {
            Camera currentCamera = Camera.main;
            float ratio = Screen.currentResolution.width / Screen.currentResolution.height;
            int widht = 190;
            int height = 90;
            if (ratio <= 1.34) {
                widht = 200;
                height = 150;
            }
            else
            if (ratio <= 1.61) {
                widht = 190;
                height = 100;
            }
            else
            if (ratio >= 1.77) {
                widht = 190;
                height = 90;
            }

            RenderTexture rt = new RenderTexture(widht, height, 24, RenderTextureFormat.ARGB32) {
                antiAliasing = 4
            };

            currentCamera.targetTexture = rt;

            currentCamera.Render();//

            //Create the blank texture container
            Texture2D thumb = new Texture2D(widht, height, TextureFormat.RGB24, false);

            //Assign rt as the main render texture, so everything is drawn at the higher resolution
            RenderTexture.active = rt;

            //Read the current render into the texture container, thumb
            thumb.ReadPixels(new Rect(0, 0, widht, height), 0, 0, false);

            byte[] bytes = thumb.EncodeToJPG(90);

            //--Clean up--
            RenderTexture.active = null;
            currentCamera.targetTexture = null;
            rt.DiscardContents();
            return bytes;
        }

        [Serializable]
        public class SaveState {
            public string gamedata;
            public string world;
            public string pcs;
            public string ges;
            public string camera;
            public string ui;
            public string fw;
        }

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
        }
    }

    [Serializable]
    public abstract class BaseSaveData {

        public string Serialize(bool PreserverReferences) {
            Formatting formatting = SaveController.DebugModeSave ? Formatting.Indented : Formatting.None;
            string save = JsonConvert.SerializeObject(this, formatting,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserverReferences ?
                                            PreserveReferencesHandling.Objects : PreserveReferencesHandling.None,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto,
                        DefaultValueHandling = DefaultValueHandling.Ignore
                    }
            );
            return save;
        }

        public static T Deserialize<T>(string save) {
            T state = JsonConvert.DeserializeObject<T>(save, new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            });
            return state;
        }
    }
}