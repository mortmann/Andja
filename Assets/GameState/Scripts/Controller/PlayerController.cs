using UnityEngine;
using System.Collections.Generic;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;

/// <summary>
/// Player controller.
/// this is mostly for the currentplayer
/// but it updates the money for all
/// </summary>
public class PlayerController : MonoBehaviour,IXmlSerializable {
	public int currentPlayerNumber;
	public Player currPlayer{get {return players [currentPlayerNumber];}}
	float balanceTicks;
	float tickTimer;
	public static PlayerController Instance { get; protected set; }
	List<Player> players;
	EventUIManager euim;


	// Use this for initialization
	void Awake () {
		if (Instance != null) {
			Debug.LogError("There should never be two mouse controllers.");
		}
		Instance = this;
		players = new List<Player> ();
		currentPlayerNumber = 0;
		Player p = new Player (currentPlayerNumber);
		players.Add (p); 
		players.Add (new Player(1)); 
		players.Add (new Player(2)); 

		balanceTicks = 5f;
		tickTimer = balanceTicks;
		GameObject.FindObjectOfType<BuildController>().RegisterCityCreated (OnCityCreated);
		GameObject.FindObjectOfType<BuildController>().RegisterStructureCreated (OnStructureCreated);
		euim = GameObject.FindObjectOfType<EventUIManager> ();
		GameObject.FindObjectOfType<EventController> ().RegisterOnEvent (OnEventCreated, OnEventEnded);
	}
	
	// Update is called once per frame
	void Update () {

		tickTimer -= Time.deltaTime;
		if(tickTimer<=0){
			foreach(Player p in players){
				if(p == null){
					continue;
				}
				p.Update ();
			}
			tickTimer = balanceTicks;
		}
		if(Application.isEditor){
			//ALLOW SWITCH OF playernumber in editor
			if(Input.GetKey (KeyCode.LeftShift)){
				if(Input.GetKeyDown (KeyCode.Alpha0)){
					currentPlayerNumber = 0;
				}  
				if(Input.GetKeyDown (KeyCode.Alpha1)){
					currentPlayerNumber = 1;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha2)){
					currentPlayerNumber = 2;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha3)){
					currentPlayerNumber = 3;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha4)){
					currentPlayerNumber = 4;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha5)){
					currentPlayerNumber = 5;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha6)){
					currentPlayerNumber = 6;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha7)){
					currentPlayerNumber = 8;
				} 
				if(Input.GetKeyDown (KeyCode.Alpha9)){
					currentPlayerNumber = 9;
				} 
			}
		}
	}
	public void OnEventCreated(GameEvent ge){
		if(ge.target == null){
			euim.AddEVENT (ge.id,ge.name,ge.position);
			InformAIaboutEvent (ge, true);
			return;
		}
		//if its a island check if the player needs to know about it
		//eg. if he has a city on it
		if(ge.target is Island){
			foreach (City item in ((Island)ge.target).myCities) {
				if(item.playerNumber == currentPlayerNumber){
					euim.AddEVENT (ge.id,ge.name,ge.position);
				} else {
					InformAIaboutEvent (ge, true);
				}
			}
			return;
		}
		//is the target not owned by anyone and it is a structure 
		//then inform all... it could be global effect on type of structure
		//should be pretty rare
		if(ge.target.GetPlayerNumber()<0&&ge.target is Structure){
			euim.AddEVENT (ge.id,ge.name,ge.position);
			InformAIaboutEvent (ge, true);
		}
		//just check if the target is owned by the player
		if(ge.target.GetPlayerNumber() == currentPlayerNumber){
			euim.AddEVENT (ge.id,ge.name,ge.position);
		} else {
			InformAIaboutEvent (ge, true);
		}
	}
	public void OnEventEnded(GameEvent ge){
		//MAYBE REMOVE the message from the ui?
		//else inform the ai again
	}
	/// <summary>
	/// NOT IMPLEMENTED YET
	/// </summary>
	/// <param name="ge">Ge.</param>
	/// <param name="start">If set to <c>true</c> start.</param>
	public void InformAIaboutEvent(GameEvent ge,bool start){
		//do something with it to inform the ai about 
	}


	public void reduceMoney(int money, int playerNr) {
		players[playerNr].reduceMoney (money);
	}
	public void addMoney(int money, int playerNr) {
		players [playerNr].addMoney (money);
	}
	public void reduceChange(int amount, int playerNr) {
		players [playerNr].reduceChange (amount);
	}
	public void addChange(int amount, int playerNr) {
		players [playerNr].reduceChange (amount);
	}
	public void OnCityCreated(City city){
		players [city.playerNumber].OnCityCreated (city);
	}
	public void OnStructureCreated(Structure structure){
		reduceMoney (structure.buildcost,structure.playerID);
	}
	public bool ArePlayersAtWar(int pnum1,int pnum2){
		if(pnum1==-1||pnum2==-1){
			return true;//could add here be at peacce with pirates through money 
		}
		return players [pnum1].playersAtWarWith.Contains (pnum2);
	}

	public Player GetPlayer(int i){
		if(i<0){
			return null;
		}
		return players [i];
	}


	#region save
	public string GetPlayerSaveData(){
		XmlSerializer serializer = new XmlSerializer( typeof(PlayerController) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer, this);
		writer.Close();
		// Create/overwrite the save file with the xml text.
		return writer.ToString();
	}

	public XmlSchema GetSchema() {
		return null;
	}

	public void WriteXml(XmlWriter writer) {

		foreach(Player p in players){
			writer.WriteStartElement ("Player");
			p.WriteXml (writer);
			writer.WriteEndElement ();
		}

	}

	public void ReadXml(XmlReader reader) {

	}
	#endregion
}
