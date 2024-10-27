using System;
using Tayx.Graphy;
using UnityEngine;

namespace Andja.Controller {
    public class GraphyCommands : ConsoleCommand {
        public GraphyManager GraphyInstance => ConsoleController.Instance.GraphyInstance;
        public GraphyCommands() : base("grapy", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("full", (_) => SetMode(GraphyManager.ModulePreset.FPS_FULL_RAM_FULL_AUDIO_FULL_ADVANCED_FULL)),
                new ConsoleCommand("medium", (_) => SetMode(GraphyManager.ModulePreset.FPS_FULL_RAM_FULL_AUDIO_FULL)),
                new ConsoleCommand("light", (_) => SetMode(GraphyManager.ModulePreset.FPS_FULL)),
                new ConsoleCommand("fps", (_) => SetMode(GraphyManager.ModulePreset.FPS_BASIC)),
                new ConsoleCommand("switchmode", (_) => { GraphyInstance.ToggleModes(); return true; }),
            };
            this.Command = TurnOff;
        }

        private bool TurnOff(string[] parameters) {
            if (parameters.Length == 0) {
                if (GraphyInstance != null) {
                    UnityEngine.Object.Destroy(GraphyInstance.gameObject);
                }
                return true;
            }
            return false;
        }

        private bool SetMode(GraphyManager.ModulePreset mode) {
            GraphyInstance.SetPreset(mode);
            return true;
        }
    }
}