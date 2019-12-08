using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
public enum YesNoDialogTypes { UnsavedProgress, DeleteSave, OverwriteSave }

public class YesNoDialog : MonoBehaviour {
    public GameObject dialog;
    public Text unsavedProgressText;
    public Text deleteSaveText;
    public Text overwriteSaveText;
    public Button yesButton;
    public Button noButton;

    public void Show(YesNoDialogTypes type, Action yesFunction, Action noFunction) {
        dialog.SetActive(true);
        unsavedProgressText.gameObject.SetActive(type==YesNoDialogTypes.UnsavedProgress);
        deleteSaveText.gameObject.SetActive(type == YesNoDialogTypes.DeleteSave);
        overwriteSaveText.gameObject.SetActive(type == YesNoDialogTypes.OverwriteSave);
        yesButton.onClick.AddListener(() => yesFunction?.Invoke());
        yesButton.onClick.AddListener(() => dialog.SetActive(false));
        noButton.onClick.AddListener(() => noFunction?.Invoke());
        noButton.onClick.AddListener(() => dialog.SetActive(false));
    }

}
