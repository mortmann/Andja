﻿using Andja.Controller;
using Andja.Editor;
using Andja.Utility;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.UI.Menu {

    public class SaveGameSelectableScript : MonoBehaviour {
        public Image SavegameImage;
        public Text TitleText;
        public Text TimeText;
        public Button DeleteButton;

        internal void Show(SaveMetaData saveMetaData, Action<string, GameObject> clickedFunction, Action<string> deleteFunction) {
            if (EditorController.IsEditor == false)
                SavegameImage.sprite = ScreenshotHelper.GetSaveFileScreenShot(saveMetaData.saveName);
            TitleText.text = saveMetaData.saveName;
            TimeText.text = saveMetaData.saveTime.ToString("G", System.Threading.Thread.CurrentThread.CurrentCulture);
            EventTrigger trigger = GetComponent<EventTrigger>();
            EventTrigger.Entry click = new EventTrigger.Entry {
                eventID = EventTriggerType.PointerClick
            };
            click.callback.AddListener((data) => { OnPointerClick(); clickedFunction(saveMetaData.saveName, this.gameObject); });
            trigger.triggers.Add(click);

            DeleteButton.onClick.AddListener(() => { deleteFunction?.Invoke(saveMetaData.saveName); });
        }

        public void OnPointerClick() {
            GetComponent<Image>().color = Color.red;
        }

        public void OnDeselectCall() {
            GetComponent<Image>().color = Color.white;
        }
    }
}