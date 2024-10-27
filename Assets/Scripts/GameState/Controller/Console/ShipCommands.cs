using Andja.Model;
using System;

namespace Andja.Controller {
    public class ShipCommands : ConsoleCommand {
        private Ship ship;

        public ShipCommands() : base("ship", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("cannon", ChangeCannon),
            };
        }
        public override bool Do(string[] parameters) {
            ship = MouseController.Instance.SelectedUnit as Ship;
            if (ship == null) {
                return false;
            }
            return base.Do(parameters);
        }
        private bool ChangeCannon(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            if (int.TryParse(parameters[0], out int amount) == false) {
                return false;
            }
            ship.CannonItem.count = amount;
            return true;
        }
    }
}