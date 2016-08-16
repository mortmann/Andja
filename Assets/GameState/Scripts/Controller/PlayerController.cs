using UnityEngine;
using System.Collections.Generic;
/// <summary>
/// Player controller.
/// this is mostly for the currentplayer
/// but it updates the money for all
/// </summary>
public class PlayerController : MonoBehaviour {
	public int currentPlayerNumber { get; protected set;}
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
		return players [pnum1].playersAtWarWith.Contains (pnum2);
	}

	public Player GetPlayer(int i){
		if(i<0){
			return null;
		}
		return players [i];
	}
}
