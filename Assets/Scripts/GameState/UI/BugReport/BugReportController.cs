using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Andja.Controller;
using Andja.Utility;
using System;

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
                metaData = SaveController.Instance?.SaveGameState(SaveController.SaveName, true)[0];
                saveFile = SaveController.Instance?.SaveGameState(SaveController.SaveName, true)[1];
            }
            if (IncludeLogfile.isOn) {
                logs = ConsoleController.Instance?.GetLogs();
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
    }
}