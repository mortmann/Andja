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
public class PlayerController : MonoBehaviour {
	public int currentPlayerNumber;
	int piratePlayerNumber = int.MaxValue; // so it isnt the same like the number of wilderness
	public Player currPlayer{get {return players [currentPlayerNumber];}}
	HashSet<KeyValuePair<int,int>> playerWars;
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
		Debug.Log (ArePlayersAtWar (0,1));
		Debug.Log (ArePlayersAtWar (1,2)); 
		playerWars = new HashSet<KeyValuePair<int, int>> ();
		playerWars.Add (new KeyValuePair<int, int>(0,1));
		playerWars.Add (new KeyValuePair<int, int>(0,2));

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
		if(pnum1==piratePlayerNumber||pnum2==piratePlayerNumber){
			return true;//could add here be at peace with pirates through money 
		}
		return playerWars.Contains (new KeyValuePair<int, int>(pnum1,pnum2));
	}

	public Player GetPlayer(int i){
		if(i<0){
			return null;
		}
		return players [i];
	}


	public string GetSavePlayerData(){
		XmlSerializer serializer = new XmlSerializer( typeof(PlayerControllerSave) );
		TextWriter writer = new StringWriter();
		serializer.Serialize(writer,new PlayerControllerSave(currentPlayerNumber, balanceTicks, tickTimer, players,playerWars));
		writer.Close();
		// Create/overwrite the save file with the xml text.
		return writer.ToString();
	}
	public void LoadPlayerData(string data){
		XmlSerializer serializer = new XmlSerializer( typeof(PlayerControllerSave) );
		TextReader reader = new StringReader( data );
		PlayerControllerSave pcs = (PlayerControllerSave)serializer.Deserialize(reader);
		reader.Close();

		currentPlayerNumber = pcs.currentPlayerNumber;
		players = pcs.players;
		tickTimer = pcs.tickTimer;
		balanceTicks = pcs.balanceTicks;
	}

	public class PlayerControllerSave : IXmlSerializable {
		
		public int currentPlayerNumber;
		public float balanceTicks;
		public float tickTimer;
		public List<Player> players;
		public HashSet<KeyValuePair<int,int>> playerWars;

		public PlayerControllerSave(int cpn,float balanceTicks,float tickTimer,List<Player> players, HashSet<KeyValuePair<int,int>> playerWars ){
			currentPlayerNumber = cpn;
			this.balanceTicks = balanceTicks;
			this.players = players;
			this.tickTimer = tickTimer;
			this.playerWars = playerWars;
		}
		public PlayerControllerSave(){
			
		}
		#region save
		public XmlSchema GetSchema() {
			return null;
		}

		public void WriteXml(XmlWriter writer) {
			writer.WriteStartElement ("Players");
			foreach(Player p in players){
				writer.WriteStartElement ("Player");
				writer.WriteAttributeString ("ID", p.GetPlayerNumber() + "");
				p.WriteXml (writer);
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
			writer.WriteStartElement ("Wars");
			foreach(KeyValuePair<int,int> pnum in playerWars){
				writer.WriteStartElement ("War");
				writer.WriteElementString ("Player Nr1", pnum.Key+"");
				writer.WriteElementString ("Player Nr2", pnum.Value+"");
				writer.WriteEndElement ();
			}
			writer.WriteEndElement ();
		}

		public void ReadXml(XmlReader reader) {
			if(reader.ReadToDescendant("Player") ) {
				do {
					if(reader.IsStartElement ("Player")==false){
						if(reader.Name == "Players"){
							break;
						}
						continue;
					}	
					int id = int.Parse( reader.GetAttribute("ID") );
					Player p = new Player(id);
					p.ReadXml (reader);
					players.Add(p);
				} while( reader.Read () );
				do {
					if(reader.IsStartElement ("War")==false){
						if(reader.Name == "Wars"){
							break;
						}
						continue;
					}	
					int pnum1 = int.Parse( reader.GetAttribute("Player Nr1") );
					int pnum2 = int.Parse( reader.GetAttribute("Player Nr2") );
					playerWars.Add (new KeyValuePair<int, int>(pnum1,pnum2));
				} while( reader.Read () );
			}

		}
		#endregion
	}


}
