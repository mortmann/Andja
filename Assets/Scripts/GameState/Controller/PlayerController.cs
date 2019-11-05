using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine.SceneManagement;

public enum DiplomacyType { War, Neutral, TradeAggrement, Alliance }

/// <summary>
/// Player controller.
/// this is mostly for the currentplayer
/// but it updates the money for all
/// </summary>
public class PlayerController : MonoBehaviour {
    public static int currentPlayerNumber;
    readonly int piratePlayerNumber = int.MaxValue; // so it isnt the same like the number of wilderness
    public Player CurrPlayer { get { return Players[currentPlayerNumber]; } }
    List<DiplomaticStatus> playerDiplomaticStatus;
    PlayerPrototypeData PlayerPrototypeData => PrototypController.CurrentPlayerPrototypData;

    float BalanceFullTime => PlayerPrototypeData.BalanceFullTime;
    float BalanceTicksTime => PlayerPrototypeData.BalanceTicksTime;

    float balanceTickTimer;

    public static PlayerController Instance { get; protected set; }
    public static List<Player> Players { get; protected set; }
    public static int PlayerCount => Players.Count;

    public static Player CurrentPlayer;

    EventUIManager euim;
    /// <summary>
    /// FIRST&SECOND Player OldType -> NewType
    /// </summary>
    Action<Player, Player, DiplomacyType, DiplomacyType> cbDiplomaticChangePlayer;
    // Use this for initialization
    void Awake() {
        if (Instance != null) {
            Debug.LogError("There should never be two mouse controllers.");
        }
        Instance = this;

    }
    private void Start() {
        BuildController.Instance.RegisterCityCreated(OnCityCreated);
        BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
        GameObject.FindObjectOfType<EventController>().RegisterOnEvent(OnEventCreated, OnEventEnded);
        SceneManager.sceneLoaded += OnLevelLoad;
        if (SaveController.IsLoadingSave == false)
            Setup();
        if(playerDiplomaticStatus==null) {//only when changing stuff
            playerDiplomaticStatus = new List<DiplomaticStatus>();
            for (int i = 0; i < PlayerCount; i++) {
                for (int s = i + 1; s < PlayerCount; s++) {
                    playerDiplomaticStatus.Add(new DiplomaticStatus(i, s));
                }
            }
            ChangeDiplomaticStanding(0, 1, DiplomacyType.War);
            ChangeDiplomaticStanding(0, 2, DiplomacyType.TradeAggrement);

        }

    }

    internal DiplomacyType GetDiplomaticStatusType(Player firstPlayer, Player secondPlayer) {
        if (firstPlayer == secondPlayer) // ONLY BE NEUTRAL TO ONESELF :)
            return DiplomacyType.Neutral;
        return GetDiplomaticStatus(firstPlayer.Number, secondPlayer.Number).currentStatus; 
    }

    public void Setup() {
        Players = new List<Player>();
        currentPlayerNumber = 0;
        Player p = new Player(currentPlayerNumber,true);
        Players.Add(p);
        Players.Add(new Player(1, false));
        Players.Add(new Player(2, false));
        playerDiplomaticStatus = new List<DiplomaticStatus>();
        for(int i = 0; i < PlayerCount; i++) {
            for (int s = i+1; s < PlayerCount; s++) {
                playerDiplomaticStatus.Add(new DiplomaticStatus(i, s));
            }
        }
        ChangeDiplomaticStanding(0, 1, DiplomacyType.War);
        ChangeDiplomaticStanding(0, 2, DiplomacyType.TradeAggrement);
        CurrentPlayer = Players.Find(x => x.Number == currentPlayerNumber);

        balanceTickTimer = BalanceTicksTime;
    }

    internal bool HasEnoughMoney(int playerNumber, int buildCost) {
        if (playerNumber < 0 || playerNumber >= Players.Count) {
            Debug.LogError("The given number was too large or negative! No such player! " + playerNumber);
            return false;
        }
        return Players[playerNumber].HasEnoughMoney(buildCost);
    }

    // Update is called once per frame
    void Update() {
        if (WorldController.Instance.IsPaused)
            return;
        balanceTickTimer -= WorldController.Instance.DeltaTime;
        if (balanceTickTimer <= 0) {
            foreach (Player p in Players) {
                if (p == null) {
                    continue;
                }
                p.UpdateBalance((BalanceFullTime / BalanceTicksTime));
            }
            balanceTickTimer = BalanceTicksTime;
        }
        if (Application.isEditor) {
            //ALLOW SWITCH OF playernumber in editor
            if (Input.GetKey(KeyCode.LeftShift)) {
                if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    currentPlayerNumber = 0;
                }
                if (Input.GetKeyDown(KeyCode.Alpha1)) {
                    currentPlayerNumber = 1;
                }
                if (Input.GetKeyDown(KeyCode.Alpha2)) {
                    currentPlayerNumber = 2;
                }
                if (Input.GetKeyDown(KeyCode.Alpha3)) {
                    currentPlayerNumber = 3;
                }
                if (Input.GetKeyDown(KeyCode.Alpha4)) {
                    currentPlayerNumber = 4;
                }
                if (Input.GetKeyDown(KeyCode.Alpha5)) {
                    currentPlayerNumber = 5;
                }
                if (Input.GetKeyDown(KeyCode.Alpha6)) {
                    currentPlayerNumber = 6;
                }
                if (Input.GetKeyDown(KeyCode.Alpha7)) {
                    currentPlayerNumber = 8;
                }
                if (Input.GetKeyDown(KeyCode.Alpha9)) {
                    currentPlayerNumber = 9;
                }
            }
        }
    }

    internal void IncreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
        DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
        if (ds.currentStatus == DiplomacyType.Alliance)
            return;
        ChangeDiplomaticStanding(playerOne.Number, playerTwo.Number, (DiplomacyType)((int)ds.currentStatus + 1));
    }

    internal void DecreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
        DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
        if (ds.currentStatus == DiplomacyType.War)
            return;
        ChangeDiplomaticStanding(playerOne.Number, playerTwo.Number, (DiplomacyType)((int)ds.currentStatus - 1));
    }

    internal void SendMoneyFromTo(Player currentPlayer, Player selectedPlayer, int amount) {
        if (CurrentPlayer.HasEnoughMoney(amount) == false)
            return;
        //TODO: Notify Player that he received gift or demand
        selectedPlayer.AddMoney(amount);
        currentPlayer.ReduceMoney(amount);
    }

    public void OnEventCreated(GameEvent ge) {
        if (ge.target == null) {
            euim.AddEVENT(ge.eventID, ge.Name, ge.position);
            InformAIaboutEvent(ge, true);
            return;
        }
        //if its a island check if the player needs to know about it
        //eg. if he has a city on it
        if (ge.target is Island) {
            foreach (City item in ((Island)ge.target).myCities) {
                if (item.playerNumber == currentPlayerNumber) {
                    euim.AddEVENT(ge.eventID, ge.Name, ge.position);
                }
                else {
                    InformAIaboutEvent(ge, true);
                }
            }
            return;
        }
        //is the target not owned by anyone and it is a structure 
        //then inform all... it could be global effect on type of structure
        //should be pretty rare
        if (ge.target.GetPlayerNumber() < 0 && ge.target is Structure) {
            euim.AddEVENT(ge.eventID, ge.Name, ge.position);
            InformAIaboutEvent(ge, true);
        }
        //just check if the target is owned by the player
        if (ge.target.GetPlayerNumber() == currentPlayerNumber) {
            euim.AddEVENT(ge.eventID, ge.Name, ge.position);
        }
        else {
            InformAIaboutEvent(ge, true);
        }
    }

    private void OnLevelLoad(Scene scene, LoadSceneMode arg1) {
        if(scene.name != "GameState") {
            Debug.LogWarning("OnLevelLoad wrong scene!");
            return;
        }
        SceneManager.sceneLoaded -= OnLevelLoad;
        euim = GameObject.FindObjectOfType<EventUIManager>();
    }


    internal Player GetRandomPlayer() {
        List<Player> players = new List<Player>(Players);
        players.RemoveAll(p => p.HasLost);
        return players[UnityEngine.Random.Range(0, players.Count)];
    }

    internal void SetPlayerData(PlayerControllerSave pcs) {
        Players = pcs.players;
        foreach (Player p in Players)
            p.Load();
        playerDiplomaticStatus = pcs.playerWars;
        balanceTickTimer = pcs.tickTimer;
        currentPlayerNumber = pcs.currentPlayerNumber;
        CurrentPlayer = Players.Find(x => x.Number == currentPlayerNumber);
    }

    public void OnEventEnded(GameEvent ge) {
        //MAYBE REMOVE the message from the ui?
        //else inform the ai again
    }
    /// <summary>
    /// NOT IMPLEMENTED YET
    /// </summary>
    /// <param name="ge">Ge.</param>
    /// <param name="start">If set to <c>true</c> start.</param>
    public void InformAIaboutEvent(GameEvent ge, bool start) {
        //do something with it to inform the ai about 
    }


    public void ReduceMoney(int money, int playerNr) {
        Players[playerNr].ReduceMoney(money);
    }
    public void AddMoney(int money, int playerNr) {
        Players[playerNr].AddMoney(money);
    }
    public void ReduceChange(int amount, int playerNr) {
        Players[playerNr].ReduceChange(amount);
    }
    public void AddChange(int amount, int playerNr) {
        Players[playerNr].AddChange(amount);
    }
    public void OnCityCreated(City city) {
        Players[city.playerNumber].OnCityCreated(city);
    }
    public void OnStructureCreated(Structure structure, bool loading = false) {
        if (loading) {
            return; // getsloaded in so no need to subtract any money
        }
        ReduceMoney(structure.BuildCost, structure.PlayerNumber);
    }
    public bool ArePlayersAtWar(int playerOne, int playerTwo) {
        if (playerOne == playerTwo) {
            return false; // LUL same player cant attack himself
        }
        if (playerOne == piratePlayerNumber || playerTwo == piratePlayerNumber) {
            return true;//could add here be at peace with pirates through money 
        }
        return GetDiplomaticStatus(playerOne, playerTwo).currentStatus == DiplomacyType.War;
    }
    

    public void ChangeDiplomaticStanding(int playerOne, int playerTwo, DiplomacyType changeTo) {
        if (playerOne == playerTwo) {
            return; 
        }
        DiplomaticStatus ds = GetDiplomaticStatus( playerOne, playerTwo );
        if(ds.currentStatus == changeTo) {
            return;
        }
        cbDiplomaticChangePlayer?.Invoke(GetPlayer(playerOne), GetPlayer(playerTwo), ds.currentStatus, changeTo);
        ds.currentStatus = changeTo;
    }
    public DiplomaticStatus GetDiplomaticStatus(Player playerOne, Player playerTwo) {
        return GetDiplomaticStatus(playerOne.Number, playerTwo.Number);
    }
    public DiplomaticStatus GetDiplomaticStatus(int playerOne, int playerTwo) {
        return playerDiplomaticStatus.Find(x => x == new DiplomaticStatus(playerOne,playerTwo));
    }
    public static Player GetPlayer(int i) {
        if (i < 0 || Players.Count <= i) {
            Debug.LogError("PlayerNumber " + i + " does not exist!");
            return null;
        }
        return Players[i];
    }

    public PlayerControllerSave GetSavePlayerData() {
        return new PlayerControllerSave(currentPlayerNumber, balanceTickTimer, Players, playerDiplomaticStatus);
    }

    void OnDestroy() {
        Instance = null;
    }

    internal bool ChangeCurrentPlayer(int player) {
        if (PlayerController.PlayerCount <= player || player < 0)
            return false;
        currentPlayerNumber = player;
        return true;
    }
    public void RegisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc) {
        cbDiplomaticChangePlayer += callbackfunc;
    }

    public void UnregisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc) {
        cbDiplomaticChangePlayer -= callbackfunc;
    }

}
[Serializable]
public class PlayerControllerSave : BaseSaveData {

    public int currentPlayerNumber;
    public float tickTimer;
    public List<Player> players;
    public List<DiplomaticStatus> playerWars;

    public PlayerControllerSave(int cpn, float tickTimer, List<Player> players, List<DiplomaticStatus> playerWars) {
        currentPlayerNumber = cpn;
        this.players = players;
        this.tickTimer = tickTimer;
        this.playerWars = playerWars;
    }
    public PlayerControllerSave() {

    }
}

[Serializable]
public class DiplomaticStatus {
    public int playerOne;
    public int playerTwo;
    public DiplomacyType currentStatus;

    public DiplomaticStatus() {
    }

    public DiplomaticStatus(int one, int two) {
        if (one > two) {
            playerOne = two;
            playerTwo = one;
        }
        else {
            playerOne = one;
            playerTwo = two;
        }
        currentStatus = DiplomacyType.Neutral;
    }
    public override bool Equals(object obj) {
        // If parameter cannot be cast to War return false:
        DiplomaticStatus p = obj as DiplomaticStatus;
        if ((object)p == null) {
            return false;
        }
        // Return true if the fields match:
        return p == this;
    }

    public override int GetHashCode() {
        var hashCode = 971533886;
        hashCode = hashCode * -1521134295 + playerOne.GetHashCode();
        hashCode = hashCode * -1521134295 + playerTwo.GetHashCode();
        return hashCode;
    }

    public static bool operator ==(DiplomaticStatus a, DiplomaticStatus b) {
        // If both are null, or both are same instance, return true.
        if (System.Object.ReferenceEquals(a, b)) {
            return true;
        }

        // If one is null, but not both, return false.
        if (((object)a == null) || ((object)b == null)) {
            return false;
        }

        // Return true if the fields match:
        return a.playerOne == b.playerOne && a.playerTwo == b.playerTwo
            || a.playerTwo == b.playerOne && a.playerOne == b.playerTwo;
    }
    public static bool operator !=(DiplomaticStatus a, DiplomaticStatus b) {
        // If both are null, or both are same instance, return false.
        if (System.Object.ReferenceEquals(a, b)) {
            return false;
        }

        // If one is null, but not both, return true.
        if (((object)a == null) || ((object)b == null)) {
            return true;
        }

        // Return true if the fields not match:
        return a.playerOne != b.playerOne || a.playerTwo != b.playerTwo;
    }
}