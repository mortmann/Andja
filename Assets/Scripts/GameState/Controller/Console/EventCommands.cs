
using Andja.Model;
using System;
using System.Linq;

namespace Andja.Controller {
    public class EventCommands : ConsoleCommand {
        public EventCommands() : base("event", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("trigger", TriggerEvent, () => PrototypController.Instance.GameEventPrototypeDatas.Keys.ToList()),
                new ConsoleCommand("stop", StopEvent, () =>  EventController.Instance.GetActiveEventsIDs().ConvertAll(i => i+"")),
                new ConsoleCommand("list", ListEvent),
            };
        }

        private bool ListEvent(string[] arg) {
            EventController.Instance.ListAllActiveEvents();
            return true;
        }

        private bool StopEvent(string[] parameters) {
            if (parameters.Length == 2 && string.IsNullOrEmpty(parameters[1]) == false &&
                            uint.TryParse(parameters[1], out uint gid)) {
                return EventController.Instance.StopGameEvent(gid);
            }
            return false;
        }

        protected bool TriggerEvent(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            string id = parameters[0].Trim();
            if (PrototypController.Instance.GameEventExists(id) == false) {
                return false;
            }
            int player = -1;
            if (parameters.Length == 2 && string.IsNullOrEmpty(parameters[1]) == false) {
                int.TryParse(parameters[1], out player);
            }
            if (parameters.Length > 2 && parameters[2].StartsWith("s"))
                return EventController.Instance.TriggerEventForEventable(new GameEvent(id), MouseController.Instance.CurrentlySelectedIGEventable);
            if (player < 0)
                return EventController.Instance.TriggerEvent(id);
            else
                return EventController.Instance.TriggerEventForPlayer(new GameEvent(id), PlayerController.Instance.GetPlayer(player));
        }
    }
}