using Andja.Controller;
using Andja.Model;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.UI.Model {

    public class MilitaryStructureUI : MonoBehaviour {
        public GameObject unitSelectionPanel;
        public CurrentlyBuildingUnitUI currentlyBuildingUnit;
        public UnitBuildUI unitSelectPrefab;
        private MilitaryStructure CurrentMilitary;
        private Dictionary<Unit, UnitBuildUI> unitToBuildUI;

        public void Show(object str) {
            if (CurrentMilitary == str) {
                return;
            }
            if (str is MilitaryStructure == false) {
                Debug.Log("Structure is not a Military!");
                return;
            }
            CurrentMilitary = (MilitaryStructure)str;
            CurrentMilitary.RegisterOnDestroyCallback(OnStructureDestroy);
            foreach (Transform child in unitSelectionPanel.transform) {
                Destroy(child.gameObject);
            }
            unitToBuildUI = new Dictionary<Unit, UnitBuildUI>();
            foreach (Unit u in CurrentMilitary.CanBeBuildUnits) {
                if (u == null) {
                    Debug.LogError("Unit is null");
                    continue;
                }

                UnitBuildUI ubui = Instantiate<UnitBuildUI>(unitSelectPrefab);
                ubui.transform.SetParent(unitSelectionPanel.transform, false);
                ubui.Show(u);
                Unit temp = u;
                ubui.SetIsBuildable(CurrentMilitary.HasEnoughResources(u));

                ubui.AddClickListener(() => { CurrentMilitary.AddUnitToBuildQueue(temp); });
                unitToBuildUI.Add(u, ubui);
            }
            currentlyBuildingUnit.Show(CurrentMilitary);
        }

        private void OnStructureDestroy(Structure str, IWarfare destroyer) {
            UIController.Instance.CloseInfoUI();
        }

        // Update is called once per frame
        private void Update() {
            if (CurrentMilitary.PlayerNumber != PlayerController.currentPlayerNumber)
                UIController.Instance.CloseInfoUI();
            foreach (Unit u in CurrentMilitary.CanBeBuildUnits) {
                unitToBuildUI[u].SetIsBuildable(CurrentMilitary.HasEnoughResources(u));
            }
            InfoUI.Instance.UpdateHealth(CurrentMilitary.CurrentHealth, CurrentMilitary.MaxHealth);
            InfoUI.Instance.UpdateUpkeep(CurrentMilitary.UpkeepCost);
        }

        public void OnDisable() {
            CurrentMilitary?.UnregisterOnDestroyCallback(OnStructureDestroy);
            MouseController.Instance?.UnselectStructure();
            CurrentMilitary = null;
        }
    }
}