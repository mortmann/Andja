
using Andja.Model;
using System;

namespace Andja.Controller {
    public class EventCommands : ConsoleCommand {
        public EventCommands() : base("event", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("trigger", TriggerEvent),
                new ConsoleCommand("stop", StopEvent),
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
            if (parameters.Length < 2) {
                return false;
            }
            string id = parameters[1].Trim();
            if (PrototypController.Instance.GameEventExists(id) == false) {
                return false;
            }
            int player = -1;
            if (parameters.Length == 3 && string.IsNullOrEmpty(parameters[2]) == false) {
                int.TryParse(parameters[2], out player);
            }
            if (parameters.Length > 3 && parameters[3].StartsWith("s"))
                return EventController.Instance.TriggerEventForEventable(new GameEvent(id), MouseController.Instance.CurrentlySelectedIGEventable);
            if (player < 0)
                return EventController.Instance.TriggerEvent(id);
            else
                return EventController.Instance.TriggerEventForPlayer(new GameEvent(id), PlayerController.Instance.GetPlayer(player));
        }
    }
}