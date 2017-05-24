using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Player : IXmlSerializable,IGEventable {
	public const int TargetType = 1;

	Action<GameEvent> cbEventCreated;
	Action<GameEvent> cbEventEnded;
	List<City> myCities;
	public int change { get; protected set;} //should be calculated after reload anyway
	public int balance { get; protected set;} //also be calculated after reload
	// because only the new level popcount is interesting
	// needs to be saved because you can lose pop due
	// war or death and only the highest ever matters here 
	private int _maxPopulationLevel;
	public int maxPopulationLevel { 
		get {return _maxPopulationLevel;} 
		set { 
			if(maxPopulationLevel<value){
				return;
			}
			_maxPopulationLevel = value;
			maxPopulationCount = 0; 
		}
	} 									  
	public int maxPopulationCount { get; protected set;}
	public int playerNumber;

	public Player(int number){
		playerNumber = number;
		maxPopulationCount = 0;
		maxPopulationLevel = 0;
		myCities = new List<City> ();
		change = -10;
		balance = 50000;
	}
	public void Update () {

		int citychange=0;
		for (int i = 0; i < myCities.Count; i++) {
			citychange += myCities[i].cityBalance;
		}

		balance += change+citychange;

		if(balance < -1000000){
			// game over !
		}
	}

	public void reduceMoney(int money) {
		if (money < 0) {
			return;	
		}
		balance -= money;
	}
	public void addMoney(int money) {
		if (money < 0) {
			return;	
		}
		balance += money;
	}
	public void reduceChange(int amount) {
		if (amount < 0) {
			return;	
		}
		change -= amount;
	}
	public void addChange(int amount) {
		if (amount < 0) {
			return;	
		}
		change += amount;
	}
	public void OnCityCreated(City city){
		myCities.Add (city);

	}
	public void OnStructureCreated(Structure structure){
		reduceMoney (structure.buildcost);
		structure.RegisterOnDestroyCallback (OnStructureDestroy);
	}
	public void OnStructureDestroy(Structure structure){
		//dosmth
	}
	public void RegisterOnEvent(Action<GameEvent> create,Action<GameEvent> ending){
		
	}
	public int GetPlayerNumber(){
		return playerNumber;
	}
	public int GetTargetType(){
		return TargetType;
	}

	#region save
	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {
		writer.WriteElementString ("maxPopulationLevel",this.maxPopulationLevel+"");
		writer.WriteElementString ("maxPopulationCount",this.maxPopulationCount+"");
	}

	public void ReadXml(XmlReader reader) {
		maxPopulationCount = int.Parse( reader.GetAttribute("maxPopulationCount") );
		maxPopulationLevel = int.Parse( reader.GetAttribute("maxPopulationLevel") );

	}
	public void SaveIGE(XmlWriter writer){
		writer.WriteAttributeString("TargetType", TargetType +"" );
		writer.WriteAttributeString("PlayerNumber", playerNumber +"" );
	}
	#endregion


}
