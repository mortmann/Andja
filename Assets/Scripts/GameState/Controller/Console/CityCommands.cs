using Andja.Model;
using System;
using UnityEngine;

namespace Andja.Controller {
    public class CityCommands : ConsoleCommand {
        //            { "item","fillitup","builditems", "name", "player", "effect" };
        City City;
        public CityCommands() : base("city", null) {
            NextLevelCommands = new ConsoleCommand[] {
                new ConsoleCommand("item", AddItem),
                new ConsoleCommand("fillitup", FillItUp),
                new ConsoleCommand("builditems", BuildItems),
                new ConsoleCommand("name", ChangeName),
                //new ConsoleCommand("player", ChangePlayer),
                new ConsoleCommand("event", (parameters) => EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[1]), City)),
                new EffectCommands(() => City),
            };
        }

        private bool BuildItems(string[] arg) {
            foreach (Item i in PrototypController.Instance.BuildItems) {
                City.Inventory.AddItem(new Item(i.ID, int.MaxValue));
            }
            return true;
        }

        private bool ChangeName(string[] arg) {
            throw new NotImplementedException();
        }

        private bool ChangePlayer(string[] arg) {
            throw new NotImplementedException();
        }

        private bool FillItUp(string[] arg) {
            foreach (Item i in City.Inventory.Items.Values) {
                City.Inventory.AddItem(new Item(i.ID, int.MaxValue));
            }
            return true;
        }

        private bool AddItem(string[] parameters) {
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
                City.Inventory.AddItem(i);
            }
            else {
                City.Inventory.RemoveItemAmount(i);
            }
            return true;
        }

        public override bool Do(string[] parameters) {
            // anything can thats not a number can be the current player
            if (int.TryParse(parameters[0], out int player) == false) {
                player = PlayerController.currentPlayerNumber;
            }
            City = CameraController.Instance.nearestIsland?.FindCityByPlayer(player) as City;
            if (City == null) return false;
            return base.Do(parameters);
        }
    }
}