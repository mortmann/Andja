using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

/// <summary>
/// Player controller.
/// this is mostly for the currentplayer
/// but it updates the money for all
/// </summary>
public class PlayerController : MonoBehaviour {
	public static int currentPlayerNumber;
	int piratePlayerNumber = int.MaxValue; // so it isnt the same like the number of wilderness
	public Player CurrPlayer{get {return players [currentPlayerNumber];}}
	HashSet<War> playerWars;
    const float BalanceFullTime = 60f;
    const float BalanceTicksTime = 4f;
	float balanceTickTimer;
	public static PlayerController Instance { get; protected set; }
	List<Player> players;
	EventUIManager euim;

    static PlayerControllerSave save;

    // Use this for initialization
    void OnEnable () {			
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
		playerWars = new HashSet<War> ();
		AddPlayersWar (0,1);
		AddPlayersWar (1,0);
		balanceTickTimer = BalanceTicksTime;
        BuildController.Instance.RegisterCityCreated (OnCityCreated);
        BuildController.Instance.RegisterStructureCreated (OnStructureCreated);
		euim = GameObject.FindObjectOfType<EventUIManager> ();
        GameObject.FindObjectOfType<EventController>().RegisterOnEvent (OnEventCreated, OnEventEnded);
        if(save != null) {
            LoadPlayerData();
            save = null;
        }
	}

    internal bool HasEnoughMoney(int playerNumber, int buildCost) {
        if(playerNumber<0 || playerNumber >= players.Count) {
            Debug.LogError("The given number was too large or negative! No such player! " + playerNumber);
            return false;
        }
        return players[playerNumber].HasEnoughMoney(buildCost);
    }

    // Update is called once per frame
    void Update () {
		balanceTickTimer -= Time.deltaTime;
		if(balanceTickTimer<=0){
			foreach(Player p in players){
				if(p == null){
					continue;
				}
				p.UpdateBalance ( (BalanceFullTime / BalanceTicksTime) );
			}
			balanceTickTimer = BalanceTicksTime;
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

    internal static void SetPlayerData(PlayerControllerSave pcs) {
        save = pcs;
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


	public void ReduceMoney(int money, int playerNr) {
        players[playerNr].ReduceMoney (money);
	}
	public void AddMoney(int money, int playerNr) {
		players [playerNr].AddMoney (money);
	}
	public void ReduceChange(int amount, int playerNr) {
		players [playerNr].ReduceChange (amount);
	}
	public void AddChange(int amount, int playerNr) {
		players [playerNr].AddChange (amount);
	}
	public void OnCityCreated(City city){
		players [city.playerNumber].OnCityCreated (city);
	}
	public void OnStructureCreated(Structure structure,bool loading = false){
		if(loading){
			return; // getsloaded in so no need to subtract any money
		}
		ReduceMoney (structure.Buildcost,structure.PlayerNumber);
	}
	public bool ArePlayersAtWar(int pnum1,int pnum2){
		if(pnum1 == pnum2){
			return false; // LUL same player cant attack himself
		}
		if(pnum1==piratePlayerNumber||pnum2==piratePlayerNumber){
			return true;//could add here be at peace with pirates through money 
		}
        return playerWars.Contains (new War(pnum1,pnum2));
	}
	public void AddPlayersWar(int pnum1,int pnum2){
		if(pnum1 == pnum2){
			return; // LUL same player cant attack himself
		}
		if(ArePlayersAtWar (pnum1,pnum2)){
			return; // already at war no need for same to be added
		}
		playerWars.Add (new War(pnum1,pnum2)); 
	}
	public void RemovePlayerWar(int pnum1, int pnum2){
		if(pnum1 == pnum2){
			return; // LUL same player cant attack himself
		}
		if(ArePlayersAtWar (pnum1,pnum2)==false){
			return; // they werent at war to begin with
		}
		playerWars.Remove (new War(pnum1,pnum2));
	}
	public Player GetPlayer(int i){
		if(i<0||players.Count <= i){
			return null;
		}
		return players [i];
	}

	public PlayerControllerSave GetSavePlayerData(){
		// Create/overwrite the save file with the xml text.
		return new PlayerControllerSave(currentPlayerNumber, balanceTickTimer, players,playerWars);
	}
	public void LoadPlayerData(){
		currentPlayerNumber = save.currentPlayerNumber;
		players = save.players;
		playerWars = save.playerWars;
		balanceTickTimer = save.tickTimer;
	}
    void OnDestroy() {
        Instance = null;
    }
}
[Serializable]
public class PlayerControllerSave {

	public int currentPlayerNumber;
	public float tickTimer;
	public List<Player> players;
	public HashSet<War> playerWars;

	public PlayerControllerSave(int cpn,float tickTimer,List<Player> players, HashSet<War> playerWars ){
		currentPlayerNumber = cpn;
		this.players = players;
		this.tickTimer = tickTimer;
		this.playerWars = playerWars;
	}
	public PlayerControllerSave(){

	}
}

[Serializable]
public class War {
	public int playerOne;
	public int playerTwo;

	public War(){
	}

	public War(int one, int two){
		if(one>two){
			playerOne = two;
			playerTwo = one;
		} else {
			playerOne = one;
			playerTwo = two;
		}
	}
	public override bool Equals (object obj) {
        // If parameter cannot be cast to War return false:
        War p = obj as War;
		if ((object)p == null){
			return false;
		}
		// Return true if the fields match:
		return p==this;
	}
	public override int GetHashCode(){
		return playerOne ^ playerTwo;
	}
	public static bool operator ==(War a, War b){
		// If both are null, or both are same instance, return true.
		if (System.Object.ReferenceEquals(a, b)){
			return true;
		}

		// If one is null, but not both, return false.
		if (((object)a == null) || ((object)b == null)){
			return false;
		}

		// Return true if the fields match:
		return a.playerOne == b.playerOne && a.playerTwo == b.playerTwo 
			|| a.playerTwo == b.playerOne && a.playerOne == b.playerTwo;
	}
	public static bool operator !=(War a, War b){
		// If both are null, or both are same instance, return false.
		if (System.Object.ReferenceEquals(a, b)){
			return false;
		}

		// If one is null, but not both, return true.
		if (((object)a == null) || ((object)b == null)){
			return true;
		}

		// Return true if the fields not match:
		return a.playerOne != b.playerOne || a.playerTwo != b.playerTwo;
	}
}