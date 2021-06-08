using Andja.Model;
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
    public class PlayerController : MonoBehaviour {
        public static int currentPlayerNumber;
        private List<DiplomaticStatus> playerDiplomaticStandings;
        PlayerPrototypeData PlayerPrototypeData => PrototypController.CurrentPlayerPrototypData;

        float BalanceFullTime => PlayerPrototypeData.BalanceFullTime;
        float BalanceTicksTime => PlayerPrototypeData.BalanceTicksTime;
        private float balanceTickTimer;

        public static PlayerController Instance { get; protected set; }
        public static List<Player> Players { get; protected set; }
        public static int PlayerCount => Players.Count;
        public static bool GameOver => CurrentPlayer.HasLost;

        public static Player CurrentPlayer;

        public Action<Player, Player> cbPlayerChange;

        private EventUIManager euim;

        /// <summary>
        /// FIRST&SECOND Player OldType -> NewType
        /// </summary>
        private Action<Player, Player, DiplomacyType, DiplomacyType> cbDiplomaticChangePlayer;

        // Use this for initialization
        private void Awake() {
            if (Instance != null) {
                Debug.LogError("There should never be two player controllers.");
            }
            Instance = this;
        }

        private void Start() {
            BuildController.Instance.RegisterCityCreated(OnCityCreated);
            BuildController.Instance.RegisterStructureCreated(OnStructureCreated);
            EventController.Instance.RegisterOnEvent(OnEventCreated, OnEventEnded);
            WorldController.Instance.RegisterWorldUnitCreated(OnUnitCreated);
            SceneManager.sceneLoaded += OnLevelLoad;
            if (SaveController.IsLoadingSave == false)
                NewGameSetup();
            Debug.Log("NewGameSetup");
        }

        internal DiplomacyType GetDiplomaticStatusType(Player firstPlayer, Player secondPlayer) {
            if (firstPlayer == secondPlayer) // ONLY BE ALLIED TO ONESELF :)
                return DiplomacyType.Alliance;
            return GetDiplomaticStatus(firstPlayer.Number, secondPlayer.Number).currentStatus;
        }

        internal static string GetPlayerName(int playerNumber) {
            if (playerNumber < 0 || Players.Count <= playerNumber) {
                if(playerNumber == GameData.PirateNumber) {
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
            return GetPlayer(playerNumber).Name;
        }

        public void NewGameSetup() {
            Players = new List<Player>();
            currentPlayerNumber = 0;
            Player p = new Player(currentPlayerNumber, true, GameData.Instance.Loadout.Money);
            Players.Add(p);
            p.RegisterHasLost(OnPlayerLost);
            Players.Add(new Player(1, false, GameData.Instance.Loadout.Money));
            Players.Add(new Player(2, false, GameData.Instance.Loadout.Money));
            playerDiplomaticStandings = new List<DiplomaticStatus>();
            for (int i = 0; i < PlayerCount; i++) {
                for (int s = i + 1; s < PlayerCount; s++) {
                    playerDiplomaticStandings.Add(new DiplomaticStatus(i, s));
                }
            }
            //ChangeDiplomaticStanding(0, 1, DiplomacyType.War);
            //ChangeDiplomaticStanding(0, 2, DiplomacyType.TradeAggrement);
            CurrentPlayer = Players.Find(x => x.Number == currentPlayerNumber);

            balanceTickTimer = BalanceTicksTime;
        }

        private void OnPlayerLost(Player player) {
            Debug.Log("Player " + player.Number + " " + player.Name + " has lost!");
            if (player.IsHuman && player.IsCurrent()) {
                Debug.Log("HUMAN YOU HAVE LOST THE GAME -- WE WILL DOMINATE ");
                UIController.Instance.ShowEndScoreScreen(); //TODO: show endscore
            }
            playerDiplomaticStandings.RemoveAll(x => x.PlayerOne == player.Number || x.PlayerTwo == player.Number);
            List<Unit> units = new List<Unit>(player.Units);
            for (int i = units.Count - 1; i >= 0; i--) {
                units[i].Destroy(null);
            }
            List<Structure> marketbuildings = new List<Structure>(player.AllStructures.Where(x => x is MarketStructure));
            foreach (Structure s in marketbuildings) {
                s.Destroy();
            }
        }

        internal bool IsAtWar(Player player) {
            return playerDiplomaticStandings.Exists(
                x => (x.PlayerOne == player.Number || x.PlayerTwo == player.Number) && x.currentStatus == DiplomacyType.War
            );
        }

        internal bool HasEnoughMoney(int playerNumber, int buildCost) {
            if (playerNumber < 0 || playerNumber >= Players.Count) {
                Debug.LogError("The given number was too large or negative! No such player! " + playerNumber);
                return false;
            }
            return Players[playerNumber].HasEnoughMoney(buildCost);
        }

        /// <summary>
        /// Update the balance for all players.
        /// </summary>
        private void Update() {
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
            }
        }
        /// <summary>
        /// Needs to check if Players are willing.
        /// Call only playerOne being the sending one. So that only playerTwo has to accept.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        internal void IncreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
            DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
            if (ds.currentStatus == DiplomacyType.Alliance) 
                return;
            if(playerTwo.AskDiplomaticIncrease(playerOne)) {
                ChangeDiplomaticStanding(playerOne.Number, playerTwo.Number, (DiplomacyType)((int)ds.currentStatus + 1));
            }
        }

        internal List<Player> GetPlayers() {
            return new List<Player>(Players);
        }
        /// <summary>
        /// Decreases the standing *always* maybe disable it in the future for missions orso.
        /// But even still make one the one that sends it and two the one receiving
        /// If the new is WAR -> change for all allies aswell. Defending call always first.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        internal void DecreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
            DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
            if (ds.currentStatus == DiplomacyType.War)
                return;
            ChangeDiplomaticStanding(playerOne.Number, playerTwo.Number, (DiplomacyType)((int)ds.currentStatus - 1));
            if (ds.currentStatus == DiplomacyType.War) {
                List<DiplomaticStatus> allies = GetAlliesFor(playerOne.Number);
                foreach (DiplomaticStatus ads in allies) {
                    if (ads.PlayerOne == playerOne.Number) {
                        ChangeDiplomaticStanding(ads.PlayerTwo, playerTwo.Number, DiplomacyType.War);
                    }
                    if (ads.PlayerTwo == playerOne.Number) {
                        ChangeDiplomaticStanding(ads.PlayerOne, playerTwo.Number, DiplomacyType.War);
                    }
                }
                allies = GetAlliesFor(playerTwo.Number);
                foreach (DiplomaticStatus ads in allies) {
                    if (ads.PlayerOne == playerTwo.Number) {
                        ChangeDiplomaticStanding(playerOne.Number, ads.PlayerTwo, DiplomacyType.War);
                    }
                    if (ads.PlayerTwo == playerTwo.Number) {
                        ChangeDiplomaticStanding(playerOne.Number, ads.PlayerOne, DiplomacyType.War);
                    }
                }
            }
        }
        /// <summary>
        /// sendPlayer sends money. receivingPlayer cannot decline.
        /// </summary>
        /// <param name="sendPlayer"></param>
        /// <param name="receivingPlayer"></param>
        /// <param name="amount"></param>
        internal void SendMoneyFromTo(Player sendPlayer, Player receivingPlayer, int amount) {
            if (CurrentPlayer.HasEnoughMoney(amount) == false)
                return;
            //TODO: Notify Player that he received gift or demand
            receivingPlayer.AddToTreasure(amount);
            sendPlayer.ReduceTreasure(amount);
        }

        public void OnEventCreated(GameEvent ge) {
            if (ge.target == null) {
                euim.AddEvent(ge);
                InformAIaboutEvent(ge, true);
                return;
            }
            //if its a island check if the player needs to know about it
            //eg. if he has a city on it
            if (ge.target is Island) {
                foreach (City item in ((Island)ge.target).Cities) {
                    if (item.PlayerNumber == currentPlayerNumber) {
                        euim.AddEvent(ge);
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
                euim.AddEvent(ge);
                InformAIaboutEvent(ge, true);
            }
            //just check if the target is owned by the player
            if (ge.target.GetPlayerNumber() == currentPlayerNumber) {
                euim.AddEvent(ge);
            }
            else {
                InformAIaboutEvent(ge, true);
            }
        }

        internal void AfterWorldLoad() {
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

        private void OnLevelLoad(Scene scene, LoadSceneMode arg1) {
            if (scene.name != "GameState") {
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
            playerDiplomaticStandings = pcs.playerDiplomaticStandings;
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

        public void OnCityCreated(City city) {
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
            DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
            if (ds == null)
                Debug.LogError("Missing DiplomaticStatus for " + playerOne + " " + playerTwo);
            return ds.currentStatus == DiplomacyType.War;
        }

        private List<DiplomaticStatus> GetAlliesFor(int player) {
            return playerDiplomaticStandings.FindAll(x => x.currentStatus == DiplomacyType.Alliance && (x.PlayerOne == player || x.PlayerTwo == player)).ToList();
        }

        public List<int> GetPlayersWithRelationTypeFor(int player, params DiplomacyType[] type) {
            //First find all where this player is in && has the status
            //then select only the int that isnt the player
            return playerDiplomaticStandings
                .FindAll(x =>(x.PlayerOne == player || x.PlayerTwo == player) && type.Contains(x.currentStatus))
                .Select(x => player == x.PlayerOne ? x.PlayerTwo : x.PlayerOne).ToList();
        }

        /// <summary>
        /// Immediate change no checks here.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        /// <param name="changeTo"></param>
        public void ChangeDiplomaticStanding(int playerOne, int playerTwo, DiplomacyType changeTo) {
            if (playerOne == playerTwo) {
                return;
            }
            DiplomaticStatus ds = GetDiplomaticStatus(playerOne, playerTwo);
            if (ds.currentStatus == changeTo) {
                return;
            }
            cbDiplomaticChangePlayer?.Invoke(GetPlayer(playerOne), GetPlayer(playerTwo), ds.currentStatus, changeTo);
            ds.currentStatus = changeTo;
            EventUIManager.Instance.Show(BasicInformation.DiplomacyChanged(ds));
        }

        public DiplomaticStatus GetDiplomaticStatus(Player playerOne, Player playerTwo) {
            return GetDiplomaticStatus(playerOne.Number, playerTwo.Number);
        }

        public DiplomaticStatus GetDiplomaticStatus(int playerOne, int playerTwo) {
            return playerDiplomaticStandings.Find(x => x == new DiplomaticStatus(playerOne, playerTwo));
        }

        public static Player GetPlayer(int i) {
            if (i < 0 || Players.Count <= i) {
                //Debug.LogError("PlayerNumber " + i + " does not exist!");
                return null;
            }
            return Players[i];
        }

        public PlayerControllerSave GetSavePlayerData() {
            return new PlayerControllerSave(currentPlayerNumber, balanceTickTimer, Players, playerDiplomaticStandings);
        }

        private void OnDestroy() {
            Instance = null;
        }

        internal bool ChangeCurrentPlayer(int player) {
            if (PlayerController.PlayerCount <= player || player < 0)
                return false;
            currentPlayerNumber = player;
            Player newOne = Players.Find(x => x.Number == currentPlayerNumber);
            cbPlayerChange?.Invoke(CurrentPlayer, newOne);
            CurrentPlayer = newOne;
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

    [Serializable]
    public class DiplomaticStatus {
        /// <summary>
        /// One is always smaller than two
        /// </summary>
        public int PlayerOne;
        /// <summary>
        /// Two is always bigger than one
        /// </summary>
        public int PlayerTwo;
        public DiplomacyType currentStatus;

        public DiplomaticStatus() {
        }

        public DiplomaticStatus(int one, int two) {
            if (one > two) {
                PlayerOne = two;
                PlayerTwo = one;
            }
            else {
                PlayerOne = one;
                PlayerTwo = two;
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
            hashCode = hashCode * -1521134295 + PlayerOne.GetHashCode();
            hashCode = hashCode * -1521134295 + PlayerTwo.GetHashCode();
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
            return a.PlayerOne == b.PlayerOne && a.PlayerTwo == b.PlayerTwo
                || a.PlayerTwo == b.PlayerOne && a.PlayerOne == b.PlayerTwo;
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
            return a.PlayerOne != b.PlayerOne || a.PlayerTwo != b.PlayerTwo;
        }
    }
}