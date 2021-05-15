using Andja.Controller;
using System;
using UnityEngine;

namespace Andja.Utility {

    [RequireComponent(typeof(AudioSource))]
    public class AudioSourcePauseable : MonoBehaviour {
        public AudioSource audioSource;
        public bool isPaused { get; protected set; }
        public bool pausePlayBackOnGamePause = true;
        internal float volume {
            get {
                return audioSource.volume;
            }
            set {
                audioSource.volume = value;
            }
        }

        internal bool isPlaying => audioSource.isPlaying;

        internal AudioClip clip {
            get {
                return audioSource.clip;
            }
            set {
                audioSource.clip = value;
            }
        }

        private void Start() {
            audioSource = GetComponent<AudioSource>();
            WorldController.Instance?.RegisterSpeedChange(OnGameSpeedChange);
        }
        private void OnDestroy() {
            WorldController.Instance?.UnregisterSpeedChange(OnGameSpeedChange);
        }
        private void OnGameSpeedChange(GameSpeed gameSpeed, float value) {
            if (gameSpeed == GameSpeed.Paused && pausePlayBackOnGamePause) {
                Pause();
            } else 
            if (isPaused && pausePlayBackOnGamePause) {
                UnPause();
                SetPitch(value);
            }
        }

        public void Play() {
            audioSource.Play();
        }

        internal void SetPitch(float speed) {
            audioSource.pitch = speed;
        }

        internal void Stop() {
            audioSource.Stop();
        }

        internal void Pause() {
            isPaused = true;
            audioSource.Pause();
        }

        internal void UnPause() {
            isPaused = false;
            audioSource.UnPause();
        }
    }
}