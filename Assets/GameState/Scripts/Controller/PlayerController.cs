using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Player controller.
/// this is mostly for the currentplayer
/// but it updates the money for all
/// </summary>
public class PlayerController : MonoBehaviour {
	public int currentPlayerNumber;
	public Player currPlayer{get {return players [currentPlayerNumber];}}
	float balanceTicks;
	float tickTimer;
	public static PlayerController Instance { get; protected set; }
	List<Player> players;
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
	}
	
	// Update is called once per frame
	void Update () {

		tickTimer -= Time.deltaTime;
		if(tickTimer<=0){
			foreach(Player p in players){
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
}
