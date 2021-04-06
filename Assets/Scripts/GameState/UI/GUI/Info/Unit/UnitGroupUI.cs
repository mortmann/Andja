using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class UnitGroupUI : InfoUI {

    public Transform unitsContent;
    public UnitHealthUI unitHealthPrefab;

    Dictionary<Unit, UnitHealthUI> unitToUI;

    public override void OnShow(object show) {
        if (show.GetType() != typeof(Unit[]))
            return;
        Unit[] group = (Unit[])show;
        UIController.Instance.HighlightUnits(group);
        foreach (Transform t in unitsContent)
            Destroy(t.gameObject);
        unitToUI = new Dictionary<Unit, UnitHealthUI>();
        foreach (Unit unit in group) {
            AddUnit(unit);
        }
    }
    public void RemoveUnit(Unit unit) {
        Destroy(unitToUI[unit]);
        unitToUI.Remove(unit);
        MouseController.Instance.RemoveUnitFromGroup(unit);
    }
    public void RemoveUnit(Unit unit, IWarfare warfare) {
        RemoveUnit(unit);
    }
    public void AddUnit(Unit unit) {
        UnitHealthUI uhu = Instantiate(unitHealthPrefab);
        uhu.Show(unit);
        uhu.AddRightClick(RemoveUnit);
        unit.RegisterOnDestroyCallback(RemoveUnit);
        uhu.transform.SetParent(unitsContent, false);
        unitToUI.Add(unit, uhu);
    }
    
    public override void OnClose() {
        if (unitToUI != null)
            foreach (Unit unit in unitToUI.Keys) {
                unit.UnregisterOnDestroyCallback(RemoveUnit);
            }
        UIController.Instance.DehighlightUnits(unitToUI.Keys.ToArray());
        MouseController.Instance.UnselectUnitGroup();
    }
}
