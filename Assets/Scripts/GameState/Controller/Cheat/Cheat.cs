using Andja.Utility;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace Andja.Controller {
    public class Cheat {
        public enum CheatCode { GodMode }

        private readonly KeyCode[] _keyCodes;
        public readonly CheatCode Code;
        private bool _possible = true;
        private byte _keystroke;

        public Cheat(KeyCode[] keyCodes, CheatCode code) {
            _keyCodes = keyCodes;
            Code = code;
        }
        public bool IsActivated() {
            return _keyCodes.Length == _keystroke;
        }
        public bool Check() {
            if (_possible == false)
                return false;
            if(Input.GetKeyDown(_keyCodes[_keystroke]) == false) {
                _possible = false;
                return false;
            }
            _keystroke++;
            return true;
        }
        public void Reset() {
            _possible = true;
            _keystroke = 0;
        }

        internal void Do() {
            switch (Code) {
                case CheatCode.GodMode:
                    GodMode();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private VideoPlayer _videoPlayer;

        /// <summary>
        /// Troll the player who thought to activate godmode.
        /// </summary>
        private void GodMode() {
            SoundController.Instance.StartCoroutine(PlayVideo());
        }

        /// <summary>
        /// This will play when the game has Internet a never gonna give you up song.
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayVideo() {
            if (Application.internetReachability == NetworkReachability.NotReachable) {
                Debug.Log("You need Internet to be able to activate GODMODE");
                yield return null;
            }
            Debug.Log("ACTIVATING GODMODE");
            WorldController.Instance.ChangeGameSpeed(GameSpeed.Paused);
            if (_videoPlayer != null)
                yield return null;
            GameObject go = new GameObject();
            _videoPlayer = go.AddComponent<VideoPlayer>();
            go.layer = LayerMask.NameToLayer("UI");
            _videoPlayer.playOnAwake = false;
            AudioSource a = go.AddComponent(SoundController.Instance.musicSource.audioSource);
            a.clip = null;
            a.playOnAwake = false;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _videoPlayer.controlledAudioTrackCount = 1;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, a);
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.skipOnDrop = true;
            _videoPlayer.url = "https://ia800803.us.archive.org/29/items/MacArthur_Foundation_100andChange_dQw4w9WgXcQ/Rick_Astley_-_Never_Gonna_Give_You_Up_dQw4w9WgXcQ.mp4";
            //"https://rickrolled.fr/rickroll.mp4";
            //"https://ia801602.us.archive.org/11/items/Rick_Astley_Never_Gonna_Give_You_Up/Rick_Astley_Never_Gonna_Give_You_Up.mp4";

            _videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            _videoPlayer.targetCamera = Camera.main;
            _videoPlayer.waitForFirstFrame = true;

            _videoPlayer.Prepare();
            while (_videoPlayer.isPrepared == false) {
                yield return null;
            }
            SoundController.Instance.PauseMusicPlayback(true);
            UIController.Instance.ChangeAllUI(false);
            _videoPlayer.Play();
            a.Play();
            _videoPlayer.loopPointReached += EndVideoReached;
        }

        internal void Stop() {
            EndVideoReached(null);
        }

        private void EndVideoReached(VideoPlayer source) {
            if (_videoPlayer == null)
                return;
            Debug.Log("Deactivated GODMODE");
            UIController.Instance.ChangeAllUI(true);
            UnityEngine.Object.Destroy(_videoPlayer.gameObject);
            SoundController.Instance.PauseMusicPlayback(false);
        }
    }
}