using Andja.Model;
using Andja.UI.Model;
using System;
using System.Linq;
using UnityEngine;

namespace Andja.Controller {
    public class UnitCommands : ConsoleCommand {
        Unit Unit => MouseController.Instance.SelectedUnit;


        public UnitCommands() : base("unit", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("kill", KillUnit),
                new ConsoleCommand("name", NameUnit),
                new ConsoleCommand("player", ChangePlayer),
                new ConsoleCommand("build", BuildUnit),
                new ConsoleCommand("item", AddItemUnit, () => PrototypController.Instance.AllItems.Keys.ToList()),
                new ConsoleCommand("event", (parameters) => EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[1]), Unit)),
                new EffectCommands(() => Unit),
                new ConsoleCommand("send", SendUnitTo),
            };
        }

        private bool SendUnitTo(string[] arg) {
            if (arg.Length != 2) return false;
            if (float.TryParse(arg[0], out float x) && float.TryParse(arg[1], out float y))
                return Unit.GiveMovementCommand(x, y, true);
            return false;
        }

        public override bool Do(string[] parameters) {
            if (MouseController.Instance.SelectedUnit == null) return false;
            return base.Do(parameters);
        }
        private bool AddItemUnit(string[] parameters) {
            if (parameters.Length != 2) {
                return false;
            }
            string id = parameters[0];
            if (int.TryParse(parameters[1], out int amount) == false) {
                return false;
            }
            if (PrototypController.Instance.AllItems.ContainsKey(id) == false) {
                return false;
            }
            Item i = new Item(id, Mathf.Abs(amount));
            if (amount > 0) {
                Unit.Inventory.AddItem(i);
            }
            else {
                Unit.Inventory.RemoveItemAmount(i);
            }
            return true;
        }

        private bool BuildUnit(string[] arg) {
            Unit.Inventory.AddItem(new Item("wood", 50));
            Unit.Inventory.AddItem(new Item("tools", 50));
            return true;
        }

        private bool ChangePlayer(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            if (int.TryParse(parameters[0], out int num)) {
                Unit.ChangePlayer(num);
            }
            return true;
        }

        private bool NameUnit(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            Unit.SetName(parameters[0]);
            return true;
        }

        private bool KillUnit(string[] arg) {
            Unit.Destroy(null);
            return Unit == null || Unit.IsDestroyed;
        }
        
    }
}