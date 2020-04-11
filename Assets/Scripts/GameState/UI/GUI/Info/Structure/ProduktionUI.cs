using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
public class ProduktionUI : MonoBehaviour {
    public GenericStructureUI StructureUI;
    public Transform inputContent;
    public Transform outputContent;
    public GameObject progressContent;
    public GameObject itemPrefab;
    public GameObject itemORSeperatorPrefab;

    Dictionary<Item, ItemUI> itemToGO;

    OutputStructure currentStructure;

    Slider progress;
    Text efficiency;
    Item currORItem;

    public void Show(OutputStructure ustr) {
        if (this.currentStructure == ustr) {
            return;
        }
        StructureUI.gameObject.SetActive(true);
        StructureUI.Show(ustr);
        this.currentStructure = ustr;
        efficiency = progressContent.GetComponentInChildren<Text>();
        progress = progressContent.GetComponentInChildren<Slider>();
        progress.maxValue = currentStructure.ProduceTime;
        progress.value = 0;
        if (itemToGO != null) {
            foreach (ItemUI go in itemToGO.Values) {
                Destroy(go.gameObject);
            }
        }
        itemToGO = new Dictionary<Item, ItemUI>();
        if (ustr.Output != null) {
            for (int i = 0; i < ustr.Output.Length; i++) {
                ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
                go.SetItem(ustr.Output[i], ustr.MaxOutputStorage);
                go.transform.SetParent(outputContent);
                itemToGO.Add(ustr.Output[i], go);
            }
        }

        if (ustr is ProductionStructure) {
            ProductionStructure pstr = (ProductionStructure)ustr;
            if (pstr.Intake == null) {
                return;
            }
            if (pstr.InputTyp == InputTyp.AND) {
                for (int i = 0; i < pstr.Intake.Length; i++) {
                    ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
                    go.SetItem(pstr.Intake[i], pstr.GetMaxIntakeForIntakeIndex(i));
                    go.transform.SetParent(inputContent);
                    itemToGO.Add(pstr.Intake[i], go);
                }
            }
            else if (pstr.InputTyp == InputTyp.OR) {
                for (int i = 0; i < pstr.ProductionData.intake.Length; i++) {
                    ItemUI go = GameObject.Instantiate(itemPrefab).GetComponent<ItemUI>();
                    if (i > 0) {
                        GameObject or = GameObject.Instantiate(itemORSeperatorPrefab);
                        or.transform.SetParent(inputContent);
                    }
                    if (i == pstr.OrItemIndex) {
                        go.SetItem(pstr.Intake[0], pstr.GetMaxIntakeForIntakeIndex(pstr.OrItemIndex));
                        currORItem = pstr.Intake[0];
                        itemToGO.Add(pstr.Intake[0], go);
                    }
                    else {
                        go.SetItem(pstr.ProductionData.intake[i], pstr.GetMaxIntakeForIntakeIndex(i));
                        int temp = i;
                        go.AddClickListener((data) => { OnItemClick(pstr.ProductionData.intake[temp]); });
                        go.SetInactive(true);
                        itemToGO.Add(pstr.ProductionData.intake[i], go);
                    }
                    go.transform.SetParent(inputContent);
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
                go.AddClickListener((data) => { OnItemClick(pstr.ProductionData.intake[i]); });
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
    void Update() {
        if (currentStructure == null) {
            Debug.LogError("Why is it open, when it has no structure?");
            return;
        }
        foreach (Item item in itemToGO.Keys) {
            itemToGO[item].ChangeItemCount(item);
        }
        progress.value = currentStructure.produceCountdown;
        efficiency.text = currentStructure.EfficiencyPercent + "%";

    }
    private void OnDisable() {
        currentStructure = null;
        StructureUI.gameObject.SetActive(false);
    }
}
