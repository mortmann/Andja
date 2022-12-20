using Andja.Model;
using System;

namespace Andja.Controller {
    public class UnitCommands : ConsoleCommand {
        Unit Unit => MouseController.Instance.SelectedUnit;
        public UnitCommands() : base("unit", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("kill", KillUnit),
                new ConsoleCommand("name", NameUnit),
                new ConsoleCommand("player", ChangePlayer),
                new ConsoleCommand("build", BuildUnit),
                new ConsoleCommand("item", AddItemUnit),
                new ConsoleCommand("event", (parameters) => EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[1]), Unit)),
                new EffectCommands(() => Unit),
            };
        }
        public override bool Do(string[] parameters) {
            if (MouseController.Instance.SelectedUnit == null) return false;
            return base.Do(parameters);
        }
        private bool AddItemUnit(string[] arg) {
            throw new NotImplementedException();
        }

        private bool BuildUnit(string[] arg) {
            Unit.Inventory.AddItem(new Item("wood", 50));
            Unit.Inventory.AddItem(new Item("tools", 50));
            return true;
        }

        private bool ChangePlayer(string[] parameters) {
            if (int.TryParse(parameters[1], out int num)) {
                Unit.ChangePlayer(num);
            }
            return true;
        }

        private bool NameUnit(string[] parameters) {
            Unit.SetName(parameters[0]);
            return true;
        }

        private bool KillUnit(string[] arg) {
            Unit.Destroy(null);
            return Unit.IsDestroyed;
        }
        
    }
}