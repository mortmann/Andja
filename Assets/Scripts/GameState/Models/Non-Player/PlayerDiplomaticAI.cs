using Andja.Controller;
using Andja.Model;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Andja.AI {

    [JsonObject(MemberSerialization.OptIn)]
    public class PlayerDiplomaticAI {
        private const float TRADEAGREEMEANT_STANDING_REQUIRED = 1.5f;
        private const float ALLIANCE_STANDING_REQUIRED = 2f;
        public readonly float TIME_PRAISE_COOLDOWN = 60 * 15;
        public readonly float GIVEN_MONEY_DECAY = 1f;
        public Player Player;
        //Temporary
        [JsonPropertyAttribute] private float _standing;
        public float Standing => _standing;

        [JsonPropertyAttribute] private float totalMoneyGiven;
        [JsonPropertyAttribute] private float givenMoneyRecently;
        [JsonPropertyAttribute] private float timeSinceLastPraise;

        public PlayerDiplomaticAI(Player player) {
            Player = player;
        }
        public PlayerDiplomaticAI() {
        }
        public void GotPraise() {
            timeSinceLastPraise = 0;
            //TODO: maybe sliding style
            if(timeSinceLastPraise > TIME_PRAISE_COOLDOWN) {
                _standing += 0.5f; //TODO: how much a praise is "valued" depends on the ai difficulty and "mentality"
            }
        }

        public void GotMoney(int amount, int totalOwning, int totalIncome) {
            totalMoneyGiven += amount;
            givenMoneyRecently += amount;
            _standing += Mathf.Clamp01(
                        (amount - givenMoneyRecently) / (totalOwning+ Mathf.Clamp(totalIncome * 4, 0, int.MaxValue))
                );
        }

        public void DecreasedDiplomaticStanding(DiplomaticStatus status) {
            switch (status.currentStatus) {
                case DiplomacyType.War:
                    _standing = -10;
                    break;
                case DiplomacyType.Neutral:
                    _standing -= 1;
                    break;
                case DiplomacyType.TradeAgreement:
                    _standing -= 2;
                    break;
                case DiplomacyType.Alliance:
                    break;
            }
        }

        public void Update(float deltaTime) {
            timeSinceLastPraise += deltaTime;
            givenMoneyRecently -= deltaTime * GIVEN_MONEY_DECAY;
        }

        internal void GotDenounce() {
            _standing -= 1f;
        }

        internal void GotDemandMoney(bool paid) {
            if(paid)
                _standing -= 1.3f;
            else
                _standing -= 0.3f;
        }

        internal bool AskDiplomaticIncrease(DiplomaticStatus status) {
            switch (status.currentStatus) {
                case DiplomacyType.War:
                    //Depends on different factors...
                    //like who started, who is stronger, what the goals may be
                    //for now gets handled in aiplayer directly
                    break;
                case DiplomacyType.Neutral:
                    return _standing > TRADEAGREEMEANT_STANDING_REQUIRED;
                case DiplomacyType.TradeAgreement:
                    return _standing > ALLIANCE_STANDING_REQUIRED;
                case DiplomacyType.Alliance:
                    //There is nothing after alliance.
                    return _standing > 100f;
            }
            return false;
        }

        internal void ForceDiplomaticIncrease(DiplomacyType changeTo) {
            switch (changeTo) {
                case DiplomacyType.War:
                    _standing = -10;
                    break;
                case DiplomacyType.Neutral:
                    _standing = 0;
                    break;
                case DiplomacyType.TradeAgreement:
                    _standing = TRADEAGREEMEANT_STANDING_REQUIRED;
                    break;
                case DiplomacyType.Alliance:
                    //There is nothing after alliance.
                    _standing = ALLIANCE_STANDING_REQUIRED;
                    break;
            }
        }
    }

}
