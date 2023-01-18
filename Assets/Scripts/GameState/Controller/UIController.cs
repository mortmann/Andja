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
using static Andja.UI.Model.EventUIManager;

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
        public ChoiceDialog ChoiceDialog;

        public void Start() {
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
        internal void ShowChoiceDialog(ChoiceInformation choiceInformation) {
            CloseCenter();
            OpenCenter();
            ChoiceDialog.Show(choiceInformation);
        }
        public void OpenStructureUI(Structure str) {
            switch (str) {
                case null:
                    return;
                case WarehouseStructure _:
                case MarketStructure _: {
                    CloseInfoUI();
                    if (str.PlayerNumber != PlayerController.currentPlayerNumber) {
                        OpenOtherCity(str.City);
                    }
                    else {
                        OpenCityInventory(str.City, str.City.TradeWithAnyShip);
                    }
                    return;
                }
                case HomeStructure home: {
                    if (home.PlayerNumber != PlayerController.currentPlayerNumber) return;
                    CloseRightUI();
                    CloseInfoUI();
                    OpenHomeUI(home);
                    return;
                }
                default:
                    uiInfoCanvas.SetActive(true);
                    uiInfoCanvas.GetComponent<InfoUI>().Show(str);
                    break;
            }
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

        public void OpenOtherCity(ICity city) {
            if (city == null) {
                return;
            }
            CloseRightUI();
            rightCanvas.SetActive(true);
            otherCityUI.SetActive(true);
            otherCityUI.GetComponent<OtherCityUI>().Show(city);
        }

        public void OpenCityInventory(ICity city, System.Action<Item> onItemPressed = null) {
            if (city == null) {
                return;
            }
            CloseRightUI();
            rightCanvas.SetActive(true);
            CityInventoryCanvas.SetActive(true);
            CityInventoryCanvas.GetComponent<CityInventoryUI>().ShowInventory(city, onItemPressed);
        }

        public void HideCityUI(ICity city) {
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
                consoleCanvas.activeSelf || centerParent.activeSelf ||
                consoleCanvas.activeSelf;
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
            return (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()) != null &&
                   (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()).isFocused;
        }

        public UIControllerSave GetUISaveData() {
            return new UIControllerSave(FindObjectOfType<ShortcutUI>().GetShortCutSave(), EventUIManager.Instance.GetSave());
        }

        public void LoadUISaveData() {
            FindObjectOfType<ShortcutUI>().LoadShortCuts(uIControllerSave.shortcuts);
            EventUIManager.Instance.Load(uIControllerSave.eventUIData);
            uIControllerSave = null;
        }
        public void ToggleDebugData() {
            DebugData.SetActive(!DebugData.activeSelf);
        }
        public void OnDestroy() {
            Instance = null;
        }

        internal static void SetSaveUIData(UIControllerSave uics) {
            uIControllerSave = uics;
        }
        public static string GetTextColor(TextColor color) {
            return color switch {
                TextColor.Positive => "#27ae60",
                TextColor.Neutral => "#323232",
                TextColor.Negative => "#e74c3c",
                _ => "#000000"
            };
        }
    }

    [Serializable]
    public class UIControllerSave : BaseSaveData {
        public string[] shortcuts;
        public EventUISave eventUIData;

        public UIControllerSave(string[] shortcuts, EventUISave eventUIData) {
            this.shortcuts = shortcuts;
            this.eventUIData = eventUIData;
        }

        public UIControllerSave() {

        }
    }
}