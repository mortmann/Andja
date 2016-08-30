using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour {

	public GameObject mainCanvas;
	public GameObject shortCutCanvas;

	public GameObject uiInfoCanvas;

	public GameObject chooseBuildCanvas;

	public GameObject buildingCanvas;
	public GameObject unitCanvas;
	public GameObject rightCanvas;
	public GameObject CityInventoryCanvas;
	public GameObject citizenCanvas;
	public GameObject tradeMapCanvas;
	public GameObject pauseMenuCanvas;
	public GameObject offWorldMapCanvas;
	public GameObject otherCityUI;

	Structure oldStr;



	public static UIController Instance;

	void Start(){
		Escape (true);
		if(Instance!=null){
			Debug.LogError ("There are two uicontroller"); 
		}
		Instance = this;
	}

	public void OpenStructureUI(Structure str){
		if(oldStr == str || str == null){
			return;
		} 
		if(oldStr!=null) {
			oldStr.OnClickClose ();
		}			
		if(str.playerID != PlayerController.Instance.currentPlayerNumber){
			if (str is Warehouse) {
				OpenOtherCity(str.City);
				return;
			}
		}
		oldStr = str;
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
		buildingCanvas.GetComponent<ProduktionUI>().ShowProduce (str);
	}
	public void CloseProduceUI(){
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
				if (u.rangeUStructure.playerID == PlayerController.Instance.currentPlayerNumber) {
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
		if(oldStr != null){
			oldStr.OnClickClose ();
			oldStr = null;
		}
		unitCanvas.SetActive (false);
		buildingCanvas.SetActive (false);
		uiInfoCanvas.SetActive (false);
	}
	public void CloseChooseBuild(){
		chooseBuildCanvas.SetActive (false);
	}
	public void CloseRightUI(){
		if(oldStr != null){
			oldStr.OnClickClose ();
			oldStr = null;
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
	public void Escape(bool firstreset=false) {
		if(AnyMenuOpen ()==false&&firstreset==false){
			TogglePauseMenu ();
		}
		CloseHomeUI ();
		CloseInfoUI ();
		CloseProduktionUI ();
		CloseUnitUI ();
		CloseChooseBuild ();
		CloseRightUI ();
		CloseTradeMenu ();
		CloseOffWorldMenu ();
	}
	public bool IsPauseMenuOpen(){
		return pauseMenuCanvas.activeSelf;
	}
	public bool AnyMenuOpen(){
		return rightCanvas.activeSelf || uiInfoCanvas.activeSelf || chooseBuildCanvas.activeSelf || tradeMapCanvas.activeSelf;
	}

	public void SetDragAndDropBuild(GameObject go){
		shortCutCanvas.GetComponent<ShortcutUI> ().SetDragAndDropBuild (go);
	}

	public void StopDragAndDropBuild(){
		Debug.Log ("EJMfkojsdoinfvoksjnvjösdn"); 
		shortCutCanvas.GetComponent<ShortcutUI> ().StopDragAndDropBuild ();
	}

}
