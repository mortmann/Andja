using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using Andja.Model;
using Andja.Controller;

namespace Andja.UI {
    public class ChoiceDialog : MonoBehaviour {
        public TMP_Text Titel;
        public TMP_Text Description;
        public Transform ChoicesParent;
        public Button ChoicePrefab;
        ChoiceInformation Current;
        public void Show(ChoiceInformation information) {
            Current = information;
            Titel.text = information.GetTitle();
            Description.text = information.GetDescription();
            foreach (Transform item in ChoicesParent) {
                Destroy(item.gameObject);
            }
            for (int i = 0; i < information.Choices.Length; i++) {
                Button choice = Instantiate(ChoicePrefab);
                string text = UILanguageController.Instance.GetTranslation(information.Choices[i].TextID);
                choice.GetComponentInChildren<TMP_Text>().text = text;
                choice.transform.SetParent(ChoicesParent, false);
                Choice currentchoice = Current.Choices[i];
                choice.onClick.AddListener(()=> { currentchoice.Action?.Invoke(); });
                choice.onClick.AddListener(()=> { Close(true); });
            }
            UILanguageController.Instance.RegisterLanguageChange(OnLanguageChange);
            PlayerController.Instance.cbPlayerChange += OnPlayerChange;
            gameObject.SetActive(true);
        }

        private void OnPlayerChange(Player arg1, Player arg2) {
            Close(false);
        }

        private void OnLanguageChange() {
            Show(Current);
        }

        private void Close(bool done) {
            if(done)
                Current.OnClose?.Invoke();
            Current = null;
            PlayerController.Instance.cbPlayerChange -= OnPlayerChange;
            UILanguageController.Instance.UnregisterLanguageChange(OnLanguageChange);
            gameObject.SetActive(false);
        }
    }
    public struct Choice {
        public object TextID;
        public Action Action;

        public Choice(object textID, Action action) {
            TextID = textID;
            Action = action;
        }
    }
}
