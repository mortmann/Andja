using Andja.Model;
using Andja.UI;
using Andja.UI.GameDebug;
using Andja.UI.Menu;
using Andja.UI.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Andja.Controller {

    public class UIController : MonoBehaviour {
        public GameObject mainCanvas;
        public GameObject shortCutCanvas;

        public GameObject uiInfoCanvas;

        public GameObject chooseBuildCanvas;

        public GameObject consoleCanvas;

        public GameObject productionStructureInfo;
        public GameObject structureInfo;
        public GameObject unitCanvas;
        public GameObject unitGroupUI;
        public GameObject militaryStructureInfo;

        public GameObject rightCanvas;
        public GameObject CityInventoryCanvas;
        public GameObject citizenCanvas;
        public GameObject tradeRouteCanvas;
        public PauseMenu pauseMenu;
        public GameObject offWorldMapCanvas;
        public GameObject otherCityUI;
        public GameObject diplomacyCanvas;

        public GameObject centerParent;
        public GameObject endScoreScreen;

        public GameObject debugInformation;
        private DebugInformation debug;
        public static UIController Instance;
        private static UIControllerSave uIControllerSave;

        private void Start() {
            Escape(true);
            endScoreScreen.SetActive(false);
            if (Instance != null) {
                Debug.LogError("There are two uicontroller");
            }
            Instance = this;
            if (uIControllerSave != null) {
                LoadUISaveData();
            }
        }

        internal void ToggleDiplomacyMenu() {
            if (diplomacyCanvas.activeSelf) {
                CloseCenter();
            }
            else {
                OpenDiplomacyUI();
            }
        }

        public void OpenStructureUI(Structure str) {
            if (str == null) {
                return;
            }
            if (str.PlayerNumber != PlayerController.currentPlayerNumber) {
                if (str is WarehouseStructure) {
                    OpenOtherCity(str.City);
                    return;
                }
            }
            CloseInfoUI();
            CloseRightUI();
            str.RegisterOnDestroyCallback(OnStructureDestroy);

            if (str is HomeStructure) {
                OpenHomeUI((HomeStructure)str);
                return;
            }
            str.OpenExtraUI();
            if (str is ProductionStructure) {
                OpenProduktionUI((OutputStructure)str);
            }
            else
            if (str is MineStructure || str is FarmStructure) {
                OpenProduceUI((OutputStructure)str);
            }
            else
            if (str is MarketStructure || str is WarehouseStructure) {
                OpenCityInventory(str.City);
            }
            else
            if (str is MilitaryStructure) {
                OpenMilitaryStructureInfo(str);
            }
            else {
                ShowStructureUI(str);
            }
        }

        private void ShowStructureUI(Structure str) {
            OpenInfoUI();
            structureInfo.SetActive(true);
            structureInfo.GetComponent<StructureUI>().Show(str);
        }

        public void OnStructureDestroy(Structure str, IWarfare destroyer) {
            CloseInfoUI();
        }

        public void OpenDiplomacyUI() {
            CloseCenter();
            OpenCenter();
            diplomacyCanvas.SetActive(true);
        }

        public void OpenCenter() {
            centerParent.SetActive(true);
        }

        public void CloseCenter() {
            foreach (Transform t in centerParent.transform) {
                t.gameObject.SetActive(false);
            }
            centerParent.SetActive(false);
        }

        internal void ShowEndScoreScreen() {
            WorldController.Instance.Pause();
            endScoreScreen.SetActive(true);
        }

        public void OpenOtherCity(City city) {
            if (city == null) {
                return;
            }
            CloseRightUI();
            rightCanvas.SetActive(true);
            otherCityUI.SetActive(true);
            otherCityUI.GetComponent<OtherCityUI>().Show(city);
        }

        public void OpenCityInventory(City city, System.Action<Item> onItemPressed = null) {
            if (city == null) {
                return;
            }
            CloseRightUI();
            rightCanvas.SetActive(true);
            CityInventoryCanvas.SetActive(true);
            CityInventoryCanvas.GetComponent<CityInventoryUI>().ShowInventory(city, onItemPressed);
        }

        public void HideCityUI(City city) {
            otherCityUI.SetActive(false);
            CityInventoryCanvas.SetActive(false);
        }

        public void ToggleRightUI() {
            rightCanvas.SetActive(!rightCanvas.activeSelf);
        }

        public void ShowBuildMenu() {
            chooseBuildCanvas.SetActive(!chooseBuildCanvas.activeSelf);
        }

        public void OpenProduktionUI(OutputStructure str) {
            if (str == null) {
                return;
            }
            OpenInfoUI();
            productionStructureInfo.SetActive(true);
            productionStructureInfo.GetComponent<OutuputStructureUI>().Show(str);
        }

        public void CloseOutputStructureUI() {
            productionStructureInfo.SetActive(false);
            CloseInfoUI();
        }

        public void OpenMilitaryStructureInfo(Structure str) {
            if (str == null) {
                return;
            }
            OpenInfoUI();
            militaryStructureInfo.SetActive(true);
            militaryStructureInfo.GetComponent<MilitaryStructureUI>().Show(str);
        }

        public void CloseMilitaryStructureInfo() {
            productionStructureInfo.SetActive(false);
            CloseInfoUI();
        }

        public void TogglePauseMenu() {
            pauseMenu?.Toggle();
            WorldController.Instance.IsPaused = PauseMenu.IsOpen;
        }

        public void OpenProduceUI(OutputStructure str) {
            if (str == null) {
                return;
            }
            OpenInfoUI();
            productionStructureInfo.SetActive(true);
            productionStructureInfo.GetComponent<OutuputStructureUI>().Show(str);
        }

        public void CloseProduceUI() {
            productionStructureInfo.SetActive(false);
            CloseInfoUI();
        }

        public void OpenUnitUI(Unit u) {
            if (u == null) {
                Debug.LogError("Unit Script is null");
                return;
            }
            if (u.rangeUStructure != null) {
                if (u.rangeUStructure is WarehouseStructure) {
                    if (u.rangeUStructure.PlayerNumber == PlayerController.currentPlayerNumber) {
                        CloseRightUI();
                        u.rangeUStructure.City.tradeUnit = u;
                        City c = u.rangeUStructure.City;
                        OpenCityInventory(c, item => c.TradeWithShip(c.Inventory.GetItemInInventoryClone(item)));
                    }
                }
            }
            productionStructureInfo.SetActive(false);
            CloseInfoUI();
            OpenInfoUI();
            unitCanvas.SetActive(true);
            unitCanvas.GetComponent<UnitUI>().Show(u);
        }

        public void OpenUnitGroupUI(Unit[] units) {
            if (units == null) {
                Debug.LogError("UnitGroup is null");
                return;
            }
            CloseInfoUI();
            OpenInfoUI();
            unitGroupUI.SetActive(true);
            unitGroupUI.GetComponent<UnitGroupUI>().Show(units);
        }

        public void OpenInfoUI() {
            uiInfoCanvas.SetActive(true);
        }

        public void CloseInfoUI() {
            if (uiInfoCanvas.activeSelf == false) {
                return;
            }
            //MouseController.Instance.UnselectStuff();
            unitCanvas.SetActive(false);
            productionStructureInfo.SetActive(false);
            militaryStructureInfo.SetActive(false);
            unitGroupUI.SetActive(false);
            uiInfoCanvas.SetActive(false);
        }

        public void CloseChooseBuild() {
            chooseBuildCanvas.SetActive(false);
        }

        public void CloseRightUI() {
            otherCityUI.SetActive(false);
            CityInventoryCanvas.SetActive(false);
            citizenCanvas.SetActive(false);
            rightCanvas.SetActive(false);
        }

        public void OpenHomeUI(HomeStructure hb) {
            CloseRightUI();
            rightCanvas.SetActive(true);
            citizenCanvas.SetActive(true);
            citizenCanvas.GetComponent<NeedsUIController>().Show(hb);
        }

        public void CloseHomeUI() {
            CloseRightUI();
        }

        public void OpenTradeRouteMenu() {
            CloseChooseBuild();
            CloseCenter();
            OpenCenter();
            if (tradeRouteCanvas.activeSelf) {
                return;
            }
            tradeRouteCanvas.SetActive(true);
            tradeRouteCanvas.GetComponent<MapImage>().Show();
        }

        public void ToggleOffWorldMenu() {
            if (offWorldMapCanvas.activeSelf) {
                CloseOffWorldMenu();
            }
            else {
                OpenOffWorldMenu();
            }
        }

        public void CloseOffWorldMenu() {
            offWorldMapCanvas.SetActive(false);
        }

        public void OpenOffWorldMenu() {
            CloseCenter();
            OpenCenter();
            tradeRouteCanvas.SetActive(false);
            offWorldMapCanvas.SetActive(true);
        }

        public void ToggleTradeMenu() {
            if (tradeRouteCanvas.activeSelf) {
                CloseTradeMenu();
            }
            else {
                OpenTradeRouteMenu();
            }
        }

        public void CloseTradeMenu() {
            tradeRouteCanvas.SetActive(false);
        }

        public void Escape(bool dontOpenPause = false) {
            if (AnyMenuOpen() == false && dontOpenPause == false && MouseController.Instance.MouseState == MouseState.Idle) {
                TogglePauseMenu();
            }
            CloseCenter();
            CloseConsole();
            CloseRightUI();
            CloseInfoUI();
            CloseChooseBuild();
        }

        public void HighlightUnits(params Unit[] units) {
            UnitSpriteController.Instance.Highlight(units);
        }

        public void DehighlightUnits(params Unit[] units) {
            UnitSpriteController.Instance.Dehighlight(units);
        }

        public bool IsPauseMenuOpen() {
            return PauseMenu.IsOpen;
        }

        internal void CloseMouseUnselect() {
            CloseInfoUI();
            CloseRightUI();
            CloseChooseBuild();
        }

        public bool AnyMenuOpen() {
            return rightCanvas.activeSelf || uiInfoCanvas.activeSelf ||
                chooseBuildCanvas.activeSelf || tradeRouteCanvas.activeSelf ||
                consoleCanvas.activeSelf || centerParent.activeSelf;
        }

        public void SetDragAndDropBuild(GameObject go, Vector2 offset) {
            shortCutCanvas.GetComponent<ShortcutUI>().SetDragAndDropBuild(go, offset);
        }

        public void StopDragAndDropBuild() {
            shortCutCanvas.GetComponent<ShortcutUI>().EndDragAndDropBuild();
        }

        public void ToggleConsole() {
            consoleCanvas.SetActive(!consoleCanvas.activeSelf);
        }

        public void CloseConsole() {
            consoleCanvas.SetActive(false);
        }

        public void ShowDebugForObject(object toDebug) {
            debug = Instantiate(debugInformation).GetComponent<DebugInformation>();
            debug.Show(toDebug);
        }

        public static bool IsTextFieldFocused() {
            if (EventSystem.current.currentSelectedGameObject == null || EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() == null) {
                return false;
            }
            return (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()).isFocused;
        }

        public UIControllerSave GetUISaveData() {
            return new UIControllerSave(FindObjectOfType<ShortcutUI>().GetShortCutSave());
        }

        public void LoadUISaveData() {
            FindObjectOfType<ShortcutUI>().LoadShortCuts(uIControllerSave.shortcuts);
            uIControllerSave = null;
        }

        private void OnDestroy() {
            Instance = null;
        }

        internal static void SetSaveUIData(UIControllerSave uics) {
            uIControllerSave = uics;
        }
    }

    [Serializable]
    public class UIControllerSave : BaseSaveData {
        public Dictionary<int, string> shortcuts;

        public UIControllerSave(Dictionary<int, string> shortcuts) {
            this.shortcuts = shortcuts;
        }

        public UIControllerSave() {
        }
    }
}