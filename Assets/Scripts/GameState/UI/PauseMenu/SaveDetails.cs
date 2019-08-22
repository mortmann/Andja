using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SaveDetails : MonoBehaviour {

    public Text creationDate;
    public Text climate;
    public Text size;

    public void ShowDetails(SaveController.SaveMetaData saveFile) {
        if (saveFile == null)
            Debug.LogError("Given SaveFile was null");
        creationDate.text = saveFile.saveTime.ToString("dd.MM.yyyy");
        size.text = saveFile.size + "";
        if (EditorController.IsEditor) {
            climate.text = saveFile.climate + "";
        }
        else {
            climate.text = saveFile.saveFileType + "";
        }
    }

    // Update is called once per frame
    void Update() {

    }
}
