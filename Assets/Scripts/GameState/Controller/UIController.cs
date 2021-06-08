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
    public enum TextColor { Positive, Neutral, Negative }
    /// <summary>
    /// Opening and Closeing UIs being handled. ShortCutMenu saved here.
    /// </summary>
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
        public GameObject DebugData;

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
        public void ToggleUI() {
            mainCanvas.SetActive(!mainCanvas.activeSelf);
        }
        public void ChangeAllUI(bool value) {
            mainCanvas.SetActive(value);
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
            if (str is WarehouseStructure || str is MarketStructure) {
                CloseInfoUI();
                if (str.PlayerNumber != PlayerController.currentPlayerNumber) {
                    OpenOtherCity(str.City);
                }
                else {
                    OpenCityInventory(str.City);
                }
                return;
            }
            if (str is HomeStructure) {
                if (str.PlayerNumber == PlayerController.currentPlayerNumber) {
                    CloseRightUI();
                    CloseInfoUI();
                    OpenHomeUI((HomeStructure)str);
                }
                return;
            }
            uiInfoCanvas.SetActive(true);
            uiInfoCanvas.GetComponent<InfoUI>().Show(str);
        }
        internal void OpenUnitUI(Unit unit) {
            if (unit == null) {
                return;
            }
            uiInfoCanvas.SetActive(true);
            uiInfoCanvas.GetComponent<InfoUI>().Show(unit);
        }

        public void OpenUnitGroupUI(List<Unit> units) {
            if (units == null) {
                Debug.LogError("UnitGroup is null");
                return;
            }
            uiInfoCanvas.SetActive(true);
            uiInfoCanvas.GetComponent<InfoUI>().Show(units);
        }

        public void CloseInfoUI() {
            if (uiInfoCanvas.activeSelf) {
                uiInfoCanvas.SetActive(false);
            }
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

        public void TogglePauseMenu() {
            pauseMenu?.Toggle();
            WorldController.Instance.IsPaused = PauseMenu.IsOpen;
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
            if (EventSystem.current.currentSelectedGameObject == null) {
                return false;
            }
            if ((EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>()) != null &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>()).isFocused)
                return true;
            if ((EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()) != null &&
                (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()).isFocused)
                return true;
            return false;
        }

        public UIControllerSave GetUISaveData() {
            return new UIControllerSave(FindObjectOfType<ShortcutUI>().GetShortCutSave());
        }

        public void LoadUISaveData() {
            FindObjectOfType<ShortcutUI>().LoadShortCuts(uIControllerSave.shortcuts);
            uIControllerSave = null;
        }
        public void ToggleDebugData() {
            DebugData.SetActive(!DebugData.activeSelf);
        }
        private void OnDestroy() {
            Instance = null;
        }

        internal static void SetSaveUIData(UIControllerSave uics) {
            uIControllerSave = uics;
        }
        public static string GetTextColor(TextColor color) {
            switch (color) {
                case TextColor.Positive:
                    return "#27ae60";
                case TextColor.Neutral:
                    return "#323232";
                case TextColor.Negative:
                    return "#e74c3c";
                default:
                    return "#000000";
            }
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