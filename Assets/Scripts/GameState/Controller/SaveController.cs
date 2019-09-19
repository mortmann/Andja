using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using UnityEngine.SceneManagement;

public class SaveController : MonoBehaviour {
    public static SaveController Instance;

    string quickSaveName = "quicksave";//TODO CHANGE THIS TO smth not hardcoded
    public static bool IsLoadingSave = false;
    public bool IsDone = false;
    public float loadingPercantage = 0;
    private const string saveMetaFileEnding = ".meta";
    private const string saveFileEnding = ".sav";
    public const string islandMetaFileEnding = ".islmeta"; // TODO: thing of a better extension
    public const string islandFileEnding = ".isl";
    public const string islandImageEnding = ".png";
    private static List<Worker> loadWorker;
    public static bool DebugModeSave = true; 
    public static string SaveName = "unsaved";
    //TODO autosave here
    const string SaveFileVersion = "0.1.4";
    const string islandSaveFileVersion = "i_0.0.2";
    float timeToAutoSave = AutoSaveInterval;
    const float AutoSaveInterval = 15 * 60; // every 15 min -- TODO: add game option to change this
    GameDataHolder GDH => GameDataHolder.Instance;
    WorldController WC => WorldController.Instance;
    EventController EC => EventController.Instance;
    CameraController CC => CameraController.Instance;
    PlayerController PC => PlayerController.Instance;

    void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two SaveController.");
        }
        Instance = this;
    }

    // Use this for initialization
    void Start() {
        if (GDH != null && GDH.Loadsavegame != null && GDH.Loadsavegame.Length > 0) {
            Debug.Log("LOADING SAVEGAME " + GDH.Loadsavegame);
            IsLoadingSave = true;
            StartCoroutine(LoadGameState(GDH.Loadsavegame));
            GameDataHolder.setloadsavegame = null;
        }
        else {
            IsLoadingSave = false;
            GDH?.GenerateMap();//just generate new map
        }
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
            GameDataHolder.setloadsavegame = quickSaveName;
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
        SaveGameState();
    }


    public void SaveIslandState(string name = "autosave") {
        string islandStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + islandFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + saveMetaFileEnding);
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
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }
        );

        EditorController.SaveIsland savestate = EditorController.Instance.GetSaveState();
        string save = JsonConvert.SerializeObject(savestate,//Formatting.Indented,
            new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            }
        );

        if (Application.isEditor) {
            System.IO.File.WriteAllText(islandStatePath, save);
        }
        else {
            System.IO.File.WriteAllBytes(islandStatePath, Zip(save));
        }
        System.IO.File.WriteAllText(finalMetaStatePath, metadata);
    }


    public EditorController.SaveIsland GetIslandSave(string name) {
        string saveStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + islandFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + islandMetaFileEnding);

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
                TypeNameHandling = TypeNameHandling.Auto
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
        string[] filePaths = System.IO.Directory.GetFiles(GetIslandSavePath(), "*" + islandMetaFileEnding, SearchOption.AllDirectories);
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
            KeyValuePair<Climate, Size> key = new KeyValuePair<Climate, Size>(metaData.climate, metaData.size);
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

        List<SaveMetaData> saveMetaDatas = new List<SaveMetaData>();
        foreach (string file in Directory.GetFiles(path, "*" + metaEnding)) {
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
    void OnDestroy() {
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
        return System.IO.Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace("/Assets", ""), "saves");
    }
    public void SaveGameState(string name = "autosave") {
        SaveName = name;
        //first pause the world so nothing changes and we can save an 
        bool wasPaused = WC.IsPaused;
        if (wasPaused == false) {
            WC.IsPaused = true;
        }
        string saveStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + saveFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + saveMetaFileEnding);

        SaveState savestate = new SaveState {
            gamedata = GDH.GetSaveGameData().Serialize(),
            pcs = PC.GetSavePlayerData().Serialize(),
            world = WC.GetSaveWorldData().Serialize(),
            ges = EC.GetSaveGameEventData().Serialize(),
            camera = CC.GetSaveCamera().Serialize()
        };
        string save = "";
        if (DebugModeSave) {
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
            isInDebugMode = DebugModeSave
        };

        string metadata = JsonConvert.SerializeObject(metaData, Formatting.Indented,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }
        );
        if (Application.isEditor) {
            System.IO.File.WriteAllText(saveStatePath, save);
        }
        else {
            System.IO.File.WriteAllBytes(saveStatePath, Zip(save));
        }
        System.IO.File.WriteAllText(finalMetaStatePath, metadata);

        if (wasPaused == false) {
            WC.IsPaused = false;
        }
    }

    public IEnumerator LoadGameState(string name = "autosave") {
        SaveName = name;
        //first pause the world so nothing changes and we can save an 		
        string finalSaveStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + saveFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + saveMetaFileEnding);
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
                TypeNameHandling = TypeNameHandling.Auto

            });
        }

        PrototypController.Instance.LoadFromXML();

        GDH.LoadGameData(BaseSaveData.Deserialize<GameData>((string)state.gamedata));  
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
        loadingPercantage += 0.05f;
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
        loadingPercantage += 0.05f;
        yield return null;
        Debug.Log("LOAD ENDED");
        IsDone = true;
        yield return null;
    }

    [Serializable]
    public class SaveState {

        public string gamedata;
        public string world;
        public string pcs;
        public string ges;
        public string camera;
    }
    [Serializable]
    public class SaveMetaData {
        public string saveName;
        public string safefileversion;
        public DateTime saveTime;
        public Difficulty difficulty;
        public GameType saveFileType;
        public float playTime;
        public Size size;
        public Climate climate;
        public bool isInDebugMode;
    }

}

[Serializable]
public abstract class BaseSaveData {

    public string Serialize() {
        Formatting formatting = SaveController.DebugModeSave ? Formatting.Indented : Formatting.None;
        string save = JsonConvert.SerializeObject(this, formatting,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }
        );
        return save;
    }
    public static T Deserialize<T>(string save) {
        T state = JsonConvert.DeserializeObject<T>(save, new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        });
        return state;
    }
}