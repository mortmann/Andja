
using UnityEngine;

namespace Andja.Controller {
    public class ToggleCheatCommands : ConsoleCommand {

        public ToggleCheatCommands() : base("toggle", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("isgod", (_) => {MouseController.Instance.IsGod = !MouseController.Instance.IsGod; return true; }),
                new ConsoleCommand("allstructuresenabled", (_) => {
                    BuildController.Instance.AllStructuresEnabled = !BuildController.Instance.AllStructuresEnabled;return true;
                }),
                new ConsoleCommand("nocost", (_) => {BuildController.Instance.ToggleBuildCost(); return true;}),
                new ConsoleCommand("debugdata", (_) => {UIController.Instance?.ToggleDebugData(); return true;}),
                new ConsoleCommand("fogofwar", (_) => { 
                    var fogOfWar = GameObject.Find("FOW Canvas").transform.GetChild(0).gameObject;
                    fogOfWar.SetActive(!fogOfWar.activeSelf);
                    return true; }),
                new ConsoleCommand("nounitbuildrestriction", (_) => { BuildController.Instance.ToggleUnitBuildRangeRestriction(); return true; }),
            };
        }
    }
}
