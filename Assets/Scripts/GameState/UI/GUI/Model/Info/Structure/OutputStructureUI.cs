using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI.Model {

    public class OutputStructureUI : MonoBehaviour {
        public Transform inputContent;
        public Transform outputContent;
        public GameObject progressContent;
        public GameObject itemPrefab;
        public GameObject itemORSeperatorPrefab;

        private Dictionary<Item, ItemUI> itemToGO;

        private OutputStructure currentStructure;

        private Slider progress;
        private Text efficiency;
        private Item currORItem;

        public void Show(object ustr) {
            if (currentStructure == ustr) {
                return;
            }
            currentStructure = ustr as OutputStructure;
            if (currentStructure == null)
                return;
            TileDeciderFuncs.Structure = currentStructure;
            TileSpriteController.Instance.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
            if (currentStructure.StructureRange > 0) {
                TileSpriteController.Instance.AddDecider(TileDeciderFuncs.StructureTileDecider);
            }
            efficiency = progressContent.GetComponentInChildren<Text>();
            progress = progressContent.GetComponentInChildren<Slider>();
            progress.maxValue = currentStructure.TotalProgress;
            progress.value = 0;
            if (itemToGO != null) {
                foreach (ItemUI go in itemToGO.Values) {
                    Destroy(go.gameObject);
                }
            }
            itemToGO = new Dictionary<Item, ItemUI>();
            if (currentStructure.Output != null) {
                for (int i = 0; i < currentStructure.Output.Length; i++) {
                    ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
                    go.SetItem(currentStructure.Output[i], currentStructure.MaxOutputStorage);
                    go.transform.SetParent(outputContent, false);
                    itemToGO.Add(currentStructure.Output[i], go);
                }
            }
            foreach (Transform transform in inputContent) {
                Destroy(transform.gameObject);
            }
            if (ustr is ProductionStructure pstr) {
                if (pstr.Intake == null) {
                    return;
                }
                if (pstr.InputTyp == InputTyp.AND) {
                    for (int i = 0; i < pstr.Intake.Length; i++) {
                        ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
                        go.SetItem(pstr.Intake[i], pstr.GetMaxIntakeForIntakeIndex(i));
                        go.transform.SetParent(inputContent, false);
                        itemToGO.Add(pstr.Intake[i], go);
                    }
                }
                else if (pstr.InputTyp == InputTyp.OR) {
                    for (int i = 0; i < pstr.ProductionData.intake.Length; i++) {
                        ItemUI go = Instantiate(itemPrefab).GetComponent<ItemUI>();
                        if (i > 0) {
                            GameObject or = Instantiate(itemORSeperatorPrefab);
                            or.transform.SetParent(inputContent, false);
                        }
                        if (i == pstr.OrItemIndex) {
                            go.SetItem(pstr.Intake[0], pstr.GetMaxIntakeForIntakeIndex(pstr.OrItemIndex));
                            currORItem = pstr.Intake[0];
                            itemToGO.Add(pstr.Intake[0], go);
                        }
                        else {
                            go.SetItem(pstr.ProductionData.intake[i], pstr.GetMaxIntakeForIntakeIndex(i));
                            int temp = i;
                            go.AddClickListener((s) => { OnItemClick(pstr.ProductionData.intake[temp]); });
                            go.SetInactive(true);
                            itemToGO.Add(pstr.ProductionData.intake[i], go);
                        }
                        go.transform.SetParent(inputContent, false);
                    }
                }
            }
        }


        public void OnItemClick(Item item) {
            //first get remove the current orItem and add the version from intake
            itemToGO[currORItem].SetInactive(true);
            ItemUI go = itemToGO[currORItem];
            ProductionStructure pstr = (ProductionStructure)currentStructure;
            for (int i = 0; i < pstr.ProductionData.intake.Length; i++) {
                if (pstr.ProductionData.intake[i].ID == currORItem.ID) {
                    SwitchItemKey(currORItem, pstr.ProductionData.intake[i]);
                    go.AddClickListener((s) => { OnItemClick(pstr.ProductionData.intake[i]); });
                    break;
                }
            }
            itemToGO[item].SetInactive(false);
            //now change the input to the selected
            //also do change the associated item
            if (currentStructure is ProductionStructure) {
                ((ProductionStructure)currentStructure).ChangeInput(item);
            }
            go = itemToGO[item];
            go.ClearAllTriggers();
            SwitchItemKey(item, ((ProductionStructure)currentStructure).Intake[0]);
            currORItem = ((ProductionStructure)currentStructure).Intake[0];
        }

        private void SwitchItemKey(Item oldKey, Item newKey) {
            ItemUI go = itemToGO[oldKey];
            itemToGO.Remove(oldKey);
            itemToGO[newKey] = go;
        }

        // Update is called once per frame
        private void Update() {
            if (currentStructure == null) {
                Debug.LogError("Why is it open, when it has no structure?");
                return;
            }
            if (currentStructure.PlayerNumber != PlayerController.currentPlayerNumber)
                UIController.Instance.CloseInfoUI();
            foreach (Item item in itemToGO.Keys) {
                itemToGO[item].ChangeItemCount(item);
            }
            progress.value = currentStructure.Progress;
            efficiency.text = currentStructure.EfficiencyPercent + "%";
        }

        public void OnDisable() {
            currentStructure?.CloseExtraUI();
            currentStructure = null;
            TileDeciderFuncs.Structure = null;
            TileSpriteController.Instance?.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
            MouseController.Instance?.UnselectStructure();
        }
    }
}