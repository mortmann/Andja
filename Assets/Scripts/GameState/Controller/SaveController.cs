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
using UnityEngine;

namespace Andja.Controller {
    /// <summary>
    /// Handles Saving and Loading for Game and Editor.
    /// Creates Screenshot, Meta and Savefile.
    /// Saves as JSON.
    /// </summary>
    public class SaveController : MonoBehaviour {
        public static SaveController Instance;

        private string _quickSaveName = "quicksave";//TODO CHANGE THIS TO smth not hardcoded
        public static bool IsLoadingSave = false;
        public bool IsDone = false;

        public float loadingPercentage = 0;
        private const string SaveMetaFileEnding = ".meta";
        private const string SaveFileEnding = ".sav";
        public const string IslandMetaFileEnding = ".islmeta"; // TODO: thing of a better extension
        public const string IslandFileEnding = ".isl";
        public const string IslandImageEnding = ".png";
        public const string SaveFileScreenShotEnding = ".jpg";

        private static List<Worker> _loadWorker;
        public static bool DebugModeSave = true;
        public static string SaveName = "unsaved";

        public const string SaveFileVersion = "0.1.13";
        public const string IslandSaveFileVersion = "i_0.0.3";
        private float _timeToAutoSave = AutoSaveInterval;
        private const float AutoSaveInterval = 15 * 60; // every 15 min -- TODO: add game option to change this

        /// <summary>
        /// Returns currently metadata being loaded or last tried to be loaded. 
        /// </summary>
        /// <returns></returns>
        public static string GetMetaDataFile(string file = null) {
            string path = EditorController.IsEditor ? GetIslandSavePath() : GetSaveGamesPath();
            string finalMetaStatePath = Path.Combine(path, (file ?? GameData.Instance.LoadSaveGame) + SaveMetaFileEnding);
            return File.Exists(finalMetaStatePath) == false ? null : File.ReadAllText(finalMetaStatePath);
        }
        /// <summary>
        /// Returns currently savegame being loaded or last tried to be loaded. 
        /// </summary>
        /// <returns></returns>
        public static string GetSaveFile(string file = null) {
            string path = EditorController.IsEditor ? GetIslandSavePath() : GetSaveGamesPath();
            string finalSaveStatePath = Path.Combine(path, (file ?? GameData.Instance.LoadSaveGame) + SaveFileEnding);
            return File.Exists(finalSaveStatePath) == false ? null : File.ReadAllText(finalSaveStatePath);
        }

        private float _lastSaved = -1;

        public bool UnsavedProgress => Math.Abs(_lastSaved - GameData.Instance.playTime) > 0.1;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two SaveController.");
            }
            Instance = this;
            _lastSaved = -1;
        }

        public void Start() {
            if (GameData.Instance != null && GameData.Instance.LoadSaveGame != null && GameData.Instance.LoadSaveGame.Length > 0) {
                Debug.Log("LOADING SAVEGAME " + GameData.Instance.LoadSaveGame);
                IsLoadingSave = true;
                this.StartThrowingCoroutine(LoadGameState(GameData.Instance.LoadSaveGame),(ex)=> {
                    Andja.UI.Menu.MainMenuInfo.AddInfo(
                    Andja.UI.Menu.MainMenuInfo.InfoTypes.SaveFileError, ex.Message+": " +ex.StackTrace);
                    SceneUtil.ChangeToMainMenuScreen(true);
                });
                GameData.loadSaveGameName = null;
            }
            else {
                IsLoadingSave = false;
                if (EditorController.IsEditor == false)
                    GameData.Instance?.GenerateMap();//just generate new map
            }
        }

        public static string GetLastSaveName() {
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

        public void QuickSave() {
            SaveGameState(_quickSaveName);
        }

        public void QuickLoad() {
            //TODO: NEEDS CHECK
            LoadWorld(true);
        }

        public void LoadWorld(bool quickload = false) {
            Debug.Log("LoadWorld button was clicked.");
            if (quickload) {
                GameData.loadSaveGameName = _quickSaveName;
            }
            // set to loadscreen to reset all data (and purge old references)
            SceneUtil.ChangeToGameStateLoadScreen(true, true);
        }

        public void Update() {
            if (_timeToAutoSave > 0) {
                _timeToAutoSave -= Time.deltaTime;
                return;
            }
            _timeToAutoSave = AutoSaveInterval;
            if (EditorController.IsEditor == false)
                SaveGameState();
            else
                SaveIslandState();
        }

        public bool DoesGameSaveExist(string savename) {
            return File.Exists(Path.Combine(GetSaveGamesPath(), savename + SaveFileEnding));
        }

        public static bool DoesEditorSaveExist(string name) {
            return File.Exists(Path.Combine(GetIslandSavePath(), name + IslandFileEnding));
        }

        public void DeleteSaveGame(string savename) {
            string filePath = Path.Combine(GetSaveGamesPath(), savename);
            string saveStatePath = filePath + SaveFileEnding;
            string finalMetaStatePath = filePath + SaveMetaFileEnding;
            string fileImagePath = filePath + SaveFileScreenShotEnding;
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

        public void SaveIslandState(string savename = "autosave") {
            Andja.UI.Menu.MainMenu.SetLastPlayed(savename, true);
            string folderPath = GetIslandSavePath(savename);
            string islandStatePath = Path.Combine(folderPath, savename + IslandFileEnding);
            string finalMetaStatePath = Path.Combine(folderPath, savename + IslandMetaFileEnding);
            SaveName = savename;
            SaveMetaData metaData = new SaveMetaData {
                safefileversion = IslandSaveFileVersion,
                saveName = savename,
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
            string saveStatePath = Path.Combine(GetIslandSavePath(name), name + IslandFileEnding);

            SaveMetaData metaData = JsonConvert.DeserializeObject<SaveMetaData>(GetIslandMetaFile(name), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
            if (IslandSaveFileVersion != metaData.safefileversion) {
                Debug.LogError("Mismatch of SaveFile Versions " + metaData.safefileversion + " & " + IslandSaveFileVersion);
                return null;
            }
            EditorController.SaveIsland state = null;
            try {
                state = JsonConvert.DeserializeObject<EditorController.SaveIsland>(
                    Unzip(File.ReadAllBytes(saveStatePath)), 
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }
                );
            }
            catch {
                state = JsonConvert.DeserializeObject<EditorController.SaveIsland>(GetIslandSaveFile(saveStatePath),
                    new JsonSerializerSettings {
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
        public static string GetIslandSaveFile(string name) {
            string saveStatePath = Path.Combine(GetIslandSavePath(name), name + IslandFileEnding);
            return File.Exists(saveStatePath) ? File.ReadAllText(saveStatePath) : null;
        }
        public static string GetIslandMetaFile(string name) {
            string finalMetaStatePath = Path.Combine(GetIslandSavePath(name), name + IslandMetaFileEnding);
            return File.Exists(finalMetaStatePath) ? File.ReadAllText(finalMetaStatePath) : null;
        }
        public void LoadIsland(string name) {
            Andja.UI.Menu.MainMenu.SetLastPlayed(name, true);
            EditorController.Instance.LoadIsland(GetIslandSave(name));
        }

        public static string GetIslandSavePath(string name = null, bool userCustom = false) {
            string path = Path.Combine(ConstantPathHolder.StreamingAssets, "Islands");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
            return name == null ? path : Path.Combine(path, name);
        }

        public static byte[] Zip(string str) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                CopyTo(msi, gs);
            }
            return mso.ToArray();
        }

        public static Sprite GetSaveFileScreenShot(string saveName) {
            string filePath = Path.Combine(GetSaveGamesPath(), saveName + SaveFileScreenShotEnding);
            if (File.Exists(filePath) == false) {
                Debug.Log("Missing Thumbnail for savegame " + filePath);
                return null;
            }
            var fileData = File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
        }

        public static bool IslandImageExists(string name) {
            return File.Exists(Path.Combine(GetIslandSavePath(name), name + IslandImageEnding));
        }

        public static Texture2D LoadIslandImage(string name, string nameAddOn = "") {
            if (IslandImageExists(name) == false) {
                return null;
            }
            string filePath = Path.Combine(GetIslandSavePath(name), name + nameAddOn + IslandImageEnding);
            if (File.Exists(filePath) == false)
                return null;
            var fileData = File.ReadAllBytes(filePath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            return tex;
        }

        public static string Unzip(byte[] bytes) {
            using var msi = new MemoryStream(bytes);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress)) {
                CopyTo(gs, mso);
            }
            return System.Text.Encoding.UTF8.GetString(mso.ToArray());
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
            string[] filePaths = Directory.GetFiles(GetIslandSavePath(), "*" + IslandMetaFileEnding, SearchOption.AllDirectories);
            foreach (string file in filePaths) {
                SaveMetaData metaData;
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
                if (metaData.safefileversion != IslandSaveFileVersion) {
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

        public static void SaveIslandImage(string name, byte[] imagePng, string addOn = "") {
            string path = Path.Combine(GetIslandSavePath(name), name + addOn + IslandImageEnding);
            File.WriteAllBytes(path, imagePng);
        }

        public static SaveMetaData[] GetMetaFiles(bool editor) {
            string path = editor ? GetIslandSavePath() : GetSaveGamesPath();
            string metaEnding = editor ? IslandMetaFileEnding : SaveMetaFileEnding;
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
                saveMetaDatas.RemoveAll(x => x.safefileversion != IslandSaveFileVersion);
            }
            else {
                saveMetaDatas.RemoveAll(x => x.safefileversion != SaveFileVersion);
            }
            return saveMetaDatas.ToArray();
        }

        public void OnDestroy() {
            Instance = null;
        }

        public static void AddWorkerForLoad(Worker w) {
            _loadWorker ??= new List<Worker>();
            _loadWorker.Add(w);
        }

        public static List<Worker> GetLoadWorker() {
            if (_loadWorker == null)
                return null;
            List<Worker> tempLoadWorker = new List<Worker>(_loadWorker);
            _loadWorker = null;
            return tempLoadWorker;
        }

        public static string GetSaveGamesPath() {
            //TODO FIXME change this to documentspath
            return Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace("/Assets", ""), "Saves");
        }

        public string[] SaveGameState(string savename = "autosave", bool returnSaveInstead = false) {
            SaveName = savename;
            Andja.UI.Menu.MainMenu.SetLastPlayed(savename);
            //first pause the world so nothing changes and we can save an
            bool wasPaused = WorldController.Instance.IsPaused;
            if (wasPaused == false) {
                WorldController.Instance.IsPaused = true;
            }
            string filePath = Path.Combine(GetSaveGamesPath(), savename);
            string saveStatePath = filePath + SaveFileEnding;
            string finalMetaStatePath = filePath + SaveMetaFileEnding;

            SaveState savestate = new SaveState {
                gamedata = GameData.Instance.GetSaveGameData().Serialize(false),
                linkedsave = new LinkedSaves(
                    PlayerController.Instance.GetSavePlayerData(),
                    WorldController.Instance.GetSaveWorldData(),
                    EventController.Instance.GetSaveGameEventData()
                    ).Serialize(true),
                camera = CameraController.Instance.GetSaveCamera().Serialize(false),
                ui = UIController.Instance.GetUISaveData().Serialize(false),
                fw = FogOfWarController.FogOfWarOn ? FogOfWarController.Instance.GetFogOfWarSave().Serialize(false) : null,
            };

            string save = "";
            if (DebugModeSave || returnSaveInstead) {
                foreach (System.Reflection.FieldInfo field in typeof(SaveState).GetFields()) {
                    string bsd = field.GetValue(savestate) as string;
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
                saveName = savename,
                saveTime = DateTime.Now,
                saveFileType = GameData.Instance.saveFileType,
                playTime = GameData.Instance.playTime,
                difficulty = GameData.Instance.difficulty,
                isInDebugMode = DebugModeSave,
                usedMods = ModLoader.GetActiveMods()
            };

            string metadata = JsonConvert.SerializeObject(metaData, Formatting.Indented,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = PreserveReferencesHandling.None,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        TypeNameHandling = TypeNameHandling.Auto
                    }
            );

            if(returnSaveInstead) {
                if (wasPaused == false) {
                    WorldController.Instance.IsPaused = false;
                }
                return new string[] { metadata, save };
            }
            File.WriteAllBytes(filePath + SaveFileScreenShotEnding, GetSaveGameThumbnail());
            if (Application.isEditor) {
                File.WriteAllText(saveStatePath, save);
            }
            else {
                File.WriteAllBytes(saveStatePath, Zip(save));
            }
            File.WriteAllText(finalMetaStatePath, metadata);
            _lastSaved = GameData.Instance.playTime;
            if (wasPaused == false) {
                WorldController.Instance.IsPaused = false;
            }
            return null;
        }

        public IEnumerator LoadGameState(string savename = "autosave") {
            if (DoesGameSaveExist(savename) == false) {
                yield return null;
            }
            Andja.UI.Menu.MainMenu.SetLastPlayed(savename);
            SaveName = savename;
            //first pause the world so nothing changes and we can save an
            string finalSaveStatePath = Path.Combine(GetSaveGamesPath(), savename + SaveFileEnding);
            string finalMetaStatePath = Path.Combine(GetSaveGamesPath(), savename + SaveMetaFileEnding);
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
            List<Mod> availableMods = ModLoader.AvaibleMods();
            if (availableMods != null && metaData.usedMods != null) {
                string missingMod = metaData.usedMods.Find((x) => availableMods.Exists(y => x == y.name));
                if (missingMod != null) {
                    Debug.LogError("Missing Mod " + missingMod);
                    yield break;
                }
            }

            DebugModeSave = metaData.isInDebugMode;
            SaveState state;
            string save;
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
            GameData.Instance.LoadGameData(BaseSaveData.Deserialize<GameDataSave>(state.gamedata));
            _lastSaved = GameData.Instance.playTime;
            loadingPercentage += 0.05f; // 5
            LinkedSaves linkedSaveData = BaseSaveData.Deserialize<LinkedSaves>(state.linkedsave);
            PlayerControllerSave pcs = linkedSaveData.player;
            loadingPercentage += 0.15f; // 15
            yield return null;
            WorldSaveState wss = linkedSaveData.world;
            loadingPercentage += 0.30f; // 50
            yield return null;
            FogOfWarSave fws = null;
            if (string.IsNullOrEmpty(state.fw)) {
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
            }
            else {
                fws = BaseSaveData.Deserialize<FogOfWarSave>(state.fw);
            }
            loadingPercentage += 0.05f; // 50
            yield return null;
            GameEventSave ges = linkedSaveData.events;
            loadingPercentage += 0.1f; // 65
            yield return null;
            CameraSave cs = BaseSaveData.Deserialize<CameraSave>(state.camera);
            loadingPercentage += 0.025f; // 67.5
            UIControllerSave uics = BaseSaveData.Deserialize<UIControllerSave>(state.ui);
            loadingPercentage += 0.025f; // 70
            yield return null;
            while (MapGenerator.Instance.IsDone == false)
                yield return null;
            PlayerController.Instance.SetPlayerData(pcs);
            loadingPercentage += 0.05f; // 75
            yield return null;
            WorldController.Instance.SetWorldData(wss);
            loadingPercentage += 0.15f; // 90
            yield return null;
            EventController.Instance.SetGameEventData(ges);
            loadingPercentage += 0.05f; // 95
            yield return null;
            CameraController.Instance.SetSaveCameraData(cs);
            FogOfWarController.SetSaveFogData(fws);
            loadingPercentage += 0.025f; // 97.5
            yield return null;
            UIController.SetSaveUIData(uics);
            loadingPercentage += 0.025f; // 100
            yield return null;
            Debug.Log("LOAD ENDED");
            IsDone = true;
            yield return null;
        }

        public byte[] GetSaveGameThumbnail() {
            Camera currentCamera = Camera.main;
            float ratio = Screen.currentResolution.width / (float)Screen.currentResolution.height;
            int width = 190;
            int height = 90;
            if (ratio <= 1.34) {
                width = 200;
                height = 150;
            }
            else
            if (ratio <= 1.61) {
                width = 190;
                height = 100;
            }
            else
            if (ratio >= 1.77) {
                width = 190;
                height = 90;
            }

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) {
                antiAliasing = 4
            };

            currentCamera.targetTexture = rt;

            currentCamera.Render();//

            //Create the blank texture container
            Texture2D thumb = new Texture2D(width, height, TextureFormat.RGB24, false);

            //Assign rt as the main render texture, so everything is drawn at the higher resolution
            RenderTexture.active = rt;

            //Read the current render into the texture container, thumb
            thumb.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);

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
            public string linkedsave;
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
    public class LinkedSaves : BaseSaveData {
        public PlayerControllerSave player;
        public WorldSaveState world;
        public GameEventSave events;

        public LinkedSaves() {
        }
        public LinkedSaves(PlayerControllerSave player, WorldSaveState world, GameEventSave events) {
            this.player = player;
            this.world = world;
            this.events = events;
        }
    }
    [Serializable]
    public abstract class BaseSaveData {

        public string Serialize(bool preserverReferences) {
            Formatting formatting = SaveController.DebugModeSave ? Formatting.Indented : Formatting.None;
            string save = JsonConvert.SerializeObject(this, formatting,
                    new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                        PreserveReferencesHandling = preserverReferences ?
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