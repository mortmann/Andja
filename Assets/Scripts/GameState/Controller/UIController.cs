using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

public class UIController : MonoBehaviour {

    public GameObject mainCanvas;
    public GameObject shortCutCanvas;

    public GameObject uiInfoCanvas;

    public GameObject chooseBuildCanvas;

    public GameObject consoleCanvas;

    public GameObject productionStructureInfo;
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

    public GameObject debugInformation;
    private DebugInformation debug;
    Structure openStructure;
    Unit openUnit;
    public static Dictionary<string, Sprite> ItemImages;
    public static UIController Instance;
    private static UIControllerSave uIControllerSave;

    void Start() {
        Escape(true);
        if (Instance != null) {
            Debug.LogError("There are two uicontroller");
        }
        Instance = this;
        Sprite[] sprites = Resources.LoadAll<Sprite>("Textures/Items/");
        Debug.Log(sprites.Length + " Item Sprite");
        ItemImages = new Dictionary<string, Sprite>();
        foreach (Sprite item in sprites) {
            ItemImages[item.name] = item;
        }
        foreach (Sprite item in ModLoader.LoadSprites(SpriteType.Item)) {
            ItemImages[item.name] = item;
        }
        if(uIControllerSave!=null) {
            LoadUISaveData();
        }
    }

    internal void ToggleDiplomacyMenu() {
        if(diplomacyCanvas.activeSelf) {
            CloseCenter();
        } else {
            OpenDiplomacyUI();
        }
    }
    public void OpenStructureUI(Structure str) {
        if (openStructure == str || str == null) {
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
        if (openStructure != null)
            UnselectStructure();

        str.RegisterOnDestroyCallback(OnStructureDestroy);
        openStructure = str;
        str.OpenExtraUI();

        if (str is ProductionStructure) {
            OpenProduktionUI((OutputStructure)str);
        }
        if (str is HomeStructure) {
            OpenHomeUI((HomeStructure)str);
        }
        if (str is MineStructure || str is FarmStructure) {
            OpenProduceUI((OutputStructure)str);
        }
        if (str is MarketStructure || str is WarehouseStructure) {
            OpenCityInventory(str.City);
        }
        if (str is MilitaryStructure) {
            OpenMilitaryStructureInfo(str);
        }
    }
    public void OnStructureDestroy(Structure str) {
        CloseInfoUI();
    }
    public void OpenDiplomacyUI () {
        CloseCenter();
        OpenCenter();
        diplomacyCanvas.SetActive(true);
    }

    public void OpenCenter() {
        centerParent.SetActive(true);
    }
    public void CloseCenter() {
        foreach(Transform t in centerParent.transform) {
            t.gameObject.SetActive(false);
        }
        centerParent.SetActive(false);
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
        productionStructureInfo.GetComponent<ProduktionUI>().Show(str);
    }
    public void CloseProduktionUI() {
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
        pauseMenu.Toggle();
        WorldController.Instance.IsPaused = PauseMenu.IsOpen;
    }
    public void OpenProduceUI(OutputStructure str) {
        if (str == null) {
            return;
        }
        OpenInfoUI();
        openStructure = str;
        productionStructureInfo.SetActive(true);
        productionStructureInfo.GetComponent<ProduktionUI>().Show(str);
        TileSpriteController.Instance.AddDecider(StrcutureTileDecider);
    }
    TileMark StrcutureTileDecider(Tile t) {
        if (openStructure != null && (openStructure.myRangeTiles.Contains(t) || openStructure.myStructureTiles.Contains(t))) {
            return TileMark.None;
        }
        else {
            return TileMark.Dark;
        }
    }
    public void CloseProduceUI() {
        TileSpriteController.Instance.RemoveDecider(StrcutureTileDecider);
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
                    OpenCityInventory(c, item => c.TradeWithShip(c.inventory.GetItemInInventoryClone(item)));
                }
            }
        }
        u.RegisterOnDestroyCallback(OnUnitDestroy);

        productionStructureInfo.SetActive(false);
        CloseInfoUI();
        OpenInfoUI();
        openUnit = u;
        unitCanvas.SetActive(true);
        unitCanvas.GetComponent<UnitUI>().Show(u);
    }
    public void OnUnitDestroy(Unit u) {
        CloseInfoUI();
    }
    public void CloseUnitUI() {
        unitCanvas.SetActive(false);
        CloseInfoUI();
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
    public void CloseUnitGroupUI() {
        unitGroupUI.SetActive(false);
        CloseInfoUI();
    }
    public void OpenInfoUI() {
        uiInfoCanvas.SetActive(true);
    }
    public void CloseInfoUI() {
        if (uiInfoCanvas.activeInHierarchy == false) {
            return;
        }
        if (openStructure != null) {
            TileSpriteController.Instance.RemoveDecider(StrcutureTileDecider);
            openStructure.CloseExtraUI();
            openStructure.UnregisterOnDestroyCallback(OnStructureDestroy);
            UnselectStructure();
        }
        if (openUnit != null) {
            openUnit.UnregisterOnDestroyCallback(OnUnitDestroy);
            openUnit = null;
        }
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
        if (openStructure != null)
            UnselectStructure();
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
        if (AnyMenuOpen() == false && dontOpenPause == false && MouseController.Instance.mouseState == MouseState.Idle) {
            TogglePauseMenu();
        }
        CloseCenter();
        CloseConsole();
        CloseRightUI();
        CloseInfoUI();
        CloseChooseBuild();

        if (openStructure != null)
            UnselectStructure();

        if (TileSpriteController.Instance != null)
            TileSpriteController.Instance.RemoveDecider(StrcutureTileDecider);
    }
    public void UnselectStructure() {
        if (openStructure != null)
            openStructure.CloseExtraUI();
        openStructure = null;
    }
    public bool IsPauseMenuOpen() {
        return PauseMenu.IsOpen;
    }
    public bool AnyMenuOpen() {
        return rightCanvas.activeSelf || uiInfoCanvas.activeSelf ||
            chooseBuildCanvas.activeSelf || tradeRouteCanvas.activeSelf ||
            consoleCanvas.activeSelf || centerParent.activeSelf;
    }

    public void SetDragAndDropBuild(GameObject go,Vector2 offset) {
        shortCutCanvas.GetComponent<ShortcutUI>().SetDragAndDropBuild(go,offset);
    }

    public void StopDragAndDropBuild() {
        shortCutCanvas.GetComponent<ShortcutUI>().StopDragAndDropBuild();
    }
    public void ToggleConsole() {
        consoleCanvas.SetActive(!consoleCanvas.activeSelf);
    }
    public void CloseConsole() {
        consoleCanvas.SetActive(false);
    }
    public static Sprite GetItemImageForID(string id) {
        if (ItemImages.ContainsKey(id) == false) {
            Debug.LogWarning("Item " + id + " is missing image!");
            return null;
        }
        return ItemImages[id];
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
    void OnDestroy() {
        Instance = null;
    }

    internal static void SetSaveUIData(UIControllerSave uics) {
        uIControllerSave = uics;
    }
}
[Serializable]
public class UIControllerSave : BaseSaveData {

    public Dictionary<int, string> shortcuts;

    public UIControllerSave(Dictionary<int,string> shortcuts) {
        this.shortcuts = shortcuts;
    }
    public UIControllerSave() {

    }
}
