using Andja.Model;
using Andja.Controller;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Andja.UI.Model {

    public class InfoUI : MonoBehaviour {
        public static InfoUI Instance;
        public GameObject SingleUI;
        public GameObject DifferentSingleUI;
        public GameObject GroupUI;

        public HiddenInputField inputName;
        public HealthBarUI healthBarUI;
        public Button Sleep;
        public ImageText upkeepIText;

        public MilitaryStructureUI militaryGO;
        public OutputStructureUI outputStructureUI;
        public StructureUI structureUI;
        public UnitUI unitUI;
        public UnitGroupUI unitGroupUI;

        public EffectsUI Effects;

        private void Awake() {
            Instance = this;
        }

        public void Show(Structure structure) {
            ActivateUI(true);
            inputName.Set(structure.Name);
            if (structure.IsPlayer()) {
                structure.OpenExtraUI();
                upkeepIText.gameObject.SetActive(true);
                upkeepIText.Set(UISpriteController.GetIcon(CommonIcon.Upkeep), StaticLanguageVariables.Upkeep, structure.UpkeepCost + "");
                if (structure is MilitaryStructure) {
                    militaryGO.gameObject.SetActive(true);
                    militaryGO.Show(structure);
                }
                else if (structure is OutputStructure) {
                    outputStructureUI.gameObject.SetActive(true);
                    Sleep.onClick.AddListener(structure.ToggleActive);
                    outputStructureUI.Show(structure);
                }
                else {
                    Sleep.gameObject.SetActive(false);
                    structureUI.gameObject.SetActive(true);
                    structureUI.Show(structure);
                }
                Effects.gameObject.SetActive(true);
                Effects.Show(structure);
            }
            else {
                Effects.gameObject.SetActive(false);
                upkeepIText.gameObject.SetActive(false);
                Sleep.gameObject.SetActive(false);
                structureUI.gameObject.SetActive(true);
                structureUI.Show(structure);
            }

        }
        public void Show(Unit unit) {
            ActivateUI(true);
            inputName.Set(unit.PlayerSetName ?? unit.Name, unit.SetName, unit.IsPlayer());
            if(unit.IsPlayer()) {
                Effects.gameObject.SetActive(true);
                Effects.Show(unit);
                upkeepIText.gameObject.SetActive(true);
                upkeepIText.Set(UISpriteController.GetIcon(CommonIcon.Upkeep), StaticLanguageVariables.Upkeep, unit.UpkeepCost + "");
            } else {
                upkeepIText.gameObject.SetActive(false);
                Effects.gameObject.SetActive(false);
            }
            unitUI.gameObject.SetActive(true);
            unitUI.Show(unit);
        }
        public void Show(List<Unit> unit) {
            ActivateUI(false);
            unitGroupUI.gameObject.SetActive(true);
            unitGroupUI.Show(unit);
        }
        public void ActivateUI(bool single) {
            if(single) {
                SingleUI.SetActive(true);
                foreach (Transform t in DifferentSingleUI.transform)
                    t.gameObject.SetActive(false);
                GroupUI.SetActive(false);
                Sleep.gameObject.SetActive(true);
            }
            else {
                SingleUI.SetActive(false);
                GroupUI.SetActive(true);
                Sleep.gameObject.SetActive(false);
                foreach (Transform t in GroupUI.transform)
                    t.gameObject.SetActive(false);
            }
        }
        public void UpdateUpkeep(int upkeep) {
            upkeepIText.SetText(upkeep + "");
        }
        public void UpdateHealth(float hp, float maxHP) {
            healthBarUI.SetHealth(hp, maxHP);
        }

        private void OnDisable() {
        }
    }
}