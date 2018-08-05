﻿using UnityEngine;
using System.Collections;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;


public class SaveController : MonoBehaviour {
    public static SaveController Instance;
    public static bool IsLoadingSave = false;
    public bool IsDone = false;
    public float loadingPercantage = 0;
    private const string metaFileEnding = ".meta";
    private const string saveFileEnding = ".sav";
    public const string islandFileEnding = ".isl";
    public const string islandImageEnding = ".png";
    private static List<Worker> loadWorker;

    //TODO autosave here
    const string SaveFileVersion = "0.1.3";
    const string islandSaveFileVersion = "i_0.0.2";

    GameDataHolder GDH => GameDataHolder.Instance;
    WorldController WC => WorldController.Instance;
    EventController EC => EventController.Instance;
    CameraController CC => CameraController.Instance;
    PlayerController PC => PlayerController.Instance;

    void Awake () {
		if (Instance != null) {
			Debug.LogError ("There should never be two SaveController.");
		}
		Instance = this;
    }
	// Use this for initialization
	void Start () {
        if (GDH!=null && GDH.loadsavegame!=null && GDH.loadsavegame.Length > 0) {
            Debug.Log("LOADING SAVEGAME " + GDH.loadsavegame);
            IsLoadingSave = true;
            StartCoroutine(LoadGameState (GDH.loadsavegame));
			GDH.loadsavegame = null;
		} else {
            IsLoadingSave = false;
        }
	}

    public void Update(){
		//autosave every soandso 
		//maybe option to choose frequenzy
	}

	public void SaveGameState(string name = "autosave"){
		//first pause the world so nothing changes and we can save an 
		bool wasPaused = WC.IsPaused;
		if(wasPaused==false){
			WC.IsPaused = true;
		}
		string saveStatePath = System.IO.Path.Combine (GetSaveGamesPath (), name + saveFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + metaFileEnding);

        SaveState savestate = new SaveState {
            gamedata = GDH.GetSaveGameData(),
            pcs = (PC.GetSavePlayerData()),
            world = (WC.GetSaveWorldData()),
            ges = (EC.GetSaveGameEventData()),
            camera = (CC.GetSaveCamera())
        };

        string save = JsonConvert.SerializeObject(savestate, Formatting.Indented,
                new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                }
        );

        SaveMetaData metaData = new SaveMetaData {
                safefileversion = SaveFileVersion,
                saveName = name,
                saveTime = DateTime.Now,
                saveFileType = GDH.saveFileType,
                playTime = GDH.playTime,
                difficulty = GDH.difficulty
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
        } else {
            System.IO.File.WriteAllBytes(saveStatePath, Zip(save));
        }
        System.IO.File.WriteAllText(finalMetaStatePath, metadata);

        if (wasPaused == false){
			WC.IsPaused = false;
		}
	}

    public void SaveIslandState(string name = "autosave") {
        string islandStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + islandFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + metaFileEnding);

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
        string finalMetaStatePath = System.IO.Path.Combine(GetIslandSavePath(name), name + metaFileEnding);

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
        EditorController.Instance.LoadIsland( GetIslandSave(name) ); 
    }

    public static string GetIslandSavePath(string name = null) {
        string path = Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace("/Assets", ""), "islands");
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
        string[] filePaths = System.IO.Directory.GetFiles(GetIslandSavePath(), "*"+metaFileEnding);
        foreach (string file in filePaths) {
            SaveMetaData metaData = null;
            try {
                metaData = JsonConvert.DeserializeObject<SaveMetaData>(File.ReadAllText(file), new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto
                });
            } catch {
                continue;
            } 
            if(metaData.safefileversion != islandSaveFileVersion) {
                continue;
            }
            KeyValuePair<Climate, Size> key = new KeyValuePair<Climate, Size>(metaData.climate, metaData.size);
            if(islands.ContainsKey(key) == false) {
                islands.Add(key, new List<string>());
            }
            islands[key].Add(metaData.saveName);
        }
        return islands;
    }

    internal static void SaveIslandImage(string name, byte[] imagePNG) {
        string path = Path.Combine(GetIslandSavePath(name), name + islandImageEnding);
        File.WriteAllBytes(path, imagePNG);
    }

    public static SaveMetaData[] GetMetaFiles(bool editor) {
        string path = editor ? GetIslandSavePath() : GetSaveGamesPath();
        List<SaveMetaData> saveMetaDatas = new List<SaveMetaData>();
        foreach (string file in Directory.GetFiles(path, "*"+metaFileEnding)) {
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
    public static string GetSaveGamesPath(){
		//TODO FIXME change this to documentspath
		return System.IO.Path.Combine(ConstantPathHolder.ApplicationDataPath.Replace ("/Assets","") , "saves");
	}

    public IEnumerator LoadGameState(string name = "autosave") {
        //first pause the world so nothing changes and we can save an 		
        string finalSaveStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + saveFileEnding);
        string finalMetaStatePath = System.IO.Path.Combine(GetSaveGamesPath(), name + metaFileEnding);
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
        SaveState state = null;
        try {
            state = JsonConvert.DeserializeObject<SaveState>(Unzip(File.ReadAllBytes(finalSaveStatePath)), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });

        } catch {
            state = JsonConvert.DeserializeObject<SaveState>( File.ReadAllText(finalSaveStatePath), new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            });
        }
        
		PrototypController.Instance.LoadFromXML ();

		GDH.LoadGameData(state.gamedata); // gamedata
        while (MapGenerator.Instance.IsDone == false)
            yield return null;
        loadingPercantage += 0.2f;
        PlayerController.SetPlayerData(state.pcs); // player
        loadingPercantage += 0.2f;
        WorldController.SetWorldData(state.world); // world
        loadingPercantage += 0.2f;
        EventController.SetGameEventData(state.ges); // event
        loadingPercantage += 0.2f;
        CameraController.SetSaveCameraData(state.camera); // camera
        loadingPercantage += 0.2f;

        IsDone = true;
        yield return null;
	}
	[Serializable]
	public class SaveState {
		public GameData gamedata;
		public WorldSaveState world;
		public PlayerControllerSave pcs;
		public GameEventSave ges;
		public CameraSave camera;
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
    }

}

