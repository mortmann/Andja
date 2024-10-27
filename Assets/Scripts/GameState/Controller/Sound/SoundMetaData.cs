using System;
using System.IO;
using UnityEngine;

namespace Andja.Controller {
    public class SoundMetaData {
        public string name;
        public string author;
        public SoundType type;
        public AmbientType ambientType;
        public MusicType musicType;
        public AudioType fileExtension;
        internal string file;
    
        public static SoundMetaData CreateMusicFromPath(string path) {
            string dir = new DirectoryInfo(path).Parent.Name;
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            return new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Music,
                musicType = (MusicType)Enum.Parse(typeof(MusicType), dir),
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
        }
        public static SoundMetaData CreateSoundEffectFromPath(string path) {
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);
            return new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.SoundEffect,
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
        }

        internal static SoundMetaData CreateAmbientFromPath(string path) {
            string name = Path.GetFileNameWithoutExtension(path);
            string dir = new DirectoryInfo(path).Parent.Name;
            string extension = Path.GetExtension(path);
            return new SoundMetaData() {
                name = name,
                author = "Andja",
                type = SoundType.Ambient,
                ambientType = (AmbientType)Enum.Parse(typeof(AmbientType), dir),
                fileExtension = extension.Contains("wav") ? AudioType.WAV : AudioType.OGGVORBIS,
                file = path
            };
        }
    }
}