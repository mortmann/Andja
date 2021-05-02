using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

namespace Andja.UI.Model {

    public class UnitGroupUI : MonoBehaviour {
        public Transform unitsContent;
        public UnitHealthUI unitHealthPrefab;

        private Dictionary<Unit, UnitHealthUI> unitToUI= new Dictionary<Unit, UnitHealthUI>();
        private void Awake() {
            foreach (Transform t in unitsContent)
                Destroy(t.gameObject);
        }
        public void Show(List<Unit> show) {
            if (unitToUI.Keys.Except(show).Any() == false)
                return;
            foreach(Unit u in unitToUI.Keys) {
                if(show.Contains(u)) {
                    continue;
                }
                Destroy(unitToUI[u].gameObject);
            }

            UIController.Instance.HighlightUnits(show.ToArray());
            
            foreach (Unit unit in show) {
                if (unit.IsPlayer() == false)
                    continue;
                if(unitToUI.ContainsKey(unit) == false) 
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

        public void OnDisable() {
            foreach (Unit unit in unitToUI.Keys) {
                unit.UnregisterOnDestroyCallback(RemoveUnit);
            }
            UIController.Instance.DehighlightUnits(unitToUI.Keys.ToArray());
            MouseController.Instance.UnselectUnitGroup();
        }
    }
}