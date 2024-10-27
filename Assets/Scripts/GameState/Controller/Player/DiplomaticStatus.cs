using Andja.Model;
using Andja.UI;
using Andja.UI.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Andja.Controller {
    [JsonObject(MemberSerialization.OptIn)]
    public class DiplomaticStatus {
        /// <summary>
        /// One is always smaller than two
        /// </summary>
        [JsonProperty] public int PlayerNumberOne;
        public Player PlayerOne => PlayerController.Instance.GetPlayer(PlayerNumberOne);
        /// <summary>
        /// Two is always bigger than one
        /// </summary>
        [JsonProperty] public int PlayerNumberTwo;
        public Player PlayerTwo => PlayerController.Instance.GetPlayer(PlayerNumberTwo);

        public DiplomacyType NextHigherStatus => CurrentStatus + 1;

        [JsonProperty] public DiplomacyType CurrentStatus { get; protected set; }

        public DiplomaticStatus() {
        }

        public DiplomaticStatus(int one, int two) {
            if (one > two) {
                PlayerNumberOne = two;
                PlayerNumberTwo = one;
            }
            else {
                PlayerNumberOne = one;
                PlayerNumberTwo = two;
            }
            CurrentStatus = DiplomacyType.Neutral;
        }

        public override bool Equals(object obj) {
            if (!(obj is DiplomaticStatus p)) {
                return false;
            }
            return p == this;
        }

        public void TryIncrease() {
            if (CurrentStatus == DiplomacyType.Alliance)
                return;
            Player playerOne = PlayerController.Instance.GetPlayer(PlayerNumberOne);
            Player playerTwo = PlayerController.Instance.GetPlayer(PlayerNumberTwo);
            if (PlayerNumberTwo == PlayerController.currentPlayerNumber) {
                EventUIManager.Instance.Show(ChoiceInformation.CreateAskDiplomaticIncrease(playerOne, (CurrentStatus + 1), this));
            }
            else {
                if (playerTwo.AI.AskDiplomaticIncrease(playerOne, this)) {
                    Increase();
                }
            }
        }
        public void Increase() {
            ChangeDiplomaticStanding(NextHigherStatus);
        }
        /// <summary>
        /// Decreases the standing *always* maybe disable it in the future for missions orso.
        /// But even still make one the one that sends it and two the one receiving
        /// If the new is WAR -> change for all allies aswell. Defending call always first.
        /// </summary>
        /// <param name="playerOne"></param>
        /// <param name="playerTwo"></param>
        public void DecreaseDiplomaticStanding(Player playerOne, Player playerTwo) {
            if (CurrentStatus == DiplomacyType.War)
                return;
            ChangeDiplomaticStanding((DiplomacyType)((int)CurrentStatus - 1));
            if (CurrentStatus == DiplomacyType.War) {
                List<DiplomaticStatus> allies = PlayerController.Instance.GetAlliesFor(playerOne.Number);
                foreach (DiplomaticStatus ads in allies) {
                    if (ads.PlayerNumberOne == playerOne.Number) {
                        ads.ChangeDiplomaticStanding(DiplomacyType.War);
                    }
                    if (ads.PlayerNumberTwo == playerOne.Number) {
                        ads.ChangeDiplomaticStanding(DiplomacyType.War);
                    }
                }
                allies = PlayerController.Instance.GetAlliesFor(playerTwo.Number);
                foreach (DiplomaticStatus ads in allies) {
                    if (ads.PlayerNumberOne == playerTwo.Number) {
                        ads.ChangeDiplomaticStanding(DiplomacyType.War);
                    }
                    if (ads.PlayerNumberTwo == playerTwo.Number) {
                        ads.ChangeDiplomaticStanding(DiplomacyType.War);
                    }
                }
            }
        }
        /// <summary>
        /// Immediate change no checks here.
        /// </summary>
        /// <param name="playerNROne"></param>
        /// <param name="playerNRTwo"></param>
        /// <param name="changeTo"></param>
        public void ChangeDiplomaticStanding(DiplomacyType changeTo, bool force = false) {
            if (CurrentStatus == changeTo) {
                return;
            }
            Player playerOne = PlayerController.Instance.GetPlayer(PlayerNumberOne);
            Player playerTwo = PlayerController.Instance.GetPlayer(PlayerNumberTwo);
            if (CurrentStatus > changeTo) {
                //Should this before forced by the game -- ai needs to know
                if (force) {
                    playerOne.AI?.ForcedIncreasedDiplomaticStanding(playerTwo, changeTo);
                    playerTwo.AI?.ForcedIncreasedDiplomaticStanding(playerOne, changeTo);
                }
            }
            else {
                playerOne.AI?.DecreaseDiplomaticStanding(playerTwo, this);
                playerTwo.AI?.DecreaseDiplomaticStanding(playerOne, this);
            }
            CurrentStatus = changeTo;
            EventUIManager.Instance.Show(BasicInformation.DiplomacyChanged(this));
            PlayerController.Instance.TriggerDiplomaticChangeCb(playerOne, playerTwo, CurrentStatus, changeTo);
        }
        public bool AreAtWar() {
            return CurrentStatus == DiplomacyType.War;
        }
        public static bool operator ==(DiplomaticStatus a, DiplomaticStatus b) {
            if (ReferenceEquals(a, b)) {
                return true;
            }
            if ((a is null) || (b is null)) {
                return false;
            }
            return a.PlayerNumberOne == b.PlayerNumberOne && a.PlayerNumberTwo == b.PlayerNumberTwo
                || a.PlayerNumberTwo == b.PlayerNumberOne && a.PlayerNumberOne == b.PlayerNumberTwo;
        }

        public static bool operator !=(DiplomaticStatus a, DiplomaticStatus b) {
            // If both are null, or both are same instance, return false.
            if (ReferenceEquals(a, b)) {
                return false;
            }

            // If one is null, but not both, return true.
            if ((object)a == null || (object)b == null) {
                return true;
            }

            // Return true if the fields not match:
            return a.PlayerOne != b.PlayerOne || a.PlayerNumberTwo != b.PlayerNumberTwo;
        }

        public override int GetHashCode() {
            var hashCode = 971533886;
            hashCode = hashCode * -1521134295 + PlayerOne.GetHashCode();
            hashCode = hashCode * -1521134295 + PlayerNumberTwo.GetHashCode();
            return hashCode;
        }

    }
}