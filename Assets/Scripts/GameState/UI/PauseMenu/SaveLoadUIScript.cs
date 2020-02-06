using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class SaveLoadUIScript : MonoBehaviour {
    public SaveDetails saveInfo;

    public GameObject listPrefab;
    public GameObject canvasGO;
    string selected;
    public InputField saveGameInput;
    GameObject selectedGO;
    Dictionary<string, SaveController.SaveMetaData> nameToFile;

    // Use this for initialization
    void OnEnable() {
        LoadSaveFiles();
    }

    private void LoadSaveFiles() {
        nameToFile = new Dictionary<string, SaveController.SaveMetaData>();
        foreach (Transform item in canvasGO.transform) {
            GameObject.Destroy(item.gameObject);
        }
        SaveController.SaveMetaData[] saveMetaDatas = SaveController.GetMetaFiles(EditorController.IsEditor);
        // Build file list by instantiating fileListItemPrefab
        for (int i = saveMetaDatas.Length - 1; i >= 0; i--) {
            SaveGameSelectableScript go = Instantiate(listPrefab).GetComponent<SaveGameSelectableScript>();

            go.Show(saveMetaDatas[i], OnSaveGameSelect, OnSaveGameDeleteClick);

            // Make sure this gameobject is a child of our list box
            go.transform.SetParent(canvasGO.transform);

            nameToFile.Add(saveMetaDatas[i].saveName, saveMetaDatas[i]);
        }
        if (saveGameInput != null) {
            saveGameInput.onValueChanged.AddListener((data) => OnInputChange());
        }
    }

    public void OnInputChange() {
        if (selectedGO != null)
            selectedGO.GetComponent<SaveGameSelectableScript>().OnDeselectCall();
        selectedGO = null;
    }
    public void OnSaveGameSelect(string fi, GameObject go) {
        if (go == selectedGO)
            return;
        selected = fi;
        if (saveGameInput != null && saveGameInput.IsActive()) {
            saveGameInput.text = fi;
        }
        if (saveInfo != null)
            saveInfo.ShowDetails(nameToFile[fi]);

        if (selectedGO != null)
            selectedGO.GetComponent<SaveGameSelectableScript>().OnDeselectCall();
        selectedGO = go;

    }
    public void OnLoadPressed() {
        if (selected == null) {
            return;
        }
        if(EditorController.IsEditor) {
            FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.UnsavedProgress, DoLoad, null);
            return;
        }
        if (SaveController.Instance?.UnsavedProgress == true) {
            FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.UnsavedProgress, DoLoad, null);
        }
        if(SaveController.Instance==null) {
            DoLoad();
        }
    }
    private void DoLoad() {
        if (EditorController.IsEditor == false) {
            GameLoad();
        }
        else {
            EditorLoad();
        }
    }
    public void OnSaveGameDeleteClick(string name) {
        FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.DeleteSave,()=> {
            SaveController.Instance.DeleteSaveGame(name);
            LoadSaveFiles();
        }, null);
    }

    public void OnSavePressed() {
        string name = "";
        if (selected != null && string.IsNullOrWhiteSpace(saveGameInput.text)) {
            name = selected; 
        }
        else {
            name = saveGameInput.text;
        }

        if (string.IsNullOrWhiteSpace(name))
            return;
        if (EditorController.IsEditor == false) {
            //if it file with name exists ask user if it supposed to be overwritten
            if (SaveController.Instance.DoesGameSaveExist(name)) {
                FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.OverwriteSave, () => {
                    GameSave(name);
                }, null);
                return;
            }
            //if it doesnt exist save it
            GameSave(name);
        }
        else {
            //if it file with name exists ask user if it supposed to be overwritten
            if (SaveController.DoesEditorSaveExist(name)) {
                FindObjectOfType<YesNoDialog>().Show(YesNoDialogTypes.OverwriteSave, () => {
                    EditorSave(name);
                }, null);
                return;
            }
            //if it doesnt exist save it
            EditorSave(name);
        }
    }

    private void GameSave(string name) {
        SaveController.Instance.SaveGameState(name);
        UIController.Instance.TogglePauseMenu();
    }
    private void EditorSave(string name) {
        SaveController.Instance.SaveIslandState(name);
        EditorUIController.Instance.TogglePauseMenu();
    }
    private void GameLoad() {
        MenuController.Instance.LoadSaveGame(selected);
    }
    private void EditorLoad() {
        SaveController.Instance.LoadIsland(nameToFile[selected].saveName);
    }
}
