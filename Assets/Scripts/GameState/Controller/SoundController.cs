﻿using Andja.Model;
using Andja.UI.Menu;
using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;

namespace Andja.Controller {

    public enum AmbientType { Ocean, North, Middle, South, Wind }

    public enum MusicType { Idle, War, Combat, Lose, Win, Disaster, Doom, MainMenu, Loading, Editor }

    public enum SoundType { Music, SoundEffect, Ambient }

    // TODO:
    //		-Make sounds for tiles on screen possible eg. beach, river.
    //		-sounds for units
    /// <summary>
    /// Handles all Soundeffects except UI-Sounds.
    /// Selects Music, Ambient and Wind. 
    /// Callbacks from Structures and Units are handled here.
    /// </summary>
    public class SoundController : MonoBehaviour {
        public static SoundController Instance;
        public AudioSourcePauseable musicSource;
        public AudioSourcePauseable oceanAmbientSource;
        public AudioSourcePauseable landAmbientSource;
        public AudioSourcePauseable windAmbientSource;

        public AudioSource uiSource;
        public AudioSource soundEffectSource;
        public AudioSourcePauseable soundEffect2DGO;
        public static List<AudioSourcePauseable> DeleteOnPlayedAudios;

        public static string[] MusicLocation = new string[] { "Audio", "Music" };
        public static string[] SoundEffectLocation = new string[] { "Audio", "Game", "SoundEffects" };
        public static string[] AmbientLocation = new string[] { "Audio", "Game", "Ambient" };

        public AudioMixer mixer;

        private Dictionary<string, SoundMetaData> _nameToMetaData;
        private Dictionary<MusicType, List<string>> _musicTypeToName;
        private Dictionary<AmbientType, List<string>> _ambientTypeToName;
        private Dictionary<object, AudioSourcePauseable> _objectToAudioSource;
        public AmbientType currentAmbient;
        public MusicType currentMusicType = MusicType.Idle;
        private string _lastPlayedMusicTrack;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("Only 1 Instance should be created.");
                return;
            }
            Instance = this;
        }

        // Use this for initialization
        public void Start() {
            BuildController.Instance.RegisterStructureCreated(OnBuild);
            BuildController.Instance.RegisterCityCreated(OnCityCreate);
            PlayerController.Instance.RegisterPlayersDiplomacyStatusChange(OnDiplomacyChange);
            EventController.Instance.RegisterOnEvent(OnEventStart, OnEventEnd);
            DeleteOnPlayedAudios = new List<AudioSourcePauseable>();
            _objectToAudioSource = new Dictionary<object, AudioSourcePauseable>();
            _nameToMetaData = new Dictionary<string, SoundMetaData>();

            Dictionary<string, int> volumes = MenuAudioManager.StaticReadSoundVolumes();
            if (volumes != null) {
                foreach (VolumeType v in Enum.GetValues(typeof(VolumeType))) {
                    if (volumes.ContainsKey(v.ToString())) {
                        mixer.SetFloat(v.ToString(), MenuAudioManager.ConvertToDecibel(volumes[v.ToString()]));
                    }
                }
            }
            _musicTypeToName = new Dictionary<MusicType, List<string>>();
            foreach (MusicType v in Enum.GetValues(typeof(MusicType))) {
                _musicTypeToName[v] = new List<string>();
            }
            _ambientTypeToName = new Dictionary<AmbientType, List<string>>();
            foreach (AmbientType v in Enum.GetValues(typeof(AmbientType))) {
                _ambientTypeToName[v] = new List<string>();
            }
            WorldController.Instance.RegisterSpeedChange(OnGameSpeedChange);
            SoundLoader.LoadFiles(_nameToMetaData, _musicTypeToName, _ambientTypeToName);
        }

        private void OnGameSpeedChange(GameSpeed gameSpeed, float speed) {
            mixer.SetFloat("SoundEffectPitchBend", 1f / speed);
        }
        public AudioSource GetCopyOfAudioSource(AudioSource audio) {
            return audio.GetCopyOf(audio);
        }
        public AudioSource GetCopyOfMusicAudioSource() {
            return musicSource.GetCopyOf(musicSource.audioSource);
        }
        private void OnDiplomacyChange(Player one, Player two, DiplomacyType oldType, DiplomacyType newType) {
            if (one.IsCurrent() == false && two.IsCurrent() == false) {
                return;
            }
            switch (newType) {
                case DiplomacyType.War:
                    ChangeMusicType(MusicType.War);
                    PlaySingle2DSoundEffect("war");
                    break;

                case DiplomacyType.Neutral:
                    if (one.IsCurrent() || two.IsCurrent()) {
                        if (PlayerController.Instance.IsAtWar(PlayerController.CurrentPlayer) == false) {
                            ChangeMusicType(MusicType.Idle);
                        }
                    }
                    if (oldType == DiplomacyType.TradeAgreement) {
                        PlaySingle2DSoundEffect("papertear");
                    }
                    else {
                        PlaySingle2DSoundEffect("signing1");
                    }
                    break;

                case DiplomacyType.TradeAgreement:
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(newType), newType, null);
            }
        }

        private void ChangeMusicType(MusicType type) {
            if (type == currentMusicType)
                return;
            currentMusicType = type;
            StartCoroutine(SoundLoader.StartFile(_nameToMetaData[GetMusicFileName()], musicSource));
        }

        private void PlaySingle2DSoundEffect(string filename, string goname = "_2DSoundEffect") {
            if (filename == null)
                return;
            if (_nameToMetaData.ContainsKey(filename) == false) {
                Debug.LogError("SoundEffect " + filename + " File missing.");
                return;
            }
            AudioSourcePauseable ac = Instantiate(soundEffect2DGO);
            ac.gameObject.name = filename + goname;
            ac.transform.SetParent(soundEffect2DGO.transform);
            StartCoroutine(SoundLoader.StartFile(_nameToMetaData[filename], ac, true));
        }

        // Update is called once per frame
        public void Update() {
            UpdateMusic();
            if (WorldController.Instance.IsPaused) {
                return;
            }
            UpdateWindEffect();
            UpdateAmbient();
            for (int i = DeleteOnPlayedAudios.Count - 1; i >= 0; i--) {
                if (DeleteOnPlayedAudios[i].isPlaying) continue;
                Destroy(DeleteOnPlayedAudios[i].gameObject);
                DeleteOnPlayedAudios.RemoveAt(i);
            }
        }

        private void UpdateMusic() {
            if (musicSource.isPaused == false && musicSource.isPlaying == false && Application.isFocused) {
                StartCoroutine(SoundLoader.StartFile(_nameToMetaData[GetMusicFileName()], musicSource));
            }
        }

        private void UpdateWindEffect() {
            windAmbientSource.volume = Mathf.Clamp(1 - CameraController.Instance.SoundAmbientValues.z - 0.7f, 0.03f, 0.15f);
            if (windAmbientSource.isPaused || windAmbientSource.isPlaying) return;
            //TODO: make this nicer
            int num = UnityEngine.Random.Range(0, _ambientTypeToName[AmbientType.Wind].Count);
            StartCoroutine(SoundLoader.StartFile(_nameToMetaData[_ambientTypeToName[AmbientType.Wind][num]], windAmbientSource));
        }

        internal void PauseMusicPlayback(bool pause) {
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

        private void AddCopiedAudioSource(GameObject goal, AudioSource copied) {
            goal.AddComponent(copied);
            _objectToAudioSource[goal] = goal.AddComponent<AudioSourcePauseable>();
            _objectToAudioSource[goal].audioSource = goal.GetComponent<AudioSource>();
        }

        public void PlaySoundEffectStructure(Structure str, string fileName, bool play) {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            GameObject go = StructureSpriteController.Instance.GetGameObject(str);
            if (go == null) {
                str.UnregisterOnSoundCallback(PlaySoundEffectStructure);
                return;
            }
            if (_objectToAudioSource.ContainsKey(go) == false) {
                AddCopiedAudioSource(go, soundEffectSource);
            }
            AudioSourcePauseable goal = _objectToAudioSource[go];
            if (play == false) {
                goal.Stop();
                return;
            }
            if (goal.isPlaying)
                return;
            StartCoroutine(SoundLoader.StartFile(_nameToMetaData[fileName], goal));
        }

        public void PlaySoundEffectWorker(Worker worker, string fileName, bool play) {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            GameObject go = WorkerSpriteController.Instance.WorkerToGO[worker];
            if (go == null) {
                worker.UnregisterOnSoundCallback(PlaySoundEffectWorker);
                return;
            }
            if (_objectToAudioSource.ContainsKey(go) == false) {
                AddCopiedAudioSource(go, soundEffectSource);
            }
            AudioSourcePauseable goal = _objectToAudioSource[go];
            if (play == false) {
                goal.Stop();
                return;
            }
            if (goal.isPlaying)
                return;
            StartCoroutine(SoundLoader.StartFile(_nameToMetaData[fileName], goal));
        }

        public void PlaySoundEffectUnit(Unit unit, string fileName, bool play) {
            if (string.IsNullOrWhiteSpace(fileName))
                return;
            GameObject go = UnitSpriteController.Instance.unitGameObjectMap[unit];
            if (go == null) {
                unit.UnregisterOnSoundCallback(PlaySoundEffectUnit);
                return;
            }
            if (_objectToAudioSource.ContainsKey(go) == false) {
                AddCopiedAudioSource(go, soundEffectSource);
            }
            AudioSourcePauseable goal = _objectToAudioSource[go];
            if (goal.isPlaying)
                return;
            PlayAudioClip(fileName, goal);
        }

        private string GetMusicFileName() {
            //This function has to decide which Music is to play atm
            //this means if the player is a war or is in combat play
            //Diffrent musicclips then if he is building/at peace.
            //also when there is a disaster it should play smth diffrent
            //TODO CHANGE THIS-
            int count = _musicTypeToName[currentMusicType].Count;
            if (count == 0)
                return "";
            string soundFileName;
            do {
                soundFileName = _musicTypeToName[currentMusicType][UnityEngine.Random.Range(0, count)];
            } while (soundFileName == _lastPlayedMusicTrack && count > 1);
            _lastPlayedMusicTrack = soundFileName;
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

        public void OnCityCreate(ICity c) {
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
            if (CameraController.Instance.nearestIsland != null) {
                ambient = CameraController.Instance.nearestIsland.Climate switch {
                    Climate.Cold => AmbientType.North,
                    Climate.Middle => AmbientType.Middle,
                    Climate.Warm => AmbientType.South,
                    _ => ambient
                };
            }

            if (oceanAmbientSource.isPaused == false && oceanAmbientSource.isPlaying == false) {
                int count = _ambientTypeToName[AmbientType.Ocean].Count;
                string soundFileName = _ambientTypeToName[AmbientType.Ocean][UnityEngine.Random.Range(0, count)];
                StartCoroutine(SoundLoader.StartFile(_nameToMetaData[soundFileName], oceanAmbientSource));
            }

            //If its the same no need to change && still playin
            if (ambient != currentAmbient || landAmbientSource.isPlaying == false && landAmbientSource.isPaused == false) {
                currentAmbient = ambient;
                if (_ambientTypeToName[currentAmbient].Count == 0)
                    return;
                int count = _ambientTypeToName[currentAmbient].Count;
                string soundFileName = _ambientTypeToName[currentAmbient][UnityEngine.Random.Range(0, count)];
                StartCoroutine(SoundLoader.StartFile(_nameToMetaData[soundFileName], landAmbientSource));
            }

            //TODO: dont loop into the same sound overagain

            //TODO make this like it should be :D, better make it sound nice
        }

        public void PlayAudioClip(string name, AudioSourcePauseable toPlay) {
            if (name == null)
                return;
            if (_nameToMetaData.ContainsKey(name)) {
                StartCoroutine(SoundLoader.StartFile(_nameToMetaData[name], toPlay));
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

        public void OnDestroy() {
            Instance = null;
        }
    }
}