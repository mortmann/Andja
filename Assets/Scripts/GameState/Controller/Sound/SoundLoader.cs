using Andja.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Andja.Controller {
    public class SoundLoader {

        public static List<SoundMetaData> LoadMusicFiles(string musicPath) {
            List<SoundMetaData> files = new List<SoundMetaData>();
            string[] musicfiles = Directory.GetFiles(musicPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
            foreach (string path in musicfiles) {
                files.Add(SoundMetaData.CreateMusicFromPath(path));
            }
            return files;
        }
        public static void LoadFiles(
            Dictionary<string, SoundMetaData> _nameToMetaData, 
            Dictionary<MusicType, List<string>> _musicTypeToName, 
            Dictionary<AmbientType, List<string>>  _ambientTypeToName) {
            string musicPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(SoundController.MusicLocation));
            string soundEffectPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(SoundController.SoundEffectLocation));
            string ambientPath = Path.Combine(ConstantPathHolder.StreamingAssets, Path.Combine(SoundController.AmbientLocation));
            foreach (SoundMetaData smd in LoadMusicFiles(musicPath)) {
                _nameToMetaData[smd.name] = smd;
                _musicTypeToName[smd.musicType].Add(smd.name);
            }
            string[] soundeffectfiles = Directory.GetFiles(soundEffectPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
            foreach (string path in soundeffectfiles) {
                SoundMetaData soundEffectMeta = SoundMetaData.CreateSoundEffectFromPath(path);
                _nameToMetaData[soundEffectMeta.name] = soundEffectMeta;
            }
            string[] ambientfiles = Directory.GetFiles(ambientPath, "*.*", SearchOption.AllDirectories)
                .Where(s => s.ToLower().EndsWith(".ogg") || s.ToLower().EndsWith(".wav")).ToArray();
            foreach (string path in ambientfiles) {
                SoundMetaData ambientMeta = SoundMetaData.CreateAmbientFromPath(path);
                _nameToMetaData[ambientMeta.name] = ambientMeta;
                _ambientTypeToName[ambientMeta.ambientType].Add(ambientMeta.name);
            }

            foreach (SoundMetaData meta in ModLoader.LoadSoundMetaDatas()) {
                _nameToMetaData[meta.name] = meta;
                switch (meta.type) {
                    case SoundType.Music:
                        _musicTypeToName[meta.musicType].Add(meta.name);
                        break;

                    case SoundType.SoundEffect:
                        break;

                    case SoundType.Ambient:
                        _ambientTypeToName[meta.ambientType].Add(meta.name);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        public static IEnumerator StartFile(SoundMetaData meta, AudioSourcePauseable toPlay, bool deleteOnDone = false) {
            string musicFile = meta.file;
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
                if (www.result == UnityWebRequest.Result.ConnectionError) {
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
            if (deleteOnDone)
                SoundController.DeleteOnPlayedAudios.Add(toPlay);
            //Debug.Log("StartFile " + loadingStopWatch.Elapsed);
            yield return null;
        }
    }
}

