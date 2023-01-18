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
using System.Linq;
using UnityEngine;

namespace Andja.Controller {
    /// <summary>
    /// Handles Saving and Loading for Game and Editor.
    /// Creates Screenshot, Meta and Savefile.
    /// Saves as JSON.
    /// </summary>
    public class SaveController : MonoBehaviour {
        public static SaveController Instance;

        public string QuickSaveName = "quicksave";//TODO CHANGE THIS TO smth not hardcoded
        public static bool IsLoadingSave = false;
        public bool IsDone = false;

        public float loadingPercentage = 0;
        public const string SaveMetaFileEnding = ".meta";
        public const string SaveFileEnding = ".sav";
        public const string IslandMetaFileEnding = ".islmeta"; // TODO: thing of a better extension
        public const string IslandFileEnding = ".isl";
        public const string IslandImageEnding = ".png";
        public const string SaveFileScreenShotEnding = ".jpg";

        private static List<Worker> _loadWorker;
        public static bool DebugModeSave = true;
        public static string SaveName = "unsaved";

        public const string SaveFileVersion = "0.1.14";
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
                this.StartThrowingCoroutine(LoadGameState(GameData.Instance.LoadSaveGame), (ex) => {
                    UI.Menu.MainMenuInfo.AddInfo(UI.Menu.MainMenuInfo.InfoTypes.SaveFileError, ex.Message + ": " + ex.StackTrace);
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
            List<SaveMetaData> saveMetaDatas = SaveMetaData.GetMetaFiles(false).ToList();
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
            SaveGameState(QuickSaveName);
        }

        public void QuickLoad() {
            //TODO: NEEDS CHECK
            LoadWorld(true);
        }

        public void LoadWorld(bool quickload = false) {
            Debug.Log("LoadWorld button was clicked.");
            if (quickload) {
                GameData.loadSaveGameName = QuickSaveName;
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
            SaveName = savename;
            UI.Menu.MainMenu.SetLastPlayed(savename, true);
            string folderPath = GetIslandSavePath(savename);
            string islandStatePath = Path.Combine(folderPath, savename + IslandFileEnding);
            string finalMetaStatePath = Path.Combine(folderPath, savename + IslandMetaFileEnding);
            SaveMetaData metaData = SaveMetaData.CreateIslandData();

            SaveIsland savestate = EditorController.Instance.GetSaveState();
            if (Directory.Exists(folderPath) == false) {
                Directory.CreateDirectory(folderPath);
            }
            if (Application.isEditor) {
                File.WriteAllText(islandStatePath, savestate.Serialize());
            }
            else {
                File.WriteAllBytes(islandStatePath, FileUtil.Zip(savestate.Serialize()));
            }
            File.WriteAllText(finalMetaStatePath, metaData.Serialize());
        }

        public SaveIsland GetIslandSave(string name) {
            string saveStatePath = Path.Combine(GetIslandSavePath(name), name + IslandFileEnding);

            SaveMetaData metaData = SaveMetaData.GetFromFile(Path.Combine(GetIslandSavePath(name), name + IslandMetaFileEnding));
            if (IslandSaveFileVersion != metaData.safefileversion) {
                Debug.LogError("Mismatch of SaveFile Versions " + metaData.safefileversion + " & " + IslandSaveFileVersion);
                return null;
            }
            SaveIsland state = SaveIsland.Deserialize(saveStatePath);
            
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
            UI.Menu.MainMenu.SetLastPlayed(name, true);
            EditorController.Instance.LoadIsland(GetIslandSave(name));
        }

        public static string GetIslandSavePath(string name = null, bool userCustom = false) {
            string path = Path.Combine(ConstantPathHolder.StreamingAssets, "Islands");
            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);
            return name == null ? path : Path.Combine(path, name);
        }

        public static Dictionary<KeyValuePair<Climate, Size>, List<string>> GetIslands() {
            Dictionary<KeyValuePair<Climate, Size>, List<string>> islands = new Dictionary<KeyValuePair<Climate, Size>, List<string>>();
            string[] filePaths = Directory.GetFiles(GetIslandSavePath(), "*" + IslandMetaFileEnding, SearchOption.AllDirectories);
            foreach (string file in filePaths) {
                SaveMetaData metaData;
                try {
                    metaData = SaveMetaData.GetFromFile(file);
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

            string save;
            if (DebugModeSave || returnSaveInstead) {
                save = SaveState.CreateNew().SerializeDebugFormat();
            }
            else {
                save = SaveState.CreateNew().Serialize();
            }

            SaveMetaData metaData = SaveMetaData.CreateGameData();

            string metadataString = metaData.Serialize();

            if (returnSaveInstead) {
                if (wasPaused == false) {
                    WorldController.Instance.IsPaused = false;
                }
                return new string[] { metadataString, save };
            }
            File.WriteAllBytes(filePath + SaveFileScreenShotEnding, ScreenshotHelper.GetSaveGameThumbnail());
            if (Application.isEditor) {
                File.WriteAllText(saveStatePath, save);
            }
            else {
                File.WriteAllBytes(saveStatePath, FileUtil.Zip(save));
            }
            File.WriteAllText(finalMetaStatePath, metadataString);
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
            UI.Menu.MainMenu.SetLastPlayed(savename);
            SaveName = savename;
            string finalSaveStatePath = Path.Combine(GetSaveGamesPath(), savename + SaveFileEnding);
            string finalMetaStatePath = Path.Combine(GetSaveGamesPath(), savename + SaveMetaFileEnding);
            SaveMetaData metaData = SaveMetaData.GetFromFile(finalMetaStatePath);
            if (SaveFileVersion != metaData.safefileversion) {
                Debug.LogError("Mismatch of SaveFile Versions " + metaData.safefileversion + " & " + SaveFileVersion);
                yield break;
            }
            List<Mod> availableMods = ModLoader.AvaibleMods();
            if (availableMods != null && metaData.usedMods != null) {
                string missingMod = metaData.usedMods.Find((x) => availableMods.Exists(y => x == y.name) == false);
                if (missingMod != null) {
                    Debug.LogError("Missing Mod " + missingMod);
                    yield break;
                }
            }
            DebugModeSave = metaData.isInDebugMode;
            string save = GetTextFromSave(finalSaveStatePath);
            SaveState state = DebugModeSave ? SaveState.GetDebugSaveStateFromSave(save) : SaveState.GetSaveStateFromSave(save);
            GameData.Instance.LoadGameData(BaseSaveData.Deserialize<GameDataSave>(state.gamedata));
            _lastSaved = GameData.Instance.playTime;
            loadingPercentage += 0.05f; // 5
            LinkedSaves linkedSaveData = BaseSaveData.Deserialize<LinkedSaves>(state.linkedsave);
            PlayerControllerSave pcs = linkedSaveData.player;
            loadingPercentage += 0.175f; // 22.5
            yield return null;
            FogOfWarSave fws = null;
            if (string.IsNullOrEmpty(state.fw)) {
                GameData.FogOfWarStyle = FogOfWarStyle.Off;
            }
            else {
                fws = BaseSaveData.Deserialize<FogOfWarSave>(state.fw);
            }
            loadingPercentage += 0.05f; // 27.5
            yield return null;
            GameEventSave ges = linkedSaveData.events;
            loadingPercentage += 0.1f; // 37.5
            yield return null;
            CameraSave cs = BaseSaveData.Deserialize<CameraSave>(state.camera);
            loadingPercentage += 0.025f; // 40
            yield return null;
            while (MapGenerator.Instance.IsDone == false)
                yield return null;
            PlayerController.Instance.SetPlayerData(pcs);
            loadingPercentage += 0.05f; // 45
            yield return null;
            WorldController.Instance.SetWorldData(linkedSaveData.world);
            loadingPercentage += 0.45f; // 90
            yield return null;
            EventController.Instance.SetGameEventData(ges);
            loadingPercentage += 0.05f; // 95
            yield return null;
            CameraController.Instance.SetSaveCameraData(cs);
            FogOfWarController.SetSaveFogData(fws);
            loadingPercentage += 0.025f; // 97.5
            yield return null;
            UIController.SetSaveUIData(linkedSaveData.ui);
            loadingPercentage += 0.025f; // 100
            yield return null;
            Debug.Log("LOAD ENDED");
            IsDone = true;
            yield return null;
        }

        private static string GetTextFromSave(string finalSaveStatePath) {
            try {
                return FileUtil.Unzip(File.ReadAllBytes(finalSaveStatePath));
            }
            catch {
                return File.ReadAllText(finalSaveStatePath);
            }
        }

    }
}