using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Audio;
using System.IO;
using System;
using UnityEngine.Networking;
using System.Linq;

public enum AmbientType { Ocean, North, Middle, South, Wind }
public enum MusicType { Idle, War, Combat, Lose, Win, Disaster, Doom }
public enum SoundType { Music, SoundEffect, Ambient }

// TODO: 
//		-Make sounds for tiles on screen possible eg. beach, river.
//		-sounds for units
public class SoundController : MonoBehaviour {
    public static SoundController Instance;
    public AudioSource musicSource;
    public AudioSource oceanAmbientSource;
    public AudioSource landAmbientSource;
    public AudioSource windAmbientSource;

    public AudioSource uiSource;
    public AudioSource soundEffectSource;
    public AudioSource soundEffect2DGO;
    List<AudioSource> deleteOnPlayedAudios;

    CameraController cameraController;
    StructureSpriteController ssc;
    WorkerSpriteController wsc;
    UnitSpriteController usc;

    public static string[] MusicLocation = new string[] { "Audio" , "Music" };
    public static string[] SoundEffectLocation = new string[] { "Audio", "Game","SoundEffects" };
    public static string[] AmbientLocation = new string[] { "Audio", "Game", "Ambient"};

    public AudioMixer mixer;

    Dictionary<string, SoundMetaData> nameToMetaData;
    Dictionary<MusicType, List<string>> musictypeToName;
    Dictionary<AmbientType, List<string>> ambientTypeToName;
    Dictionary<object, AudioSource> objectToAudioSource;
    public AmbientType currentAmbient;
    public MusicType currentMusicType = MusicType.Idle;
    string lastPlayedMusicTrack;
    private void Awake() {
        if (Instance != null) {
            Debug.LogError("Only 1 Instance should be created.");
            return;
        }
        Instance = this;
    }
    // Use this for initialization
    void Start() {
        cameraController = CameraController.Instance;
        BuildController.Instance.RegisterStructureCreated(OnBuild);
        BuildController.Instance.RegisterCityCreated(OnCityCreate);
        PlayerController.Instance.RegisterPlayersDiplomacyStatusChange(OnDiplomacyChange);
        EventController.Instance.RegisterOnEvent(OnEventStart, OnEventEnd);
        deleteOnPlayedAudios = new List<AudioSource>();
        objectToAudioSource = new Dictionary<object, AudioSource>();
        nameToMetaData = new Dictionary<string, SoundMetaData>();
        ssc = FindObjectOfType<StructureSpriteController>();
        wsc = FindObjectOfType<WorkerSpriteController>();
        usc = FindObjectOfType<UnitSpriteController>();

        Dictionary<string, int> volumes = MenuAudioManager.StaticReadSoundVolumes();
        if (volumes != null) {
            foreach (VolumeType v in Enum.GetValues(typeof(VolumeType))) {
                if (volumes.ContainsKey(v.ToString())) {
                    mixer.SetFloat(v.ToString(), MenuAudioManager.ConvertToDecibel(volumes[v.ToString()]));
                }
            }
        }
        musictypeToName = new Dictionary<MusicType, List<string>>();
        foreach (MusicType v in Enum.GetValues(typeof(MusicType))) {
            musictypeToName[v] = new List<string>();
        }
        ambientTypeToName = new Dictionary<AmbientType, List<string>>();
        foreach (AmbientType v in Enum.GetValues(typeof(AmbientType))) {
            ambientTypeToName[v] = new List<string>();
        }
        WorldController.Instance.RegisterSpeedChange(OnGameSpeedChange);
        LoadFiles();
    }

    private void OnGameSpeedChange(GameSpeed gameSpeed, float speed) {
        foreach(AudioSource source in objectToAudioSource.Values) {
            source.pitch = speed;
        }
        mixer.SetFloat("SoundEffectPitchBend", 1f / speed);
    }

    private void OnDiplomacyChange(Player one, Player two, DiplomacyType oldType, DiplomacyType newType) {
        if(one.IsCurrent() == false && two.IsCurrent() == false) {
            return;
        }
        switch (newType) {
            case DiplomacyType.War:
                ChangeMusicType(MusicType.War);
                PlaySingle2DSoundEffect("war");
                break;
            case DiplomacyType.Neutral:
                if (one.IsCurrent() || two.IsCurrent()) {
                    if (PlayerController.Instance.IsAtWar(PlayerController.Instance.CurrPlayer)==false) {
                        ChangeMusicType(MusicType.Idle);
                    }
                }
                if (oldType == DiplomacyType.TradeAggrement) {
                    PlaySingle2DSoundEffect("papertear");
                } else {
                    PlaySingle2DSoundEffect("signing1");
                }
                break;
            case DiplomacyType.TradeAggrement:
                if (oldType == DiplomacyType.Alliance) {
                    PlaySingle2DSoundEffect("papertear");
                }
                else {
                    PlaySingle2DSoundEffect("signing1");
                }
                break;
            case DiplomacyType.Alliance:
                PlaySingle2DSoundEffect("signing2");
                break;
        }
    }

    private void ChangeMusicType(MusicType type) {
        if (type == currentMusicType)
            return;
        currentMusicType = type;
        StartCoroutine(StartFile(nameToMetaData[GetMusicFileName()], musicSource));
    }

    private void PlaySingle2DSoundEffect(string filename, string goname ="_2DSoundEffect") {
        if (filename == null)
            return;
        if (nameToMetaData.ContainsKey(filename) == false) {
            Debug.LogError("SoundEffect "+ filename +" File missing.");
            return;
        }
        AudioSource ac = Instantiate(soundEffect2DGO);
        ac.gameObject.name = filename + goname;
        ac.transform.SetParent(soundEffect2DGO.transform);
        StartCoroutine(StartFile(nameToMetaData[filename], ac, true));
    }

    // Update is called once per frame
    void Update() {
        UpdateMusic();
        if (WorldController.Instance.IsPaused) {
            return;
        }
        UpdateWindEffect();
        UpdateAmbient();
        for (int i = deleteOnPlayedAudios.Count - 1; i >= 0; i--) {
            if (deleteOnPlayedAudios[i].isPlaying == false) {
                Destroy(deleteOnPlayedAudios[i].gameObject);
                deleteOnPlayedAudios.RemoveAt(i);
            }
        }

    }

    private void UpdateMusic() {
        if (musicSource.isPlaying == false && Application.isFocused) {
            StartCoroutine(StartFile(nameToMetaData[GetMusicFileName()], musicSource));
        }
    }

    private void UpdateWindEffect() {
        windAmbientSource.volume = Mathf.Clamp(1 - CameraController.Instance.SoundAmbientValues.z - 0.7f, 0.03f, 0.15f);
        if (windAmbientSource.isPlaying == false) {
            //TODO: make this nicer
            int num = UnityEngine.Random.Range(0, ambientTypeToName[AmbientType.Wind].Count);
            StartCoroutine(StartFile(nameToMetaData[ambientTypeToName[AmbientType.Wind][num]], windAmbientSource));
        }
    }

    public void LoadFiles() {
        string musicPath = Path.Combine(ConstantPathHolder.StreamingAssets,Path.Combine(MusicLocation));
        string soundEffectPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(SoundEffectLocation));
        string ambientPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(AmbientLocation));

        string[] musicfiles = Directory.GetFiles(musicPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
        foreach (string path in musicfiles) {
            string dir = new DirectoryInfo(path).Parent.Name;
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Music,
                musicType = (MusicType)Enum.Parse(typeof(MusicType), dir),
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
            nameToMetaData[name] = meta;
            musictypeToName[meta.musicType].Add(name);
        }
        string[] soundeffectfiles = Directory.GetFiles(soundEffectPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
        foreach (string path in soundeffectfiles) {
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.SoundEffect,
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
            nameToMetaData[name] = meta;
        }
        string[] ambientfiles = Directory.GetFiles(ambientPath, "*.*", SearchOption.AllDirectories)
            .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
        foreach (string path in ambientfiles) {
            string name = Path.GetFileNameWithoutExtension(path);
            string dir = new DirectoryInfo(path).Parent.Name;
            string extension = Path.GetExtension(path);
            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Ambient,
                ambientType = (AmbientType)Enum.Parse(typeof(AmbientType), dir),
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
            nameToMetaData[name] = meta;
            ambientTypeToName[meta.ambientType].Add(name);
        }

        SoundMetaData[] custom = ModLoader.LoadSoundMetaDatas();
        foreach (SoundMetaData meta in custom) {
            nameToMetaData[meta.name] = meta;
            switch (meta.type) {
                case SoundType.Music:
                    musictypeToName[meta.musicType].Add(name);
                    break;
                case SoundType.SoundEffect:
                    break;
                case SoundType.Ambient:
                    ambientTypeToName[meta.ambientType].Add(name);
                    break;
            }
        }

    }

    internal void ChangeMusicPlayback(bool pause) {
        if (pause)
            musicSource.Pause();
        else
            musicSource.UnPause();
    }

    public void OnStructureGOCreated(Structure structure, GameObject go) {
        if (go == null) {
            return;
        }
        structure.RegisterOnSoundCallback(PlaySoundEffectStructure);
    }
    public void OnStructureGODestroyed(Structure structure, GameObject go) {
        if (go == null) {
            return;
        }
        structure.UnregisterOnSoundCallback(PlaySoundEffectStructure);
    }
    public void OnUnitCreated(Unit unit, GameObject go) {
        if (go == null) {
            return;
        }
        unit.RegisterOnSoundCallback(PlaySoundEffectUnit);
    }
    public void OnWorkerCreated(Worker worker, GameObject go) {
        if (go == null) {
            return;
        }
        worker.RegisterOnSoundCallback(PlaySoundEffectWorker);
    }

    private AudioSource ADDCopiedAudioSource(GameObject goal, AudioSource copied) {
        AudioSource a = goal.AddComponent(copied);
        objectToAudioSource[goal] = a;
        return a;
    }

    public void PlaySoundEffectStructure(Structure str, string fileName, bool play) {
        if (String.IsNullOrWhiteSpace(fileName))
            return;
        GameObject go = ssc.GetGameObject(str);
        if (go == null) {
            str.UnregisterOnSoundCallback(PlaySoundEffectStructure);
            return;
        }
        if (objectToAudioSource.ContainsKey(go)==false) {
            objectToAudioSource[go] = ADDCopiedAudioSource(go, soundEffectSource);
        }
        AudioSource goal = objectToAudioSource[go];
        if (play == false) {
            goal.Stop();
            return;
        }
        if (goal.isPlaying)
            return;
        StartCoroutine(StartFile(nameToMetaData[fileName], goal));
    }
    public void PlaySoundEffectWorker(Worker worker, string fileName, bool play) {
        if (String.IsNullOrWhiteSpace(fileName))
            return;
        GameObject go = wsc.workerToGO[worker];
        if (go == null) {
            worker.UnregisterOnSoundCallback(PlaySoundEffectWorker);
            return;
        }
        if (objectToAudioSource.ContainsKey(go) == false) {
            objectToAudioSource[go] = ADDCopiedAudioSource(go, soundEffectSource);
        }
        AudioSource goal = objectToAudioSource[go];
        if (play == false) {
            goal.Stop();
            return;
        }
        if (goal.isPlaying)
            return;
        StartCoroutine(StartFile(nameToMetaData[fileName], goal));
    }
    public void PlaySoundEffectUnit(Unit unit, string fileName, bool play) {
        if (String.IsNullOrWhiteSpace(fileName))
            return;
        GameObject go = usc.unitGameObjectMap[unit];
        if (go == null) {
            unit.UnregisterOnSoundCallback(PlaySoundEffectUnit);
            return;
        }
        if (objectToAudioSource.ContainsKey(go) == false) {
            objectToAudioSource[go] = ADDCopiedAudioSource(go, soundEffectSource);
        }
        AudioSource goal = objectToAudioSource[go];
        if (goal == null) {
            goal = ADDCopiedAudioSource(go, soundEffectSource);
        }
        if (goal.isPlaying)
            return;
        PlayAudioClip(fileName, goal);
    }
    string GetMusicFileName() {
        //This function has to decide which Music is to play atm
        //this means if the player is a war or is in combat play
        //Diffrent musicclips then if he is building/at peace.
        //also when there is a disaster it should play smth diffrent
        //TODO CHANGE THIS-
        int count = musictypeToName[currentMusicType].Count;
        if (count == 0)
            return "";
        string soundFileName;
        do {
            soundFileName = musictypeToName[currentMusicType][UnityEngine.Random.Range(0, count)];
        } while (soundFileName == lastPlayedMusicTrack && count > 1);
        lastPlayedMusicTrack = soundFileName;
        Debug.Log("PLAYING: " + soundFileName);
        return soundFileName;
    }

    public void OnBuild(Structure str, bool loading) {
        if (loading) {
            return;
        }
        if (str.PlayerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        string name = "_sound_" + Time.frameCount;
        if (GameObject.Find("build" + name) != null) {
            return;
        }
        PlaySingle2DSoundEffect("build", name);
    }
    public void OnCityCreate(City c) {
        if (c.PlayerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        //diffrent sounds for diffrent locations of City? North,middle,South?
        PlaySingle2DSoundEffect("citybuild");
    }
    public void UpdateAmbient() {
        landAmbientSource.volume = CameraController.Instance.SoundAmbientValues.y * CameraController.Instance.SoundAmbientValues.z;
        oceanAmbientSource.volume = CameraController.Instance.SoundAmbientValues.x * CameraController.Instance.SoundAmbientValues.z;
        AmbientType ambient = AmbientType.Ocean;
        if (cameraController.nearestIsland != null) {
            switch (cameraController.nearestIsland.Climate) {
                case Climate.Cold:
                    ambient = AmbientType.North;
                    break;
                case Climate.Middle:
                    ambient = AmbientType.Middle;
                    break;
                case Climate.Warm:
                    ambient = AmbientType.South;
                    break;
            }
        } 
        
        if(oceanAmbientSource.isPlaying == false) {
            int count = ambientTypeToName[AmbientType.Ocean].Count;
            string soundFileName = ambientTypeToName[AmbientType.Ocean][UnityEngine.Random.Range(0, count)];
            StartCoroutine(StartFile(nameToMetaData[soundFileName], oceanAmbientSource));
        }

        //If its the same no need to change && still playin
        if (ambient != currentAmbient || landAmbientSource.isPlaying == false) {        
            currentAmbient = ambient;
            if (ambientTypeToName[currentAmbient].Count == 0)
                return;
            int count = ambientTypeToName[currentAmbient].Count;
            string soundFileName = ambientTypeToName[currentAmbient][UnityEngine.Random.Range(0, count)];
            StartCoroutine(StartFile(nameToMetaData[soundFileName], landAmbientSource));
        }

        //TODO: dont loop into the same sound overagain

        //TODO make this like it should be :D, better make it sound nice 
    }

    public void PlayAudioClip(string name, AudioSource toPlay) {
        if (name == null)
            return;
        if (nameToMetaData.ContainsKey(name)) {
            StartCoroutine(StartFile(nameToMetaData[name], toPlay));
            return;
        }
        Debug.LogError("File missing! Wanted to play: " + name);
    }

    //TODO implement a way of playing these sounds
    public void OnEventStart(GameEvent ge) {
        //Debug.LogWarning("Not implemented yet!");
    }
    public void OnEventEnd(GameEvent ge) {
        //Maybe never used?
    }

    IEnumerator StartFile(SoundMetaData meta, AudioSource toPlay, bool deleteOnDone = false) {
        string musicFile = nameToMetaData[meta.name].file;
        if (File.Exists(musicFile) == false)
            yield return null;
        //System.Diagnostics.Stopwatch loadingStopWatch = new System.Diagnostics.Stopwatch();
        //loadingStopWatch.Start();
        //Using www is outdated so using unitywebrequest 
        string url = string.Format("file://{0}", musicFile);
        AudioType audioType = meta.fileExtension;
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, audioType)) {
            //not sure if it has any benefit at all -- but should not be negativ ...
            //hopefully atleast 
            ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
            yield return www.SendWebRequest();
            if (www.isNetworkError) {
                Debug.Log(www.error);
            }
            else {
                toPlay.clip = DownloadHandlerAudioClip.GetContent(www);
            }

            www.Dispose();
        }
        if (toPlay.clip.loadState != AudioDataLoadState.Loaded)
            yield return toPlay.clip.loadState;
        if (!toPlay.isPlaying && toPlay.clip != null && toPlay.clip.loadState == AudioDataLoadState.Loaded)
            toPlay.Play();
        if(deleteOnDone)
            deleteOnPlayedAudios.Add(toPlay);
        //Debug.Log("StartFile " + loadingStopWatch.Elapsed);
        yield return null;
    }

     
    void OnDestroy() {
        Instance = null;
    }
}
public class SoundMetaData {
    public string name;
    public string author;
    public SoundType type;
    public AmbientType ambientType;
    public MusicType musicType;
    public AudioType fileExtension;
    internal string file;
}