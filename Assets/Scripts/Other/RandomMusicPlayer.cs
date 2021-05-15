using Andja.Controller;
using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Andja {
    public enum MusicSpace { MainMenu, Loading, Editor }
    
    public class RandomMusicPlayer : MonoBehaviour {
        public MusicSpace Type;
        public MusicType MusicType;
        List<SoundMetaData> songs;
        int currentSong;
        AudioSourcePauseable audioSource;
        void Start() {
            songs = SoundController.LoadMusicFiles(Path.Combine(ConstantPathHolder.StreamingAssets, "Audio", "Music", Type.ToString()));
            foreach(SoundMetaData smd in ModLoader.LoadSoundMetaDatas()) {
                if(smd.musicType == MusicType)
                    songs.Add(smd);
            }
            audioSource = GetComponent<AudioSourcePauseable>();
        }

        private void Update() {
            if (songs.Count == 0)
                return;
            if (audioSource.isPlaying == false && Application.isFocused) {
                StartCoroutine(SoundController.StartFile(songs[currentSong], audioSource));
                currentSong = (currentSong + 1) % songs.Count;
            }
        }
    }

}