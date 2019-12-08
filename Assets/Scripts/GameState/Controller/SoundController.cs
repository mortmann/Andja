using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;
using System.IO;
using System;

public enum AmbientType { Water, North, Middle, South, Wind }
public enum MusicType { Idle, War, Combat, Lose, Win, Disaster, Doom }
public enum SoundType { Music, SoundEffect, Ambient }

// TODO: 
//		-Make sounds for tiles on screen possible eg. beach, river.
//		-sounds for units
public class SoundController : MonoBehaviour {
    public static SoundController Instance;
    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource windAmbientSource;

    public AudioSource uiSource;
    public AudioSource soundEffectSource;
    public GameObject soundEffect2DGO;
    List<AudioSource> playedAudios;

    CameraController cameraController;
    StructureSpriteController ssc;
    WorkerSpriteController wsc;
    UnitSpriteController usc;

    public static string[] MusicLocation = new string[] { "Audio" , "Music" };
    public static string[] SoundEffectLocation = new string[] { "Audio","Game","SoundEffects" };
    public static string[] AmbientLocation = new string[] { "Audio", "Game", "Ambient"};


    AudioType standardAudioType = AudioType.WAV;
    public AudioMixer mixer;

    Dictionary<string, SoundMetaData> nameToMetaData;
    Dictionary<MusicType, List<string>> musictypeToName;
    Dictionary<AmbientType, List<string>> ambienttypeToName;

    public AmbientType currentAmbient;
    public MusicType currentMusicType = MusicType.Idle;

    // Use this for initialization
    void Start() {
        if (Instance != null) {
            Debug.LogError("Only 1 Instance should be created.");
            return;
        }
        Instance = this;

        cameraController = CameraController.Instance;
        BuildController.Instance.RegisterStructureCreated(OnBuild);
        BuildController.Instance.RegisterCityCreated(OnCityCreate);
        EventController.Instance.RegisterOnEvent(OnEventStart, OnEventEnd);
        playedAudios = new List<AudioSource>();
        nameToMetaData = new Dictionary<string, SoundMetaData>();
        windAmbientSource.loop = true;
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
        ambienttypeToName = new Dictionary<AmbientType, List<string>>();
        foreach (AmbientType v in Enum.GetValues(typeof(AmbientType))) {
            ambienttypeToName[v] = new List<string>();
        }
        LoadFiles();
        StartCoroutine(StartFile(nameToMetaData["wind-1"], windAmbientSource));

    }

    // Update is called once per frame
    void Update() {
        if (musicSource.isPlaying == false) {
            StartCoroutine(StartFile(nameToMetaData[GetMusicFileName()], musicSource));
        }
        if (WorldController.Instance.IsPaused) {
            return;
        }
        ambientSource.volume = Mathf.Clamp((CameraController.MaxZoomLevel - cameraController.zoomLevel) / CameraController.MaxZoomLevel, 0, 1f);
        windAmbientSource.volume = Mathf.Clamp(1 - ambientSource.volume - 0.7f, 0.03f, 0.15f);
        UpdateAmbient();
        UpdateSoundEffects();
        for (int i = playedAudios.Count - 1; i >= 0; i--) {
            if (playedAudios[i].isPlaying == false) {
                Destroy(playedAudios[i].gameObject);
                playedAudios.RemoveAt(i);
            }
        }

    }

    public void LoadFiles() {
        string musicPath = Path.Combine(ConstantPathHolder.StreamingAssets,Path.Combine(MusicLocation));
        string soundEffectPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(SoundEffectLocation));
        string ambientPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(AmbientLocation));

        string[] musicfiles = Directory.GetFiles(musicPath, "*.wav", SearchOption.AllDirectories);
        foreach(string path in musicfiles) {
            string dir = new DirectoryInfo(path).Parent.Name;
            string name = Path.GetFileNameWithoutExtension(path);
            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Music,
                musicType = (MusicType)Enum.Parse(typeof(MusicType), dir),
                fileExtension = standardAudioType,
                file = path
            };
            nameToMetaData[name] = meta;
            musictypeToName[meta.musicType].Add(name);
        }
        string[] soundeffectfiles = Directory.GetFiles(soundEffectPath,"*.wav", SearchOption.AllDirectories);
        foreach (string path in soundeffectfiles) {
            string name = Path.GetFileNameWithoutExtension(path);
            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.SoundEffect,
                fileExtension = standardAudioType,
                file = path
            };
            nameToMetaData[name] = meta;
        }
        string[] ambientfiles = Directory.GetFiles(ambientPath, "*.wav", SearchOption.AllDirectories);
        foreach (string path in ambientfiles) {
            string name = Path.GetFileNameWithoutExtension(path);
            string dir = new DirectoryInfo(path).Parent.Name;

            SoundMetaData meta = new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Ambient,
                ambientType = (AmbientType)Enum.Parse(typeof(AmbientType), dir),
                fileExtension = standardAudioType,
                file = path
            };
            nameToMetaData[name] = meta;
            ambienttypeToName[meta.ambientType].Add(name);
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
                    ambienttypeToName[meta.ambientType].Add(name);
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

    void UpdateSoundEffects() {
        //We add to any gameobject that has been created from
        //the StructurespriteController AND its in the list provided
        //by the CameraController an audiosource so that it 
        //can play a sound if it needs too. 
        foreach (Structure item in cameraController.structureCurrentInCameraView) {
            GameObject go = ssc.GetGameObject(item);
            if (go == null || go.GetComponent<AudioSource>() != null) {
                continue;
            }
            ADDCopiedAudioSource(go, soundEffectSource);
            item.RegisterOnSoundCallback(PlaySoundEffectStructure);
        }
        foreach (Worker item in wsc.workerToGO.Keys) {
            GameObject go = wsc.workerToGO[item];
            if (go == null || go.GetComponent<AudioSource>() != null) {
                continue;
            }
            ADDCopiedAudioSource(go, soundEffectSource);
            item.RegisterOnSoundCallback(PlaySoundEffectWorker);
        }
        foreach (Unit item in usc.unitGameObjectMap.Keys) {
            GameObject go = usc.unitGameObjectMap[item];
            if (go == null || go.GetComponent<AudioSource>() != null) {
                continue;
            }
            ADDCopiedAudioSource(go, soundEffectSource);
            item.RegisterOnSoundCallback(PlaySoundEffectUnit);
        }
    }
    private void ADDCopiedAudioSource(GameObject goal, AudioSource copied) {
        if (goal.GetComponent<AudioSource>() != null) {
            return;
        }
        AudioSource audio = goal.AddComponent<AudioSource>();
        // Copied fields can be restricted with BindingFlags
        System.Reflection.FieldInfo[] fields = audio.GetType().GetFields();
        foreach (System.Reflection.FieldInfo field in fields) {
            field.SetValue(audio, field.GetValue(copied));
        }
    }

    public void PlaySoundEffectStructure(Structure str, string fileName) {
        GameObject go = ssc.GetGameObject(str);
        if (go == null) {
            str.UnregisterOnSoundCallback(PlaySoundEffectStructure);
            return;
        }
        AudioSource goal = go.GetComponent<AudioSource>();
        if (goal == null) {
            str.UnregisterOnSoundCallback(PlaySoundEffectStructure);
            return;
        }
        StartCoroutine(StartFile(nameToMetaData[fileName], goal));
    }
    public void PlaySoundEffectWorker(Worker worker, string filePath) {
        GameObject go = wsc.workerToGO[worker];
        if (go == null) {
            worker.UnregisterOnSoundCallback(PlaySoundEffectWorker);
            return;
        }
        AudioSource goal = go.GetComponent<AudioSource>();
        if (goal == null) {
            worker.UnregisterOnSoundCallback(PlaySoundEffectWorker);
            return;
        }
        AudioClip ac = Resources.Load(SoundEffectLocation + filePath) as AudioClip;
        if (ac == null)
            return;
        ac.LoadAudioData();
        goal.PlayOneShot(ac);
    }
    public void PlaySoundEffectUnit(Unit unit, string filePath) {
        GameObject go = usc.unitGameObjectMap[unit];
        if (go == null) {
            unit.UnregisterOnSoundCallback(PlaySoundEffectUnit);
            return;
        }
        AudioSource goal = go.GetComponent<AudioSource>();
        if (goal == null) {
            unit.UnregisterOnSoundCallback(PlaySoundEffectUnit);
            return;
        }
        AudioClip ac = Resources.Load(SoundEffectLocation + filePath) as AudioClip;
        ac.LoadAudioData();
        goal.PlayOneShot(ac);
    }
    string GetMusicFileName() {
        //This function has to decide which Music is to play atm
        //this means if the player is a war or is in combat play
        //Diffrent musicclips then if he is building/at peace.
        //also when there is a disaster it should play smth diffrent
        //TODO CHANGE THIS-
        //for now it will choose a song random from the music folder
        //maybe add a User addable song loader into this
        //TODO: dont loop into the same sound overagain
        int count = musictypeToName[currentMusicType].Count;
        if (count == 0)
            return "";
        string soundFileName = musictypeToName[currentMusicType][UnityEngine.Random.Range(0, count)];


        return soundFileName;
    }

    public void OnBuild(Structure str, bool loading) {
        if (loading) {
            return;
        }
        if (str.PlayerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        string name = "BuildSound_" + Time.frameCount;
        if (GameObject.Find(name) != null) {
            return;
        }
        //Maybe make diffrent sound when diffrent buildingtyps are placed
        GameObject g = Instantiate(soundEffect2DGO);
        g.name = name;
        g.transform.SetParent(soundEffect2DGO.transform);
        AudioSource ac = g.GetComponent<AudioSource>();
        StartCoroutine(StartFile(nameToMetaData["build"], ac));
        playedAudios.Add(ac);
    }
    public void OnCityCreate(City c) {
        if (c.playerNumber != PlayerController.currentPlayerNumber) {
            return;
        }
        //diffrent sounds for diffrent locations of City? North,middle,South?
        GameObject g = Instantiate(soundEffect2DGO);
        g.transform.SetParent(soundEffect2DGO.transform);
        AudioSource ac = g.GetComponent<AudioSource>();
        StartCoroutine(StartFile(nameToMetaData["citybuild"], ac));
        playedAudios.Add(ac);
    }
    public void UpdateAmbient() {
        AmbientType ambient = AmbientType.Water;
        if (cameraController.nearestIsland != null) {
            switch (cameraController.nearestIsland.myClimate) {
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
        else {
            ambient = AmbientType.Water;
        }
        //If its the same no need to change && still playin
        if (ambient == currentAmbient && ambientSource.isPlaying) {
            return;
        }

        currentAmbient = ambient;
        if (ambienttypeToName[currentAmbient].Count == 0)
            return;
        //TODO: dont loop into the same sound overagain
        int count = ambienttypeToName[currentAmbient].Count;
        string soundFileName = ambienttypeToName[currentAmbient][UnityEngine.Random.Range(0,count)];
        
        if(nameToMetaData.ContainsKey(soundFileName))
            StartCoroutine(StartFile(nameToMetaData[soundFileName], ambientSource));
        //TODO make this like it should be :D, better make it sound nice 
    }

    public void PlayAudioClip(string name, AudioSource toPlay) {
        if(nameToMetaData.ContainsKey(name)) {
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

    IEnumerator<WWW> StartFile(SoundMetaData meta, AudioSource toPlay) {
        string musicFile = nameToMetaData[meta.name].file;
        if (File.Exists(musicFile) == false)
            yield return null;
        //System.Diagnostics.Stopwatch loadingStopWatch = new System.Diagnostics.Stopwatch();
        //loadingStopWatch.Start();
        string url = string.Format("file://{0}", musicFile);
        using (var www = new WWW(url)) {
            if(meta.fileExtension == AudioType.ACC || meta.fileExtension == AudioType.UNKNOWN) // not supported so it has to be unkown audiotype
                toPlay.clip = www.GetAudioClip(false,true);
            else // it is faster when knwon file to load it
                toPlay.clip = www.GetAudioClip(false, true, meta.fileExtension);

            if (toPlay.clip.loadState != AudioDataLoadState.Loaded)
                yield return www;
            if (!toPlay.isPlaying && toPlay.clip!=null&& toPlay.clip.loadState==AudioDataLoadState.Loaded)
                toPlay.Play();
            www.Dispose();
        }
        //Debug.Log("StartFile " +loadingStopWatch.Elapsed);
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