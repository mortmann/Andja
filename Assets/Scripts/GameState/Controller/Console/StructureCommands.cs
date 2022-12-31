using Andja.Model;
using System;

namespace Andja.Controller {
    public class StructureCommands : ConsoleCommand {
        Structure Structure => MouseController.Instance.SelectedStructure;
        public StructureCommands() : base("structure", null) {
            NextLevelCommands = new ConsoleCommand[]{
                new ConsoleCommand("destroy", DeleteStructure),
                new ConsoleCommand("filloutput", FillOutput),
                new ConsoleCommand("fillinput", FillInput),
                new ConsoleCommand("event", (parameters) => EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[1]), Structure)),
                new EffectCommands(() => Structure),
            };
        }
        public override bool Do(string[] parameters) {
            if (Structure == null) return false;
            return base.Do(parameters);
        }

        private bool FillOutput(string[] arg) {
            if (!(Structure is OutputStructure os)) return false;
            foreach (Item output in os.Output) {
                output.count = os.MaxOutputStorage;
                os.CallOutputChangedCb();
            }
            return true;
        }
        private bool FillInput(string[] arg) {
            if (!(Structure is ProductionStructure ps)) return false;
            for (int i = 0; i < ps.Intake.Length; i++) {
                ps.Intake[i].count = ps.GetMaxIntakeForIndex(i);
            }
            return true;
        }

        private bool DeleteStructure(string[] arg) {
            Structure?.Destroy();
            return Structure.IsDestroyed;
        }
    }
}