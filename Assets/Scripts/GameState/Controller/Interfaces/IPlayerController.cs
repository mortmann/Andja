using Andja.Model;
using System;
using System.Collections.Generic;

namespace Andja.Controller {
    public interface IPlayerController {
        Action<Player, Player> cbPlayerChange { get; set; }
        List<Player> Players { get; }
        int PlayerCount { get; }
        bool GameOver { get; }
        void AddChange(int amount, int playerNr);
        void AddMoney(int money, int playerNr);
        void AfterWorldLoad();
        bool ArePlayersAtWar(int playerOne, int playerTwo);
        bool ChangeCurrentPlayer(int player);
        void ChangeDiplomaticStanding(int playerNROne, int playerNRTwo, DiplomacyType changeTo, bool force = false);
        void DecreaseDiplomaticStanding(Player playerOne, Player playerTwo);
        void DenouncePlayer(Player from, Player to);
        DiplomaticStatus GetDiplomaticStatus(int playerOne, int playerTwo);
        DiplomaticStatus GetDiplomaticStatus(Player playerOne, Player playerTwo);
        DiplomacyType GetDiplomaticStatusType(Player firstPlayer, Player secondPlayer);
        Player GetPlayer(int i);
        List<Player> GetPlayers();
        List<int> GetPlayersWithRelationTypeFor(int player, params DiplomacyType[] type);
        Player GetRandomPlayer();
        PlayerControllerSave GetSavePlayerData();
        bool HasEnoughMoney(int playerNumber, int buildCost);
        void IncreaseDiplomaticStanding(Player playerOne, Player playerTwo);
        void InformAIaboutEvent(GameEvent ge, bool start);
        bool IsAtWar(Player player);
        void NewGameSetup();
        void OnCityCreated(City city);
        void OnEventCreated(GameEvent ge);
        void OnEventEnded(GameEvent ge);
        void OnStructureCreated(Structure structure, bool loading = false);
        void PraisePlayer(Player from, Player to);
        void ReduceChange(int amount, int playerNr);
        void ReduceMoney(int money, int playerNr);
        void RegisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc);
        void SendMoneyFromTo(Player sendPlayer, Player receivingPlayer, int amount);
        void SetPlayerData(PlayerControllerSave pcs);
        void TryToDemandMoney(Player demands, Player target, int amount);
        void UnregisterPlayersDiplomacyStatusChange(Action<Player, Player, DiplomacyType, DiplomacyType> callbackfunc);
        string GetPlayerName(int playerNumber);
    }
}