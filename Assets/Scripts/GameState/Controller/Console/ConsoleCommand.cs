using System;
using System.Collections.Generic;
using System.Linq;

namespace Andja.Controller {

    public class ConsoleCommand {
        public virtual string Argument { get; protected set; }

        protected Func<string[], bool> Command;
        protected List<string> commandArgumentList;
        private Func<List<string>> getArguments;

        public ConsoleCommand[] NextLevelCommands { get; protected set; }
        public ConsoleCommand(string argument, Func<string[], bool> action) {
            Argument = argument;
            Command = action;
        }

        public ConsoleCommand(string argument, Func<string[], bool> action, Func<List<string>> getArguments) : this(argument, action) {
            this.getArguments = getArguments;
        }

        public bool IsResponsible(string[] parameters) {
            return parameters[0].ToLower() == Argument;
        }

        public virtual bool Do(string[] parameters) {
            if(NextLevelCommands != null) {
                foreach (ConsoleCommand command in NextLevelCommands) {
                    if (command.IsResponsible(parameters)) {
                        return command.Do(parameters.Skip(1).ToArray());
                    }
                }
            }
            return Command?.Invoke(parameters) == true;
        }

        public List<string> GetCommandList() {
            if(commandArgumentList == null && NextLevelCommands != null) {
                commandArgumentList = NextLevelCommands.Select(c => c.Argument).ToList();
                commandArgumentList.Sort();
            }
            return commandArgumentList ?? getArguments?.Invoke();
        }

        public static ConsoleCommand GetEntryCommand() {
            return new ConsoleCommand(null, null) {
                NextLevelCommands = new ConsoleCommand[] {
                    new CityCommands(),
                    new UnitCommands(),
                    new ShipCommands(),
                    new GraphyCommands(),
                    new ToggleCheatCommands(),
                    new EventCommands(),
                    new StructureCommands(),
                    new SpawnCommands(),
                    new PlayerCommands(),
                }.Union(FirstLevelCommands.GetFirstLevel()).ToArray()
            };
        }
    }
}