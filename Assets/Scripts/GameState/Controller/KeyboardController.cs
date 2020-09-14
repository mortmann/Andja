using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.Video;
using System;

public class KeyboardController : MonoBehaviour {

    // Use this for initialization
    UIController UIC => UIController.Instance;
    MouseController MouseController => MouseController.Instance;
    BuildController BuildController => BuildController.Instance;
    enum CheatCode { GodMode }
    Dictionary<KeyCode[], CheatCode> cheatCodes = new Dictionary<KeyCode[], CheatCode> {
        { new KeyCode[] {
            KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow,
            KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow,
            KeyCode.RightArrow, KeyCode.B, KeyCode.A  }, CheatCode.GodMode
        }
    };

    float cheatCodeMaxDelay = 1.5f;
    float currentCheatCodeInputDelay = 0;
    int currentCheatCodeIndex = 0;
    void Start() {
        new InputHandler();
    }
    // Update is called once per frame
    void Update() {
        if (EditorController.IsEditor)
            return;
        if (PlayerController.GameOver)
            return;
        if (Input.GetKeyDown(KeyCode.Escape)) {
            MouseController.Escape();
            BuildController.Escape();
            UIC.Escape(BuildController.BuildState != BuildStateModes.None);
            ShortcutUI.Instance.StopDragAndDropBuild();
            EndVideoReached(null);
        }
        if (InputHandler.GetButtonDown(InputName.Console)) {
            UIC.ToggleConsole();
        }
        if (UIController.IsTextFieldFocused()) {
            return;
        }
        UpdateCheatCodes();
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
        if (InputHandler.GetButtonDown(InputName.Screenshot)) {
            ScreenCapture.CaptureScreenshot("screenshot_" + System.DateTime.Now.ToString("dd_MM_yyyy-hh_mm_ss_ff") + ".png");
        }
        if (Application.isEditor) {
            if (Input.GetKey(KeyCode.LeftShift)) {
                if (EventSystem.current.IsPointerOverGameObject() == false) {
                    FindObjectOfType<HoverOverScript>().DebugTileInfo(MouseController.GetTileUnderneathMouse());
                }
            }
            if (Input.GetKeyUp(KeyCode.LeftShift)) {
                FindObjectOfType<HoverOverScript>().Unshow();
            }
        }

    }

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
            //Reset because no correct CheatCode entered
            if(correctKey) {
                currentCheatCodeIndex++;
            } 
            else {
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
    private void GodMode() {
        StartCoroutine(PlayVideo());
    }
    VideoPlayer videoPlayer;
    IEnumerator PlayVideo() {
        if (Application.internetReachability == NetworkReachability.NotReachable) {
            Debug.Log("You need Internet to be able to activate GODMODE");
            yield return null;
        }
        Debug.Log("ACTIVATING GODMODE");

        if (videoPlayer!=null)
            yield return null;
        videoPlayer = gameObject.AddComponent<VideoPlayer>();

        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = "https://ia801602.us.archive.org/11/items/Rick_Astley_Never_Gonna_Give_You_Up/Rick_Astley_Never_Gonna_Give_You_Up.mp4";
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        float musicVolume = MenuAudioManager.Instance.GetVolumeFor(VolumeType.Music) / 100; //returns int %
        videoPlayer.SetDirectAudioVolume(0, SoundController.Instance.musicSource.volume * musicVolume);
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = Camera.main;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) {
            yield return null;
        }
        SoundController.Instance.ChangeMusicPlayback(true);
        videoPlayer.Play();
        videoPlayer.skipOnDrop = true;
        videoPlayer.loopPointReached += EndVideoReached;
    }

    private void EndVideoReached(VideoPlayer source) {
        if (videoPlayer == null)
            return;
        Debug.Log("Deactivated GODMODE");
        Destroy(videoPlayer);
        SoundController.Instance.ChangeMusicPlayback(false);
    }
}
