using Andja.Model;
using System;

namespace Andja.Controller {
    public class EffectCommands : ConsoleCommand {
        private readonly Func<IGEventable> _getEventable;

        public EffectCommands(Func<IGEventable> GetEventable) : base("effect", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("add", AddEffect),
                new ConsoleCommand("remove", RemoveEffect),
            };
            _getEventable = GetEventable;
        }

        private bool RemoveEffect(string[] parameters) {
            bool all = parameters.Length > 2 && bool.TryParse(parameters[2], out _);
            return _getEventable.Invoke().RemoveEffect(new Effect(parameters[1]), all);
        }

        private bool AddEffect(string[] parameters) {
            return _getEventable.Invoke().AddEffect(new Effect(parameters[1]));
        }
    }
}