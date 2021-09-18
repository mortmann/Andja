using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuInfo : MonoBehaviour {
    public enum InfoTypes { ModError, MissingModError, SaveFileError, NewVersion, }
    static Dictionary<InfoTypes, string> ShowInfos = new Dictionary<InfoTypes, string>();
    public List<OkDialogOptions> okDialogOptions;
    public Transform Titles;
    public Transform Body;
    public GameObject Dialog;
    public Button OKButton;

    void Start() {
        OKButton.onClick.AddListener(() => { Dialog.SetActive(false); });
        StartCoroutine(ShowAfterEachother());
    }
    private void Update() {
        if(Dialog.activeSelf == false && ShowInfos.Count > 0) {
            StartCoroutine(ShowAfterEachother());
        }
    }
    private IEnumerator ShowAfterEachother() {
        foreach (var item in ShowInfos.Keys.ToArray()) {
            Dialog.SetActive(true);
            ShowInfo(item, ShowInfos[item]);
            ShowInfos.Remove(item);
            yield return new WaitWhile(()=>Dialog.activeSelf);
        }
        yield break;
    }

    private void ShowInfo(InfoTypes item, string additionalInfo) {
        foreach (Transform t in Titles) {
            t.gameObject.SetActive(false);
        }
        foreach (Transform t in Body) {
            if(t != OKButton.transform)
                t.gameObject.SetActive(false);
        }
        OkDialogOptions option = okDialogOptions.Find(x => x.Type == item);
        option.SetActive(additionalInfo);
    }

    public static void AddInfo(InfoTypes type, string additionalInfo) {
        ShowInfos.Add(type, additionalInfo);
    }
    [Serializable]
    public class OkDialogOptions {
        public InfoTypes Type;
        public Text Title;
        public Text Body;

        internal void SetActive(string additionalInfo) {
            Title.gameObject.SetActive(true);
            Body.gameObject.SetActive(true);
            if(additionalInfo != null)
                Body.text += "\n" + additionalInfo;
        }
    }

}
