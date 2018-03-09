using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIController : MonoBehaviour {

	public GameObject mainCanvas;
	public GameObject shortCutCanvas;

	public GameObject uiInfoCanvas;

	public GameObject chooseBuildCanvas;

	public GameObject consoleCanvas;
	public GameObject buildingCanvas;
	public GameObject unitCanvas;
	public GameObject rightCanvas;
	public GameObject CityInventoryCanvas;
	public GameObject citizenCanvas;
	public GameObject tradeMapCanvas;
	public GameObject pauseMenuCanvas;
	public GameObject offWorldMapCanvas;
	public GameObject otherCityUI;

	Structure openStructure;

	public static Dictionary<string,Sprite> ItemImages;
	public static string itemSpriteName = "item_";
	public static UIController Instance;

	void Start(){
		Escape (true);
		if(Instance!=null){
			Debug.LogError ("There are two uicontroller"); 
		}
		Instance = this;
		Sprite[] sprites = Resources.LoadAll<Sprite> ("Textures/Items/");
		Debug.Log (sprites.Length + " Item Sprite");
		ItemImages = new Dictionary<string, Sprite> ();
		foreach (Sprite item in sprites) {
			ItemImages [item.name] = item;
		}
	}

	public void OpenStructureUI(Structure str){
		if(openStructure == str || str == null){
			return;
		} 
		if(openStructure!=null) {
			openStructure.OnClickClose ();
		}			
		if(str.playerNumber != PlayerController.currentPlayerNumber){
			if (str is Warehouse) {
				OpenOtherCity(str.City);
				return;
			}
		}
		openStructure = str;
		str.OnClick ();
		if (str is ProductionBuilding) {
			OpenProduktionUI ((OutputStructure)str);
		}
		if (str is HomeBuilding) {
			OpenHomeUI ((HomeBuilding)str);
		}
		if(str is MineStructure || str is Farm ){
			OpenProduceUI ((OutputStructure)str);
		}
		if (str is MarketBuilding || str is Warehouse) {
			OpenCityInventory (str.City);
		}
	}
	public void OpenOtherCity(City city){
		if(city == null){
			return;
		}
		CloseRightUI ();
		rightCanvas.SetActive (true);
		otherCityUI.SetActive (true);
		otherCityUI.GetComponent<OtherCityUI> ().Show(city);
	}
	public void OpenCityInventory(City city, bool trade = false){
		if(city == null){
			return;
		}
		CloseRightUI ();
		rightCanvas.SetActive (true);
		CityInventoryCanvas.SetActive (true);
		CityInventoryCanvas.GetComponent<CityInventoryUI>().ShowInventory (city,trade);
	}
	public void HideCityUI (City city){
		otherCityUI.SetActive (false);
		CityInventoryCanvas.SetActive (false);
	}
	public void toggleRightUI(){
		rightCanvas.SetActive (!rightCanvas.activeSelf);
	}

	public void showBuildMenu(){
		chooseBuildCanvas.SetActive (!chooseBuildCanvas.activeSelf);
	}
	public void toggleInfoUI(){
		if(unitCanvas.activeSelf || buildingCanvas.activeSelf){
			uiInfoCanvas.SetActive (true);
		} else {
			uiInfoCanvas.SetActive (false);
		}
	}
	public void OpenProduktionUI(OutputStructure str){
		if(str == null){
			return;
		}
		unitCanvas.SetActive (false);
		buildingCanvas.SetActive (true);
		toggleInfoUI ();
		buildingCanvas.GetComponent<ProduktionUI>().Show (str);
	}
	public void CloseProduktionUI(){
		buildingCanvas.SetActive (false);
		CloseInfoUI ();
	}

	public void TogglePauseMenu(){
		pauseMenuCanvas.SetActive (!pauseMenuCanvas.activeSelf);
		WorldController.Instance.IsModal = pauseMenuCanvas.activeSelf;
	}

	public void OpenProduceUI(OutputStructure str){
		if(str == null){
			return;
		}
		unitCanvas.SetActive (false);
		buildingCanvas.SetActive (true);
		toggleInfoUI ();
		buildingCanvas.GetComponent<ProduktionUI>().Show (str);
		TileSpriteController.Instance.AddDecider (StrcutureTileDecider);
	}
	TileMark StrcutureTileDecider(Tile t){
		if(openStructure!=null&&openStructure.myRangeTiles.Contains(t)){
			return TileMark.None;
		} else {
			return TileMark.Dark; 
		}
	}
	public void CloseProduceUI(){
		TileSpriteController.Instance.removeDecider (StrcutureTileDecider);
		buildingCanvas.SetActive (false);
		CloseInfoUI ();
	}

	public void OpenUnitUI(Unit u){
		if(u==null){
			Debug.LogError ("Unit Script is null"); 
			return;
		}
		if (u.rangeUStructure != null) {
			if (u.rangeUStructure is Warehouse) {
				if (u.rangeUStructure.playerNumber == PlayerController.currentPlayerNumber) {
					CloseRightUI ();
					u.rangeUStructure.City.tradeUnit = u;
					OpenCityInventory (u.rangeUStructure.City, true);
				}
			}
		}
		buildingCanvas.SetActive (false);
		CloseInfoUI ();
		OpenInfoUI ();
		unitCanvas.SetActive (true);
		unitCanvas.GetComponent<UnitUI> ().Show (u);
	}

	public void CloseUnitUI(){
		unitCanvas.SetActive (false);
		CloseInfoUI ();
	}
	public void OpenInfoUI (){
		uiInfoCanvas.SetActive (true);
	}
	public void CloseInfoUI (){
		if(uiInfoCanvas.activeSelf == false){
			return;
		}		
		if(openStructure != null){
			TileSpriteController.Instance.removeDecider (StrcutureTileDecider);
			openStructure.OnClickClose ();
			openStructure = null;
		}
		unitCanvas.SetActive (false);
		buildingCanvas.SetActive (false);
		uiInfoCanvas.SetActive (false);
	}
	public void CloseChooseBuild(){
		chooseBuildCanvas.SetActive (false);
	}
	public void CloseRightUI(){
		if(openStructure != null){
			openStructure.OnClickClose ();
			openStructure = null;
		}
		otherCityUI.SetActive (false);
		CityInventoryCanvas.SetActive (false);
		citizenCanvas.SetActive (false);
		rightCanvas.SetActive (false);
	}

	public void OpenHomeUI(HomeBuilding hb){
		CloseRightUI ();
		rightCanvas.SetActive (true);
		citizenCanvas.SetActive (true);
		citizenCanvas.GetComponent<NeedsUIController>().Show (hb);

	}
	public void CloseHomeUI(){
		CloseRightUI ();
	}
	public void OpenTradeMenu(){
		CloseChooseBuild ();
		if(tradeMapCanvas.activeSelf){
			return;
		}
		offWorldMapCanvas.SetActive (false);
		tradeMapCanvas.SetActive (true);
		tradeMapCanvas.GetComponent<MapImage> ().Show();
	}
	public void ToggleOffWorldMenu(){
		if(offWorldMapCanvas.activeSelf){
			CloseOffWorldMenu ();
		}else {
			OpenOffWorldMenu ();
		}
	}
	public void CloseOffWorldMenu(){
		offWorldMapCanvas.SetActive (false);
	}
	public void OpenOffWorldMenu(){
		tradeMapCanvas.SetActive (false);
		offWorldMapCanvas.SetActive (true);
	}
	public void ToggleTradeMenu(){
		if(tradeMapCanvas.activeSelf){
			CloseTradeMenu ();
		}else {
			OpenTradeMenu ();
		}
	}
	public void CloseTradeMenu(){
		tradeMapCanvas.SetActive (false);
	}
	public void Escape(bool dontOpenPause=false) {
		if(AnyMenuOpen ()==false&&dontOpenPause==false&&MouseController.Instance.mouseState==MouseState.Idle){
			TogglePauseMenu ();
		}
		CloseConsole ();
		CloseHomeUI ();
		CloseInfoUI ();
		CloseProduktionUI ();
		CloseUnitUI ();
		CloseChooseBuild ();
		CloseRightUI ();
		CloseTradeMenu ();
		CloseOffWorldMenu ();
		if (TileSpriteController.Instance != null)
			TileSpriteController.Instance.removeDecider (StrcutureTileDecider);
	}
	public bool IsPauseMenuOpen(){
		return pauseMenuCanvas.activeSelf;
	}
	public bool AnyMenuOpen(){
		return rightCanvas.activeSelf || uiInfoCanvas.activeSelf || 
			chooseBuildCanvas.activeSelf || tradeMapCanvas.activeSelf || 
			consoleCanvas.activeSelf;
	}

	public void SetDragAndDropBuild(GameObject go){
		shortCutCanvas.GetComponent<ShortcutUI> ().SetDragAndDropBuild (go);
	}

	public void StopDragAndDropBuild(){
		shortCutCanvas.GetComponent<ShortcutUI> ().StopDragAndDropBuild ();
	}
	public void ToggleConsole(){
		consoleCanvas.SetActive (!consoleCanvas.activeSelf);
	}
	public void CloseConsole(){
		consoleCanvas.SetActive (false);
	}
	public static Sprite GetItemImageForID(int id){
		if(ItemImages.ContainsKey(itemSpriteName + id) == false){
			Debug.LogWarning ("Item " + id + " is missing image!");
			return null;
		}
		return ItemImages [itemSpriteName + id];
	}

	public static bool IsTextFieldFocused(){
		if(EventSystem.current.currentSelectedGameObject==null || EventSystem.current.currentSelectedGameObject.GetComponent<InputField>() ==null){
			return false;
		}
		return (EventSystem.current.currentSelectedGameObject.GetComponent<InputField>()).isFocused;
	}

}
