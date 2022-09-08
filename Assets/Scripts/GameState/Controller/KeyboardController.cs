using System;
using Andja.Editor;
using Andja.UI;
using Andja.UI.Menu;
using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Video;

namespace Andja.Controller {

    public class KeyboardController : MonoBehaviour {
        // Use this for initialization

        private VideoPlayer _videoPlayer;
        private enum CheatCode { GodMode }

        private readonly Cheat[] _codes = new Cheat[] {
            new Cheat (
                new KeyCode[]
                {
                    KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
                    KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow,
                    KeyCode.RightArrow, KeyCode.B, KeyCode.A
                },
                CheatCode.GodMode),
        };

        private const float CheatCodeMaxDelay = 1.5f;
        private float _currentCheatCodeInputDelay = 0;

        private void Start() {
            new InputHandler();
        }

        /// <summary>
        /// Checks for any Input Down and calls responding functions
        /// </summary>
        private void Update() {
            if (InputHandler.GetButtonDown(InputName.Screenshot)) {
                if(SaveController.Instance != null) {
                    ScreenCapture.CaptureScreenshot(
                        "screenshot_" + SaveController.SaveName + "_" 
                        + System.DateTime.Now.ToString("dd_MM_yyyy-hh_mm_ss_ff") + ".png");
                }
                ScreenCapture.CaptureScreenshot("screenshot_" + System.DateTime.Now.ToString("dd_MM_yyyy-hh_mm_ss_ff") + ".png");
            }
            UpdateCheatCodes();
            if (WorldController.Instance == null)
                return;
            if (PlayerController.Instance.GameOver)
                return;
            if (Input.GetKeyDown(KeyCode.Escape)) {
                UIController.Instance.Escape(BuildController.Instance.BuildState != BuildStateModes.None);
                MouseController.Instance.Escape();
                BuildController.Instance.Escape();
                ShortcutUI.Instance.StopDragAndDropBuild();
                EndVideoReached(null);
            }
            if (InputHandler.GetButtonDown(InputName.Console)) {
                UIController.Instance.ToggleConsole();
            }
            if (UIController.IsTextFieldFocused()) {
                return;
            }
            if (UIController.Instance.IsPauseMenuOpen()) {
                return;
            }
            if (InputHandler.GetButtonDown(InputName.BuildMenu)) {
                UIController.Instance.ShowBuildMenu();
            }
            if (InputHandler.GetButtonDown(InputName.TradeMenu)) {
                UIController.Instance.ToggleTradeMenu();
            }
            if (InputHandler.GetButtonDown(InputName.Offworld)) {
                UIController.Instance.ToggleOffWorldMenu();
            }
            if (InputHandler.GetButtonDown(InputName.TogglePause)) {
                WorldController.Instance.TogglePause();
            }
            if (InputHandler.GetButtonDown(InputName.Stop)) {
                MouseController.Instance.StopUnit();
            }
            if (InputHandler.GetButtonDown(InputName.UpgradeTool)) {
                MouseController.Instance.SetMouseState(MouseState.Upgrade);
            }
            if (InputHandler.GetButtonUp(InputName.UpgradeTool)) {
                if(MouseController.Instance.MouseState == MouseState.Upgrade)
                    MouseController.Instance.SetMouseState(MouseState.Idle);
            }
            if (InputHandler.GetButtonDown(InputName.DiplomacyMenu)) {
                UIController.Instance.ToggleDiplomacyMenu();
            }
            if (InputHandler.GetButtonDown(InputName.Rotate)) {
                BuildController.Instance.RotateBuildStructure();
            }
            if (InputHandler.GetButtonDown(InputName.CopyStructure)) {
                MouseController.Instance.SetCopyMode(true);
            }
            if (InputHandler.GetButtonUp(InputName.CopyStructure)) {
                MouseController.Instance.SetCopyMode(false);
            }
            int num = InputHandler.HotkeyDown() - 1;
            if(num >= 0) {
                if(InputHandler.GetButton(InputName.UnitGrouping)) {
                    if (MouseController.Instance.selectedUnitGroup != null) {
                        PlayerController.CurrentPlayer.unitGroups[num] = MouseController.Instance.selectedUnitGroup;
                    }
                    else if (MouseController.Instance.SelectedUnit != null) {
                        PlayerController.CurrentPlayer.unitGroups[num] = new List<Model.Unit> {
                            MouseController.Instance.SelectedUnit
                        };
                    }
                    else {
                        PlayerController.CurrentPlayer.unitGroups[num] = null;
                    }
                } 
                else if(InputHandler.GetButton(InputName.UnitGrouping)) {
                    if(PlayerController.CurrentPlayer.unitGroups[num] != null) {
                        MouseController.Instance.SelectUnitGroup(PlayerController.CurrentPlayer.unitGroups[num]);
                    }
                } else {
                    string id = ShortcutUI.Instance.ShortcutIds[num];
                    if(string.IsNullOrWhiteSpace(id) == false)
                        BuildController.Instance.StartStructureBuild(id);
                } 
            }
            if (Application.isEditor) {
                if (Input.GetKey(KeyCode.LeftShift)) {
                    if (EventSystem.current.IsPointerOverGameObject() == false) {
                        FindObjectOfType<ToolTip>().DebugTileInfo(MouseController.Instance.GetTileUnderneathMouse());
                    }
                }
                if (Input.GetKeyUp(KeyCode.LeftShift)) {
                    FindObjectOfType<ToolTip>().Unshow();
                }
            }
        }
        /// <summary>
        /// Some fun cheat codes will be updated here.
        /// They have to be pressed in time for them to count.
        /// </summary>
        private void UpdateCheatCodes() {
            if (Input.anyKeyDown) {
                foreach (Cheat item in _codes) {
                    if(item.Check())
                        _currentCheatCodeInputDelay = 0;
                    if (item.IsActivated()) {
                        item.Reset();
                        switch (item.Code) {
                            case CheatCode.GodMode:
                                GodMode();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
            }
            else {
                if (_currentCheatCodeInputDelay > CheatCodeMaxDelay) {
                    foreach (Cheat item in _codes) {
                        item.Reset();
                    }
                }
                else {
                    _currentCheatCodeInputDelay += Time.deltaTime;
                }
            }
        }
        /// <summary>
        /// Troll the player who thought to activate godmode.
        /// </summary>
        private void GodMode() {
            StartCoroutine(PlayVideo());
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

        private void EndVideoReached(VideoPlayer source) {
            if (_videoPlayer == null)
                return;
            Debug.Log("Deactivated GODMODE");
            UIController.Instance.ChangeAllUI(true);
            Destroy(_videoPlayer.gameObject);
            SoundController.Instance.PauseMusicPlayback(false);
        }

        private class Cheat {
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
        }
    }
}