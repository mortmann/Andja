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

        private Dictionary<KeyCode[], CheatCode> cheatCodes = new Dictionary<KeyCode[], CheatCode> {
            { new KeyCode[] 
                {
                    KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
                    KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow,
                    KeyCode.RightArrow, KeyCode.B, KeyCode.A  
                }, 
                CheatCode.GodMode
            },
        };

        private static readonly float cheatCodeMaxDelay = 1.5f;
        private float currentCheatCodeInputDelay = 0;
        private int currentCheatCodeIndex = 0;

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
            if (PlayerController.GameOver)
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
            if (InputHandler.GetButtonDown(InputName.DiplomacyMenu)) {
                UIC.ToggleDiplomacyMenu();
            }
            if (InputHandler.GetButtonDown(InputName.Rotate)) {
                if (BuildController.toBuildStructure != null) {
                    BuildController.toBuildStructure.RotateStructure();
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
                currentCheatCodeInputDelay = 0;
                bool correctKey = false;
                foreach (KeyCode[] code in cheatCodes.Keys) {
                    if (code.Length > currentCheatCodeIndex
                        && Input.GetKeyDown(code[currentCheatCodeIndex])) {
                        correctKey = true;
                        if (currentCheatCodeIndex == code.Length - 1) {
                            switch (cheatCodes[code]) {
                                case CheatCode.GodMode:
                                    GodMode();
                                    break;
                            }
                        }
                    }
                }
                if (correctKey) {
                    currentCheatCodeIndex++;
                }
                else {                
                    //Reset because no correct CheatCode entered
                    currentCheatCodeIndex = 0;
                }
            }
            else {
                if (currentCheatCodeInputDelay > cheatCodeMaxDelay) {
                    currentCheatCodeIndex = 0;
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
            GameObject go = new GameObject();
            if (videoPlayer != null)
                yield return null;
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
            while (!videoPlayer.isPrepared) {
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
    }
}