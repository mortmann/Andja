using Andja.Model;
using System;
using System.Linq;

namespace Andja.Controller {
    public class EffectCommands : ConsoleCommand {
        private readonly Func<GEventable> _getEventable;

        public EffectCommands(Func<GEventable> GetEventable) : base("effect", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("add", AddEffect, () => PrototypController.Instance.EffectPrototypeDatas.ToList()
                                                            .Where(e => e.Value.targets.IsTargeted(_getEventable.Invoke().TargetGroups))
                                                            .Select(e => e.Key).ToList()),
                new ConsoleCommand("remove", RemoveEffect, () => _getEventable.Invoke().Effects?.Select(e => e.ID).ToList()),
            };
            _getEventable = GetEventable;
        }

        private bool RemoveEffect(string[] parameters) {
            if (parameters.Length != 2)
                return false;
            bool all = parameters.Length > 2 && bool.TryParse(parameters[1], out _);
            return _getEventable.Invoke().RemoveEffect(new Effect(parameters[0]), all);
        }

        private bool AddEffect(string[] parameters) {
            if (parameters.Length == 0)
                return false;
            return _getEventable.Invoke().AddEffect(new Effect(parameters[0]));
        }
    }
}