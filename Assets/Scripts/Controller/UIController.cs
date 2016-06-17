using UnityEngine;
using System.Collections;

public class UIController : MonoBehaviour {

	public GameObject uiInfoCanvas;
	public GameObject chooseBuildCanvas;
	public GameObject buildingCanvas;
	public GameObject unitCanvas;
	public GameObject rightCanvas;
	public GameObject CityInventoryCanvas;

	public Structure oldStr;

	void Start(){
		CloseInfoUI ();
		chooseBuildCanvas.SetActive (false);
		buildingCanvas.SetActive (false);
		unitCanvas.SetActive (false);
	}

	public void OpenStructureUI(Structure str){
		if(oldStr == str || str == null){
			return;
		} 
		if(oldStr!=null) {
			oldStr.OnClickClose ();
		}
		oldStr = str;
		str.OnClick ();
		if (str is ProductionBuilding) {
			OpenProduktionUI ((UserStructure)str);
		}
		if(str is MineStructure || str is Farm ){
			OpenProduceUI ((UserStructure)str);
		}
		if (str is MarketBuilding || str is Warehouse) {
			OpenCityInventory (str.city);
		}
	}
	public void OpenCityInventory(City city, bool trade = false){
		if(city == null){
			return;
		}
		toggleRightUI ();
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
	public void OpenProduktionUI(UserStructure str){
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
	public void OpenProduceUI(UserStructure str){
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
			return;
		}
		if (u.rangeUStructure != null) {
			if (u.rangeUStructure is Warehouse) {
				CloseRightUI ();
				OpenCityInventory (u.rangeUStructure.city,true);
			}
		}
		buildingCanvas.SetActive (false);
		unitCanvas.SetActive (true);
		toggleInfoUI ();
		unitCanvas.GetComponent<UnitUI> ().Show (u);
	}

	public void CloseUnitUI(){
		unitCanvas.SetActive (false);
		CloseInfoUI ();
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
		CityInventoryCanvas.SetActive (false);
		rightCanvas.SetActive (false);
	}
	public void Escape() {
		CloseInfoUI ();
		CloseProduktionUI ();
		CloseUnitUI ();
		CloseChooseBuild ();
		CloseRightUI ();
	}


}
