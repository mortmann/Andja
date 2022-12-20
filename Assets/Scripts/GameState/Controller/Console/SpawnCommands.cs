
using Andja.Model;
using System;

namespace Andja.Controller {
    public class SpawnCommands : ConsoleCommand {
        public SpawnCommands() : base("spawn", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("unit", SpawnUnit),
                new ConsoleCommand("crate", SpawnCrate),
            };
        }

        private bool SpawnCrate(string[] parameters) {
            string id = parameters[0];
            if (PrototypController.Instance.AllItems.ContainsKey(id) == false) {
                return false;
            }
            Item i = new Item(id);
            if (parameters.Length == 1 || int.TryParse(parameters[1], out int temp) == false) {
                i.count = 1;
            }
            else {
                i.count = temp;
            }
            World.Current.CreateItemOnMap(i, MouseController.Instance.GetMousePosition());
            return true;
        }

        private bool SpawnUnit(string[] parameters) {
            string id = parameters[0];
            // anything can thats not a number can be the current player
            if (PrototypController.Instance.UnitPrototypes.ContainsKey(id) == false) {
                return false;
            }
            int player = PlayerController.currentPlayerNumber;
            if (parameters.Length > 1) {
                if (int.TryParse(parameters[1], out player) == false) {
                    return false;
                }
            }
            Unit u = PrototypController.Instance.GetUnitForID(id);
            if (u == null)
                return false;
            Tile t = MouseController.Instance.GetTileUnderneathMouse();
            if (u.IsShip && t.Type != TileType.Ocean) {
                return false;
            }
            if (u.IsShip == false && t.Type == TileType.Ocean) {
                return false;
            }
            if (PlayerController.Instance.GetPlayer(player) == null)
                return false;
            World.Current.CreateUnit(u, PlayerController.Instance.GetPlayer(player), t);
            return true;
        }
    }
}