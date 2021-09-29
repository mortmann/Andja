using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Andja.Controller;
using Andja.Utility;
using System;
using System.IO;
using Andja.UI.Menu;

namespace Andja {

    public class BugReportController : MonoBehaviour {
        enum IssuePriority { Minor, Normal, Major, Critical, GameBreaking }
        public TMP_InputField Title;
        public TMP_InputField Description;
        public TMP_InputField Autor;
        public Image Buffering;
        public ScreenshotImage[] Images;
        public Toggle IncludeSavefile;
        public Toggle IncludeLogfile;
        public TMP_Dropdown Label;
        public TMP_Dropdown Priority;
        public TMP_Text ErrorText;
        public Button SendReport;
        const string Prev_Log = "Player-prev.log";
        const string Curr_Log = "Player.log";

        void Start()  {
            GetComponentInParent<Canvas>().worldCamera = Camera.main;
            SendReport.onClick.AddListener(DoSendReport);
        }
        private void OnEnable() {
            WorldController.Instance?.Pause();
        }
        void DoSendReport() {
            if (string.IsNullOrEmpty(Title.text) || string.IsNullOrEmpty(Description.text))
                return;
            SendReport.interactable = false;
            ErrorText.gameObject.SetActive(false);
            Buffering.gameObject.SetActive(true);
            Texture2D[] texs = new Texture2D[Images.Length];
            for (int i = 0; i < Images.Length; i++) {
                texs[i] = Images[i].GetImage();
            }
            string logs = null;
            string saveFile = null;
            string metaData = null;
            if (IncludeSavefile.isOn) {
                if(Loading.IsLoading == false) {
                    if (MainMenu.IsMainMenu) {
                        if(MainMenu.Instance.LastIsEditorSave) {
                            metaData = SaveController.GetIslandMetaFile(MainMenu.Instance.LastPlayedSavefile);
                            saveFile = SaveController.GetIslandSaveFile(MainMenu.Instance.LastPlayedSavefile);
                        }
                        else {
                            metaData = SaveController.GetMetaDataFile(MainMenu.Instance.LastPlayedSavefile);
                            saveFile = SaveController.GetSaveFile(MainMenu.Instance.LastPlayedSavefile);
                        }
                    }
                    else
                    if (Editor.EditorController.IsEditor == false) {
                        string[] save = SaveController.Instance.SaveGameState(SaveController.SaveName, true);
                        if (save != null) {
                            metaData = save[0];
                            saveFile = save[1];
                        }
                    }
                } else {
                    if(Editor.EditorController.IsEditor && Editor.EditorController.Generate) {
                        metaData = "Editor Seed: " + Model.Generator.MapGenerator.Instance.MapSeed;
                    }
                    else {
                        metaData = SaveController.GetMetaDataFile();
                        saveFile = SaveController.GetSaveFile();
                    }
                }
            }
            if (IncludeLogfile.isOn) {
                logs = ConsoleController.Instance?.GetLogs();
                if (logs == null) {
                    if (MainMenu.JustOpenedGame) {
                        if (File.Exists(GetUnityLogFilePath(Prev_Log))) {
                            logs = Environment.NewLine + "Current Log:" + Environment.NewLine;
                            logs += File.ReadAllText(GetUnityLogFilePath(Prev_Log));
                        }
                        logs += Environment.NewLine + "Current Log:" + Environment.NewLine;
                    }
                    if (File.Exists(GetUnityLogFilePath(Curr_Log))) {
                        FileStream fs = File.Open(GetUnityLogFilePath(Curr_Log), 
                                                    FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        logs += new StreamReader(fs, System.Text.Encoding.Default).ReadToEnd();
                    }
                }
            }
            StartCoroutine(
                YouTrackHandler.SendReport(
                    Title.text,
                    Autor.text + "\n" + Description.text,
                    new string[] { Label.options[Label.value].text },
                    texs,
                    logs,
                    metaData,
                    saveFile,
                    ((IssuePriority)Priority.value).ToString(),
                    OnSuccess,
                    OnFailure
                )
           );
        }
        void OnSuccess() {
            Title.text = "";
            Description.text = "";
            for (int i = 0; i < Images.Length; i++) {
                Images[i].DeleteImage();
            }
            gameObject.SetActive(false);
            Buffering.gameObject.SetActive(false);
            SendReport.interactable = true;
            WorldController.Instance?.Unpause();
        }
        void OnFailure(string error) {
            ErrorText.gameObject.SetActive(true);
            ErrorText.text = error;
            Buffering.gameObject.SetActive(false);
            SendReport.interactable = true;
        }
        private void OnDisable() {
            WorldController.Instance?.Unpause();
        }

        private string GetUnityLogFilePath(string file) {
#if UNITY_STANDALONE_LINUX
             return Path.Combine("~/.config/unity3d", Application.companyName, Application.productName, 
                        file);
#endif
#if UNITY_STANDALONE_WIN
            return Path.Combine(Environment.GetEnvironmentVariable("AppData"), "..", "LocalLow",
                        Application.companyName, Application.productName, file);
#endif
#if UNITY_STANDALONE_OSX
             return Path.Combine("~/Library/Logs/Unity/", file);
#endif
        }
    }
}