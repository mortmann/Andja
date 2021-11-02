using Andja.Controller;
using Andja.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ShortcutUI : MonoBehaviour {
        static readonly int ShortcutCount = 8;
        public static ShortcutUI Instance { get; protected set; }
        public bool IsDragging;
        private Vector3 mouseOffset;
        private GameObject dragADropGO;
        private GameObject shortCutDraggedParent;

        private List<GameObject> shortcutsGO;
        private Dictionary<GameObject, GameObject> shortcutParentToButton;
        public Button buildButtonPrefab;

        public string[] ShortcutIds;

        private void Awake() { //has to be before uicontroller so it can be loaded
            if (Instance != null) {
                Debug.LogError("There should never be two StructureBuildUI.");
            }
            Instance = this;
            shortcutParentToButton = new Dictionary<GameObject, GameObject>();
            ShortcutIds = new string[ShortcutCount];
            for (int i = 0; i < ShortcutCount; i++) {
                ShortcutIds[i] = "";
            }
            shortcutsGO = new List<GameObject>();
            foreach (Transform item in transform) {
                shortcutsGO.Add(item.gameObject);
            }
        }

        public void SetDragAndDropBuild(GameObject go, Vector3 offset) {
            mouseOffset = offset;
            IsDragging = true;
            dragADropGO = Instantiate(go);
            dragADropGO.transform.SetParent(transform.parent, false);
            dragADropGO.GetComponent<StructureBuildUI>().Show(go.GetComponent<StructureBuildUI>().structure, false);
            Color c = dragADropGO.GetComponent<Image>().color;
            c.a = 0.3f;
            dragADropGO.GetComponent<Image>().color = c;
            dragADropGO.GetComponent<RectTransform>().sizeDelta = go.GetComponent<RectTransform>().sizeDelta;
            ShortCutMenuEmpties(true);
            if (shortcutParentToButton.ContainsValue(go)) {
                shortCutDraggedParent = go.transform.parent.gameObject;
            }
        }

        public void EndDragAndDropBuild() {
            IsDragging = false;
            GameObject parent = null;
            foreach (GameObject shortcut in shortcutsGO) {
                Rect shortRect = shortcut.GetComponent<RectTransform>().rect;
                shortRect.position = shortcut.transform.position;
                Rect dragRect = dragADropGO.GetComponent<RectTransform>().rect;
                dragRect.position = dragADropGO.transform.position;
                //prefer the one with the mousepointer over it
                if (shortRect.Contains(Input.mousePosition)) {
                    parent = shortcut;
                    if (shortcutParentToButton.ContainsKey(shortcut))
                        continue;
                    break;
                }
                else //for case the mouse is not over it but still overlaps the rectangle
                if (shortRect.Overlaps(dragRect))
                    parent = shortcut;
            }
            //delete existing if exists
            if (shortCutDraggedParent != null) {
                shortCutDraggedParent.transform.GetChild(0).gameObject.SetActive(true);
                Destroy(shortcutParentToButton[shortCutDraggedParent]);
                shortcutParentToButton.Remove(shortCutDraggedParent);
                shortCutDraggedParent.transform.GetChild(0).gameObject.SetActive(false);
                shortCutDraggedParent = null;
            }
            //if it gets added
            if (parent != null) {
                if (shortcutParentToButton.ContainsKey(parent)) {
                    Destroy(parent.GetComponentInChildren<StructureBuildUI>().gameObject);
                    shortcutParentToButton.Remove(parent);
                }
                CreateButton(dragADropGO.GetComponentInChildren<StructureBuildUI>().structure, parent);
                GameObject sc = shortcutParentToButton[parent];
                ShortcutIds[shortcutsGO.IndexOf(sc)] = sc.GetComponentInChildren<StructureBuildUI>().structure.ID;
            }
            // stopping drag everytime so delete dragged & unshow spots
            StopDragAndDropBuild();
        }

        public void StopDragAndDropBuild() {
            // stopping drag everytime so delete dragged & unshow spots
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

        private void Update() {
            if (dragADropGO != null) {
                dragADropGO.transform.position = Input.mousePosition - mouseOffset;
            }
        }

        public string[] GetShortCutSave() {
            return ShortcutIds;
        }

        public void LoadShortCuts(string[] shortcuts) {
            if (shortcuts == null)
                return;
            ShortcutIds = shortcuts;
            for (int pos = 0; pos < shortcuts.Length; pos++) {
                Structure structure = PrototypController.Instance.GetStructure(shortcuts[pos]);
                if (structure == null)
                    continue;
                if (pos > shortcutsGO.Count)
                    break;
                CreateButton(structure, shortcutsGO[pos]);
            }
            Array.Resize(ref ShortcutIds, ShortcutCount);
        }

        private void CreateButton(Structure structure, GameObject parent) {
            Button go = Instantiate(buildButtonPrefab);
            go.name = "ShortCut " + structure.ID;
            go.GetComponent<StructureBuildUI>().Show(structure, true);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = Vector3.zero;
            go.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            Color c = go.GetComponent<Image>().color;
            c.a = 0.8f;
            go.GetComponent<Image>().color = c;
            parent.transform.GetChild(0).gameObject.SetActive(false);
            shortcutParentToButton[parent] = go.gameObject;
        }
        private void OnDestroy() {
            Instance = null;
        }
    }
}