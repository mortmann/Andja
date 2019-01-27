﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class UnitUI : MonoBehaviour {
    public Canvas content;
    public GameObject itemPrefab;
    public GameObject settleButton;
    public GameObject patrolButton;

    public GameObject buttonCanvas;
    public ItemUI cannonsItem;
    public GameObject unitCombatInfo;

    public Text healthText;
    public Inventory inv;
    Dictionary<int, ItemUI> itemToGO;
    Unit unit;

    bool IsCurrentShipUI => unit is Ship;
    public Button addCannon;
    public Button removeCannon;
    private List<GameObject> unitGoalGOs;
    public GameObject unitGoalPrefab;

    public void Show(Unit unit) {

        this.unit = unit;

        settleButton.SetActive(unit.IsPlayerUnit());
        patrolButton.SetActive(unit.IsPlayerUnit());
        cannonsItem.gameObject.SetActive(unit.IsPlayerUnit());
        addCannon.gameObject.SetActive(unit.IsPlayerUnit());
        removeCannon.gameObject.SetActive(unit.IsPlayerUnit());
        //clear inventory screen
        foreach (Transform item in content.transform) {
            GameObject.Destroy(item.gameObject);
        }
        unitGoalGOs = new List<GameObject>();
        if (unitGoalGOs != null)
            foreach (GameObject goal in unitGoalGOs)
                goal.SetActive(unit.IsPlayerUnit());
        if (unit.IsPlayerUnit() == false) {
            return;
        }

        inv = unit.inventory;
        buttonCanvas.SetActive(true);

        //only ships can settle
        if (IsCurrentShipUI) {
            Ship ship = ((Ship)unit);
            cannonsItem.gameObject.transform.parent.gameObject.SetActive(true);
            cannonsItem.SetItem(ship.CannonItem, ship.MaximumAmountOfCannons);
            settleButton.SetActive(true);
        }
        else {
            cannonsItem.gameObject.transform.parent.gameObject.SetActive(false);
            settleButton.SetActive(false);
        }

        if (inv == null) {
            return;
        }

        inv.RegisterOnChangedCallback(OnInvChange);
        itemToGO = new Dictionary<int, ItemUI>();
        if (inv == null) {
            return;
        }
        for (int i = 0; i < inv.NumberOfSpaces; i++) {
            AddItemGameObject(i);
        }
    }

    private void AddItemGameObject(int i) {
        GameObject go = GameObject.Instantiate(itemPrefab);
        go.transform.SetParent(content.transform);
        ItemUI iui = go.GetComponent<ItemUI>();
        if (inv.Items.ContainsKey(i) == false) {
            go.name = "item " + i;
            iui.SetItem(null, inv.MaxStackSize);
            itemToGO.Add(i, iui);
            return;
        }
        Item item = inv.Items[i];
        go.name = "item " + i;
        if (item.ID != -1) {
            iui.SetItem(item, inv.MaxStackSize);
            iui.AddClickListener((data) => { OnItemClick(i); });
            //			EventTrigger trigger = go.GetComponent<EventTrigger> ();
            //			EventTrigger.Entry entry = new EventTrigger.Entry( );
            //			entry.eventID = EventTriggerType.PointerClick;
            //			entry.callback.AddListener(  );
            //			trigger.triggers.Add( entry );
        }
        itemToGO.Add(i, iui);

    }
    void OnItemClick(int clicked) {
        Debug.Log("clicked " + clicked);
        unit.ToTradeItemToNearbyWarehouse(inv.Items[clicked]);
    }
    public void OnInvChange(Inventory changedInv) {
        foreach (int i in itemToGO.Keys) {
            GameObject.Destroy(itemToGO[i].gameObject);
        }
        itemToGO = new Dictionary<int, ItemUI>();
        for (int i = 0; i < inv.NumberOfSpaces; i++) {
            AddItemGameObject(i);
        }
        inv = changedInv;

    }
    public void Update() {
        if (unit.CurrHealth <= 0) {
            UIController.Instance.CloseUnitUI();
        }
        healthText.text = Mathf.CeilToInt(unit.CurrHealth) + "/" + unit.MaxHealth + "HP";
        if (unit.IsPlayerUnit()) {
            if (IsCurrentShipUI) {
                Ship ship = ((Ship)unit);
                if (ship.HasCannonsToAddInInventory() != addCannon.gameObject.activeSelf) {
                    addCannon.gameObject.SetActive(ship.HasCannonsToAddInInventory());
                }
                if (ship.CanRemoveCannons() != removeCannon.gameObject.activeSelf) {
                    removeCannon.gameObject.SetActive(ship.CanRemoveCannons());
                }
                cannonsItem.RefreshItem(((Ship)unit).CannonItem);
            }
            if (unit.QueuedCommands != null) {
                //if (unitGoalGOs[0] == null)
                //    unitGoalGOs.Add(Instantiate(unitGoalPrefab));
                //if (unitGoalGOs[0].activeSelf == false)
                //    unitGoalGOs[0].SetActive(true);
                //unitGoalGOs[0].transform.position = new Vector3(unit.pathfinding.dest_X, unit.pathfinding.dest_Y);
                for (int i = 0; i < unit.QueuedCommands.Count; i++) {
                    Command c = unit.QueuedCommands[i];
                    if (c is MoveCommand == false) {
                        continue; // TODO: make it otherwise visible
                    }
                    if (unitGoalGOs.Count - 1 < i)
                        unitGoalGOs.Add(Instantiate(unitGoalPrefab));
                    unitGoalGOs[i].transform.position = ((MoveCommand)c).position;
                }
                while (unit.QueuedCommands.Count < unitGoalGOs.Count) {
                    Destroy(unitGoalGOs[unitGoalGOs.Count - 1]);
                    unitGoalGOs.RemoveAt(unitGoalGOs.Count - 1);
                }
            }
        }
    }
    //TODO: make this possible with 
    public void AddCannons() {
        if (IsCurrentShipUI == false) {
            return;
        }
        Ship ship = ((Ship)unit);
        ship.AddCannonsFromInventory();
    }
    public void RemoveCannons() {
        if (IsCurrentShipUI == false) {
            return;
        }
        Ship ship = ((Ship)unit);
        ship.RemoveCannonsToInventory();
    }
    private void OnDisable() {
        if (unitGoalGOs == null)
            return;
        foreach (var unitGoalGO in unitGoalGOs) {
            Destroy(unitGoalGO);
        }
        unitGoalGOs.Clear();
    }
}
