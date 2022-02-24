using Andja.Controller;
using Andja.Model;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.AI {

    public class PlayerDiplomaticAI {
        public readonly float TIME_PRAISE_COOLDOWN = 60 * 15;
        public readonly float GIVEN_MONEY_DECAY = 1f;
        public Player Player;
        //Temporary
        private float _standing;
        public float Standing => _standing;

        private float totalMoneyGiven;
        private float givenMoneyRecently;
        private float timeSinceLastPraise;

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

        public bool AcceptStatus(DiplomaticStatus status) {
            switch (status.currentStatus) {
                case DiplomacyType.War:
                    //_standing = -1000000;
                    break;
                case DiplomacyType.Neutral:
                    //TODO: check combat value comparison here
                    break;
                case DiplomacyType.TradeAgreement:
                    return _standing > 1;
                case DiplomacyType.Alliance:
                    return _standing > 2;
            }
            return false;
        }

        public void DecreasedDiplomaticStanding(DiplomaticStatus status) {
            switch (status.currentStatus) {
                case DiplomacyType.War:
                    _standing = -1000000;
                    break;
                case DiplomacyType.Neutral:
                    _standing = 0;
                    break;
                case DiplomacyType.TradeAgreement:
                    _standing += 1;
                    break;
                case DiplomacyType.Alliance:
                    _standing += 10;
                    break;
            }
        }

        public void Update(float deltaTime) {
            timeSinceLastPraise += deltaTime;
            givenMoneyRecently -= deltaTime * GIVEN_MONEY_DECAY;
        }


    }

}
