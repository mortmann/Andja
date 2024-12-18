using Andja.Model;
using Andja.UI;
using Andja.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Andja.Controller {

    public enum DiplomacyType { War, Neutral, TradeAgreement, Alliance }

    /// <summary>
    /// This is mostly for the currentplayer,
    /// but it updates the money and interactions for all
    /// </summary>
    public class PlayerController : MonoBehaviour, IPlayerController {
        public static int currentPlayerNumber;
        private List<DiplomaticStatus> _playerDiplomaticStandings;
        public PlayerPrototypeData PlayerPrototypeData => PrototypController.CurrentPlayerPrototypData;
        public float BalanceFullTime => PlayerPrototypeData.BalanceFullTime;
        public float BalanceTicksTime => PlayerPrototypeData.BalanceTicksTime;
        private float _balanceTickTimer;

        public static IPlayerController Instance { get; set; }
        public List<Player> Players { get; protected set; }
        public int PlayerCount => Players.Count;
        public bool GameOver => CurrentPlayer.HasLost;

        public static Player CurrentPlayer;

        public Action<Player, Player> cbPlayerChange { get; set; }

        /// <summary>
        /// FIRST&SECOND Player OldType -> NewType
        /// </summary>
        private Action<Player, Player, DiplomacyType, DiplomacyType> _cbDiplomaticChangePlayer;

        public void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two player controllers.");
            }
            Instance = this;
        }

        public void Start() {
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
            BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
            EventController.Instance.RegisterOnEvent(OnEventCreated, OnEventEnded);
            WorldController.Instance.RegisterWorldUnitCreated(OnUnitCreated);
            if (SaveController.IsLoadingSave == false)
                NewGameSetup();
        }

        public DiplomacyType GetDiplomaticStatusType(Player firstPlayer, Player secondPlayer) {
            return firstPlayer == secondPlayer ? DiplomacyType.Alliance : GetDiplomaticStatus(firstPlayer.Number, secondPlayer.Number).CurrentStatus;
        }

        public string GetPlayerName(int playerNumber) {
            if (playerNumber >= 0 && Players.Count > playerNumber) 
                return GetPlayer(playerNumber).Name;
            if (playerNumber == GameData.PirateNumber) {
                return UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.Pirate);
            }
            if (playerNumber == GameData.FlyingTraderNumber) {
                return UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.FlyingTrader);
            }
            if (playerNumber == GameData.WorldNumber) {
                return UILanguageController.Instance.GetStaticVariables(StaticLanguageVariables.World);
            }
            return null;
        }

        public void NewGameSetup() {
            Debug.Log("NewGameSetup");
            Players = new List<Player>();
            currentPlayerNumber = 0;
            Player p = new Player(currentPlayerNumber, true, GameData.Instance.Loadout.Money);
            Players.Add(p);
            p.RegisterHasLost(OnPlayerLost);
            Players.Add(new Player(1, false, GameData.Instance.Loadout.Money));
            Players.Add(new Player(2, false, GameData.Instance.Loadout.Money));
            _playerDiplomaticStandings = new List<DiplomaticStatus>();
            for (int i = 0; i < PlayerCount; i++) {
                for (int s = i + 1; s < PlayerCount; s++) {
                    _playerDiplomaticStandings.Add(new DiplomaticStatus(i, s));
                }
            }
            CurrentPlayer = Players.Find(x => x.Number == currentPlayerNumber);
            _balanceTickTimer = BalanceTicksTime;
        }

        private void OnPlayerLost(Player player) {
            Debug.Log("Player " + player.Number + " " + player.Name + " has lost!");
            if (player.IsHuman && player.IsCurrent()) {
                Debug.Log("HUMAN YOU HAVE LOST THE GAME -- WE WILL DOMINATE ");
                UIController.Instance.ShowEndScoreScreen(); //TODO: show endscore
            }
            _playerDiplomaticStandings.RemoveAll(x => x.PlayerNumberOne == player.Number || x.PlayerNumberTwo == player.Number);
            List<Unit> units = new List<Unit>(player.Units);
            for (int i = units.Count - 1; i >= 0; i--) {
                units[i].Destroy(null);
            }
            List<Structure> marketbuildings = new List<Structure>(player.AllStructures.Where(x => x is MarketStructure));
            foreach (Structure s in marketbuildings) {
                s.Destroy();
            }
        }

        public bool IsAtWar(Player player) {
            return _playerDiplomaticStandings.Exists(
                x => (x.PlayerNumberOne == player.Number || x.PlayerNumberTwo == player.Number) && x.AreAtWar()
            );
        }

        public bool HasEnoughMoney(int playerNumber, int buildCost) {
            if (playerNumber >= 0 && playerNumber < Players.Count)
                return Players[playerNumber].HasEnoughMoney(buildCost);
            Debug.LogError("The given number was too large or negative! No such player! " + playerNumber);
            return false;
        }

        /// <summary>
        /// Update the balance for all players.
        /// </summary>
        public void Update() {
            if (WorldController.Instance.IsPaused)
                return;
            _balanceTickTimer -= WorldController.Instance.DeltaTime;
            if (_balanceTickTimer <= 0) {
                Players.ForEach(p => p.UpdateBalance(BalanceFullTime / BalanceTicksTime));
                _balanceTickTimer = BalanceTicksTime;
            }
            //ALLOW SWITCH OF playernumber in editor
            if (Application.isEditor == false) return;
            if (Input.GetKey(KeyCode.LeftShift) == false) return;
            if (Input.GetKeyDown(KeyCode.Alpha0)) {
                ChangeCurrentPlayer(0);
            }
            if (Input.GetKeyDown(KeyCode.Alpha1)) {
                ChangeCurrentPlayer(1);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2)) {
                ChangeCurrentPlayer(2);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3)) {
                ChangeCurrentPlayer(3);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4)) {
                ChangeCurrentPlayer(4);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5)) {
                ChangeCurrentPlayer(5);
            }
            if (Input.GetKeyDown(KeyCode.Alpha6)) {
                ChangeCurrentPlayer(6);
            }
            if (Input.GetKeyDown(KeyCode.Alpha7)) {
                ChangeCurrentPlayer(7);
            }
            if (Input.GetKeyDown(KeyCode.Alpha9)) {
                ChangeCurrentPlayer(8);
            }
        }

        public void PraisePlayer(Player from, Player to) {
            if (to.Number == PlayerController.currentPlayerNumber) {
                EventUIManager.Instance.Show(BasicInformation.CreatePraiseReceived(from));
            }
            if (to.IsHuman == false) {
                to.AI.ReceivePraise(from);
            }
        }

        public void DenouncePlayer(Player from, Player to) {
            if (to.Number == PlayerController.currentPlayerNumber) {
                EventUIManager.Instance.Show(BasicInformation.CreateDenounceReceived(from));
            }
            if (to.IsHuman == false) {
                to.AI.ReceiveDenounce(from);
            }
        }

        public void TryToDemandMoney(Player demands, Player target, int amount) {
            if (target.Number == PlayerController.currentPlayerNumber) {
                EventUIManager.Instance.Show(ChoiceInformation.CreateMoneyDemand(demands, amount));
            }
            if (target.IsHuman == false) {
                target.AI.ReceiveDemandMoney(demands, amount);
            }
        }

        /// <summary>
        /// Needs to check if Players are willing.
        /// Call only playerOne being the sending one. So that only playerTwo has to accept.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        public void IncreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
            GetDiplomaticStatus(playerOne, playerTwo)?.TryIncrease();
        }

        public List<Player> GetPlayers() {
            return new List<Player>(Players);
        }
        /// <summary>
        /// Decreases the standing *always* maybe disable it in the future for missions orso.
        /// But even still make one the one that sends it and two the one receiving
        /// If the new is WAR -> change for all allies aswell. Defending call always first.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        public void DecreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
            GetDiplomaticStatus(playerOne, playerTwo)?.DecreaseDiplomaticStanding(playerOne, playerTwo);
        }
        /// <summary>
        /// sendPlayer sends money. receivingPlayer cannot decline.
        /// </summary>
        /// <param name="sendPlayer"></param>
        /// <param name="receivingPlayer"></param>
        /// <param name="amount"></param>
        public void SendMoneyFromTo(Player sendPlayer, Player receivingPlayer, int amount) {
            if (CurrentPlayer.HasEnoughMoney(amount) == false)
                return;
            receivingPlayer.AddToTreasure(amount);
            sendPlayer.ReduceTreasure(amount);
            if (receivingPlayer.Number == PlayerController.currentPlayerNumber) {
                EventUIManager.Instance.Show(BasicInformation.CreateMoneyReceived(sendPlayer, amount));
            }
            if (receivingPlayer.IsHuman == false) {
                receivingPlayer.AI.ReceivedMoney(sendPlayer, amount);
            }
        }

        public void OnEventCreated(GameEvent ge) {
            switch (ge.target) {
                case null:
                    EventUIManager.Instance.AddEvent(ge);
                    InformAIaboutEvent(ge, true);
                    return;
                //if its a island check if the player needs to know about it
                //eg. if he has a city on it
                case Island island: {
                    foreach (City item in island.Cities) {
                        if (item.PlayerNumber == currentPlayerNumber) {
                            EventUIManager.Instance.AddEvent(ge);
                        }
                        else {
                            InformAIaboutEvent(ge, true);
                        }
                    }
                    return;
                }
                case Structure _: {
                    //is the target not owned by anyone and it is a structure
                    //then inform all... it could be global effect on type of structure
                    //should be pretty rare
                    if (ge.target.GetPlayerNumber() >= 0) return;
                    EventUIManager.Instance.AddEvent(ge);
                    InformAIaboutEvent(ge, true);
                    return;
                }
                default:
                    Debug.LogWarning("Not implemented yet.");
                    break;
            }
            //just check if the target is owned by the player
            if (ge.target.GetPlayerNumber() == currentPlayerNumber) {
                EventUIManager.Instance.AddEvent(ge);
            }
            else {
                InformAIaboutEvent(ge, true);
            }
        }

        public void AfterWorldLoad() {
            foreach (Player p in Players) {
                p.Cities.ForEach(c => {
                    c.CalculateExpanses();
                    c.CalculateIncome();
                });
                p.CalculateBalance();
            }
        }

        private void OnUnitCreated(Unit unit) {
            if (unit.IsNonPlayer)
                return;
            Players[unit.PlayerNumber].OnUnitCreated(unit);
        }

        public Player GetRandomPlayer() {
            List<Player> players = new List<Player>(Players);
            players.RemoveAll(p => p.HasLost);
            return players[UnityEngine.Random.Range(0, players.Count)];
        }

        public void SetPlayerData(PlayerControllerSave pcs) {
            Players = pcs.players;
            foreach (Player p in Players)
                p.Load();
            _playerDiplomaticStandings = pcs.playerDiplomaticStandings;
            _balanceTickTimer = pcs.tickTimer;
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
            Players[playerNr].ReduceTreasure(money);
        }

        public void AddMoney(int money, int playerNr) {
            Players[playerNr].AddToTreasure(money);
        }

        public void ReduceChange(int amount, int playerNr) {
            Players[playerNr].ReduceTreasureChange(amount);
        }

        public void AddChange(int amount, int playerNr) {
            Players[playerNr].AddTreasureChange(amount);
        }

        public void OnCityCreated(ICity city) {
            Players[city.PlayerNumber].OnCityCreated(city);
        }

        public void OnStructureCreated(Structure structure, bool loading = false) {
            if (loading) {
                return;
            }
        }

        public bool ArePlayersAtWar(int playerOne, int playerTwo) {
            if (playerOne == playerTwo) {
                return false; // same player cant attack himself
            }
            if (playerOne == Pirate.Number || playerTwo == Pirate.Number) {
                return true;//could add here be at peace with pirates through money
            }
            if (playerOne == FlyingTrader.Number || playerTwo == FlyingTrader.Number) {
                return false;//No war with trader ships yet... maybe in the future
            }
            if (playerOne == GameData.WorldNumber || playerTwo == GameData.WorldNumber) {
                return false;//No war with world stuff
            }
            return GetDiplomaticStatus(playerOne, playerTwo)?.AreAtWar() == true;
        }

        public List<DiplomaticStatus> GetAlliesFor(int player) {
            return _playerDiplomaticStandings.FindAll(x => x.CurrentStatus == DiplomacyType.Alliance && (x.PlayerNumberOne == player || x.PlayerNumberTwo == player)).ToList();
        }

        public List<int> GetPlayersWithRelationTypeFor(int player, params DiplomacyType[] type) {
            //First find all where this player is in && has the status
            //then select only the int that isnt the player
            return _playerDiplomaticStandings
                .FindAll(x => (x.PlayerNumberOne == player || x.PlayerNumberTwo == player) && type.Contains(x.CurrentStatus))
                .Select(x => player == x.PlayerNumberOne ? x.PlayerNumberTwo : x.PlayerNumberOne).ToList();
        }

        /// <summary>
        /// Immediate change no checks here.
        /// </summary>
        /// <param name="playerNROne"></param>
        /// <param name="playerNRTwo"></param>
        /// <param name="changeTo"></param>
        public void ChangeDiplomaticStanding(int playerNROne, int playerNRTwo, DiplomacyType changeTo, bool force = false) {
            GetDiplomaticStatus(playerNROne, playerNRTwo)?.ChangeDiplomaticStanding(changeTo, force);
        }

        public DiplomaticStatus GetDiplomaticStatus(Player playerOne, Player playerTwo) {
            return GetDiplomaticStatus(playerOne.Number, playerTwo.Number);
        }

        public DiplomaticStatus GetDiplomaticStatus(int playerOne, int playerTwo) {
            return _playerDiplomaticStandings.Find(x => x == new DiplomaticStatus(playerOne, playerTwo));
        }

        public Player GetPlayer(int i) {
            if (i < 0 || Players.Count <= i) {
                //Debug.LogError("PlayerNumber " + i + " does not exist!");
                return null;
            }
            return Players[i];
        }

        public PlayerControllerSave GetSavePlayerData() {
            return new PlayerControllerSave(currentPlayerNumber, _balanceTickTimer, Players, _playerDiplomaticStandings);
        }

        public void OnDestroy() {
            Instance = null;
        }

        public bool ChangeCurrentPlayer(int player) {
            if (PlayerCount <= player || player < 0)
                return false;
            currentPlayerNumber = player;
            Player newOne = Players.Find(x => x.Number == currentPlayerNumber);
            cbPlayerChange?.Invoke(CurrentPlayer, newOne);
            CurrentPlayer = newOne;
            return true;
        }

        public void RegisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc) {
            _cbDiplomaticChangePlayer += callbackfunc;
        }

        public void UnregisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc) {
            _cbDiplomaticChangePlayer -= callbackfunc;
        }

        public void TriggerDiplomaticChangeCb(Player playerOne, Player playerTwo, DiplomacyType currentStatus, DiplomacyType changeTo) {
            _cbDiplomaticChangePlayer?.Invoke(playerOne, playerTwo, currentStatus, changeTo);
        }
    }

    [Serializable]
    public class PlayerControllerSave : BaseSaveData {
        public int currentPlayerNumber;
        public float tickTimer;
        public List<Player> players;
        public List<DiplomaticStatus> playerDiplomaticStandings;

        public PlayerControllerSave(int cpn, float tickTimer, List<Player> players, List<DiplomaticStatus> playerDiplomaticStandings) {
            currentPlayerNumber = cpn;
            this.players = players;
            this.tickTimer = tickTimer;
            this.playerDiplomaticStandings = playerDiplomaticStandings;
        }

        public PlayerControllerSave() {
        }
    }
}