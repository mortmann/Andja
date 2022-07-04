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
        UIController UIC => UIController.Instance;

        MouseController MouseController => MouseController.Instance;
        BuildController BuildController => BuildController.Instance;

        private VideoPlayer videoPlayer;
        private enum CheatCode { GodMode }

        readonly Cheat[] codes = new Cheat[] {
            new Cheat (
                new KeyCode[]
                {
                    KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
                    KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow,
                    KeyCode.RightArrow, KeyCode.B, KeyCode.A
                },
                CheatCode.GodMode),
        };

        private static readonly float cheatCodeMaxDelay = 1.5f;
        private float currentCheatCodeInputDelay = 0;

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
                UIC.Escape(BuildController.BuildState != BuildStateModes.None);
                MouseController.Escape();
                BuildController.Escape();
                ShortcutUI.Instance.StopDragAndDropBuild();
                EndVideoReached(null);
            }
            if (InputHandler.GetButtonDown(InputName.Console)) {
                UIC.ToggleConsole();
            }
            if (UIController.IsTextFieldFocused()) {
                return;
            }
            if (UIC.IsPauseMenuOpen()) {
                return;
            }
            if (InputHandler.GetButtonDown(InputName.BuildMenu)) {
                UIC.ShowBuildMenu();
            }
            if (InputHandler.GetButtonDown(InputName.TradeMenu)) {
                UIC.ToggleTradeMenu();
            }
            if (InputHandler.GetButtonDown(InputName.Offworld)) {
                UIC.ToggleOffWorldMenu();
            }
            if (InputHandler.GetButtonDown(InputName.TogglePause)) {
                WorldController.Instance.TogglePause();
            }
            if (InputHandler.GetButtonDown(InputName.Stop)) {
                MouseController.StopUnit();
            }
            if (InputHandler.GetButtonDown(InputName.UpgradeTool)) {
                MouseController.SetMouseState(MouseState.Upgrade);
            }
            if (InputHandler.GetButtonUp(InputName.UpgradeTool)) {
                if(MouseController.MouseState == MouseState.Upgrade)
                    MouseController.SetMouseState(MouseState.Idle);
            }
            if (InputHandler.GetButtonDown(InputName.DiplomacyMenu)) {
                UIC.ToggleDiplomacyMenu();
            }
            if (InputHandler.GetButtonDown(InputName.Rotate)) {
                if (BuildController.toBuildStructure != null) {
                    BuildController.toBuildStructure.RotateStructure();
                }
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
                        FindObjectOfType<ToolTip>().DebugTileInfo(MouseController.GetTileUnderneathMouse());
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
                foreach (Cheat item in codes) {
                    if(item.Check())
                        currentCheatCodeInputDelay = 0;
                    if (item.IsActivated()) {
                        item.Reset();
                        switch (item.Code) {
                            case CheatCode.GodMode:
                                GodMode();
                                break;
                        }
                    }
                }
            }
            else {
                if (currentCheatCodeInputDelay > cheatCodeMaxDelay) {
                    foreach (Cheat item in codes) {
                        item.Reset();
                    }
                }
                else {
                    currentCheatCodeInputDelay += Time.deltaTime;
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
            if (videoPlayer != null)
                yield return null;
            GameObject go = new GameObject();
            videoPlayer = go.AddComponent<VideoPlayer>();
            go.layer = LayerMask.NameToLayer("UI");
            videoPlayer.playOnAwake = false;
            AudioSource a = go.AddComponent(SoundController.Instance.musicSource.audioSource);
            a.clip = null;
            a.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, a);
            videoPlayer.source = VideoSource.Url;
            videoPlayer.skipOnDrop = true;
            videoPlayer.url = "https://ia800803.us.archive.org/29/items/MacArthur_Foundation_100andChange_dQw4w9WgXcQ/Rick_Astley_-_Never_Gonna_Give_You_Up_dQw4w9WgXcQ.mp4";
            //"https://rickrolled.fr/rickroll.mp4";
                //"https://ia801602.us.archive.org/11/items/Rick_Astley_Never_Gonna_Give_You_Up/Rick_Astley_Never_Gonna_Give_You_Up.mp4";

            videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.waitForFirstFrame = true;

            videoPlayer.Prepare();            
            while (videoPlayer.isPrepared == false) {
                yield return null;
            }
            SoundController.Instance.PauseMusicPlayback(true);
            UIController.Instance.ChangeAllUI(false);
            videoPlayer.Play();
            a.Play();
            videoPlayer.loopPointReached += EndVideoReached;
        }

        private void EndVideoReached(VideoPlayer source) {
            if (videoPlayer == null)
                return;
            Debug.Log("Deactivated GODMODE");
            UIController.Instance.ChangeAllUI(true);
            Destroy(videoPlayer.gameObject);
            SoundController.Instance.PauseMusicPlayback(false);
        }
        class Cheat {
            public KeyCode[] KeyCodes;
            public CheatCode Code;
            bool possible = true;
            byte keystroke;

            public Cheat(KeyCode[] keyCodes, CheatCode code) {
                KeyCodes = keyCodes;
                Code = code;
            }
            public bool IsActivated() {
                return KeyCodes.Length == keystroke;
            }
            public bool Check() {
                if (possible == false)
                    return false;
                if(Input.GetKeyDown(KeyCodes[keystroke]) == false) {
                    possible = false;
                    return false;
                }
                keystroke++;
                return true;
            }
            public void Reset() {
                possible = true;
                keystroke = 0;
            }
        }
    }
}