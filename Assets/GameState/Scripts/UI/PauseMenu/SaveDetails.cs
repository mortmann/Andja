using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class SaveDetails : MonoBehaviour {

    public Text creationDate;
    public Text climate;
    public Text size;

    public void ShowDetails(FileInfo saveFile) {
        if (EditorController.IsEditor) {
            creationDate.text = saveFile.CreationTime.ToString("dd.MM.yyyy");
            climate.text = saveFile.Directory.Parent.Parent.Name;
            size.text = saveFile.Directory.Parent.Name;
        }
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
