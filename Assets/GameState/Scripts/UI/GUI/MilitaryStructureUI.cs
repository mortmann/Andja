﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MilitaryStructureUI : MonoBehaviour {
    public GenericStructureUI StructureUI;
    public GameObject unitSelectionPanel;
    public CurrentlyBuildingUnitUI currentlyBuildingUnit;
    public UnitBuildUI unitSelectPrefab;
    private MilitaryStructure military;
    Dictionary<Unit, UnitBuildUI> unitToBuildUI;
    public void Show(Structure str) {
        if (str is MilitaryStructure == false) {
            Debug.Log("Structure is not a Military!");
            return;
        }
        StructureUI.gameObject.SetActive(true);
        StructureUI.Show(str);
        military = (MilitaryStructure)str;
        foreach (Transform child in unitSelectionPanel.transform) {
            Destroy(child.gameObject);
        }
        unitToBuildUI = new Dictionary<Unit, UnitBuildUI>();
        foreach (Unit u in military.CanBeBuildUnits) {
            if (u == null) {
                Debug.LogError("Unit is null");
                continue;
            }

            UnitBuildUI ubui = Instantiate<UnitBuildUI>(unitSelectPrefab);
            ubui.transform.SetParent(unitSelectionPanel.transform);
            ubui.Show(u);
            Unit temp = u;
            ubui.SetIsBuildable(military.HasEnoughResources(u));

            ubui.AddClickListener(() => { military.AddUnitToBuildQueue(temp); });
            unitToBuildUI.Add(u, ubui);
        }
        currentlyBuildingUnit.Show(military);
    }
    // Update is called once per frame
    void Update() {
        foreach (Unit u in military.CanBeBuildUnits) {
            unitToBuildUI[u].SetIsBuildable(military.HasEnoughResources(u));
        }
    }
    private void OnDisable() {
        StructureUI.gameObject.SetActive(false);
    }
}
