using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class SaveGameSelectableScript : MonoBehaviour {

    public Image SavegameImage;
    public Text TitleText;
    public Text TimeText;
    public Button DeleteButton;

    internal void Show(SaveController.SaveMetaData saveMetaData, Action<string, GameObject> clickedFunction, Action<string> deleteFunction) {
        if(EditorController.IsEditor==false)
            SavegameImage.sprite = SaveController.Instance.GetSaveFileScreenShot(saveMetaData.saveName);
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
