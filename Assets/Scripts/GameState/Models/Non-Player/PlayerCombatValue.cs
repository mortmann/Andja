using System.Collections.Generic;
using System.Linq;

namespace Andja.Model {

    public class PlayerCombatValue {
        public Player Player;
        public float EndScore => UnitValue * 0.5f + ShipValue * 0.5f + MoneyValue * 0.25f + MilitaryStructureValue * 0.25f;
        public float UnitValue;
        public float ShipValue;
        public float MoneyValue; // are they supposed to know how much they have?
        public float MilitaryStructureValue;
        //public float TechnologyValue; // still not implemented

        public PlayerCombatValue(Player player, PlayerCombatValue isMe) {
            Player = player;
            UnitValue = 0;
            foreach (Unit u in player.GetLandUnits()) {
                UnitValue += u.Damage / 2 + u.MaxHealth / 2;
            }
            ShipValue = 0;
            foreach (Ship s in player.GetShipUnits()) {
                ShipValue += s.Damage / 2 + s.MaxHealth / 2;
            }
            List<MilitaryStructure> militaryStructures = new List<MilitaryStructure>(player.AllStructures.OfType<MilitaryStructure>());
            MilitaryStructureValue = 0;
            foreach (MilitaryStructure structure in militaryStructures) {
                MilitaryStructureValue++;
            }
            //imitates a guess how much the player makes
            //also guess the money in the bank?
            if (isMe != null) {
                MoneyValue = UnityEngine.Random.Range(player.TreasuryChange - player.TreasuryChange / 4, player.TreasuryChange + player.TreasuryChange / 4);
                //compare the value to the calculating player
                UnitValue = Divide(UnitValue, isMe.UnitValue);
                ShipValue = Divide(ShipValue, isMe.ShipValue);
                MoneyValue = Divide(MoneyValue, isMe.MoneyValue);
                MilitaryStructureValue = Divide(MilitaryStructureValue, isMe.MilitaryStructureValue);
            }
            else {
                MoneyValue = player.TreasuryChange;
            }
        }

        private float Divide(float one, float two) {
            if (two == 0)
                return one;
            return one / two;
        }
    }
}