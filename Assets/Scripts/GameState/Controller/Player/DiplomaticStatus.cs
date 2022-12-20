using System;

namespace Andja.Controller {
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
            if (ReferenceEquals(a, b)) {
                return false;
            }

            // If one is null, but not both, return true.
            if ((object)a == null || (object)b == null) {
                return true;
            }

            // Return true if the fields not match:
            return a.PlayerOne != b.PlayerOne || a.PlayerTwo != b.PlayerTwo;
        }
    }
}