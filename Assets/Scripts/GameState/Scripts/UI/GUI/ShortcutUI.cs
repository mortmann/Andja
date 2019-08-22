using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShortcutUI : MonoBehaviour {

    Dictionary<int, GameObject> shortcuts;
    int shortCut;
    GameObject dragADropGO;
    List<GameObject> mygos;

    // Use this for initialization
    void Start() {
        shortcuts = new Dictionary<int, GameObject>();
        mygos = new List<GameObject>();
        int i = 1;
        foreach (Transform item in transform) {
            shortcuts.Add(i, item.gameObject);
            i++;
        }
        shortCut = -1;
    }
    public void SetDragAndDropBuild(GameObject go) {

        dragADropGO = Instantiate(go);
        dragADropGO.transform.SetParent(transform.parent);
        dragADropGO.GetComponent<StructureBuildUI>().Show(go.GetComponent<StructureBuildUI>().structure, false);
        Color c = dragADropGO.GetComponent<Image>().color;
        c.a = 0.3f;
        dragADropGO.GetComponent<Image>().color = c;
        dragADropGO.GetComponent<RectTransform>().sizeDelta = new Vector3(45, 45, 0);
        ShortCutMenuEmpties(true);

        if (mygos.Contains(go)) {
            mygos.Remove(go);
            Destroy(go);
            shortCut = -1;
        }

    }
    public void StopDragAndDropBuild() {
        if (shortCut != -1) {
            GameObject g = GameObject.Instantiate(dragADropGO);
            int i = shortCut;
            g.GetComponent<Button>().onClick.AddListener(() => { OnClick(i); });
            g.name = "ShortCut" + shortCut;

            g.GetComponent<StructureBuildUI>().Show(dragADropGO.GetComponent<StructureBuildUI>().structure, true);

            g.transform.SetParent(shortcuts[shortCut].transform);

            g.transform.localPosition = new Vector3(-20, -25);//cause setting it to zero fucks up

            Color c = g.GetComponent<Image>().color;
            c.a = 0.8f;
            g.GetComponent<Image>().color = c;
            shortCut = -1;
            mygos.Add(g);
        }
        GameObject.Destroy(dragADropGO);
        dragADropGO = null;
        ShortCutMenuEmpties(false);
    }
    public void ShortCutMenuEmpties(bool show) {

        foreach (Transform t in transform) {
            if (t.childCount == 1) {
                t.GetChild(0).gameObject.SetActive(show);
            }
        }
    }

    public void OnShortCutOver(int number) {
        shortCut = number;
    }
    void Update() {
        if (dragADropGO != null) {
            dragADropGO.transform.position = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0)) {
            StopDragAndDropBuild();
        }
    }
    public void OnClick(int number) {
        BuildController.Instance.OnClick(shortcuts[number].GetComponentInChildren<StructureBuildUI>().structure.ID);
    }
}
