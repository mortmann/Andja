using Andja.Controller;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ConsoleUI : MonoBehaviour {
        private const int showLast = 30;
        public GameObject TextPrefab;
        public Transform outputTransform;
        public InputField inputField;
        public Text predictiveText;

        public bool cheats_enabled;
        private List<string> Commands;
        private int currentCommandIndex = 0;
        ConsoleController cc => ConsoleController.Instance;
        private List<string> ItemIDs;
        private List<string> UnitIDs;
        private List<string> EventIDs;
        private List<string> EffectIDs;

        // Use this for initialization
        private void Start() {
            Commands = new List<string>();
            foreach (string s in ConsoleController.logs.Skip(ConsoleController.logs.Count - showLast))
                WriteToConsole(s);
            ConsoleController.Instance.RegisterOnLogAdded(WriteToConsole);
            LayoutElement le = TextPrefab.GetComponent<LayoutElement>();
            le.preferredWidth = Screen.width;
            le.minWidth = Screen.width;
            ItemIDs = new List<string>(PrototypController.Instance.AllItems.Keys);
            UnitIDs = new List<string>(PrototypController.Instance.UnitPrototypeDatas.Keys);
            EventIDs = new List<string>(PrototypController.Instance.GameEventPrototypeDatas.Keys);
            EffectIDs = new List<string>(PrototypController.Instance.EffectPrototypeDatas.Keys);
        }

        private void OnEnable() {
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.UpArrow)) {
                currentCommandIndex = Mathf.Clamp(currentCommandIndex - 1, 0, currentCommandIndex);
                if (Commands.Count > 0)
                    inputField.text = Commands[currentCommandIndex];
                inputField.MoveTextEnd(false);
            }
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                currentCommandIndex = Mathf.Clamp(currentCommandIndex + 1, 0, Commands.Count);
                if (currentCommandIndex == Commands.Count)
                    inputField.text = "";
                else
                    inputField.text = Commands[currentCommandIndex];
                inputField.MoveTextEnd(false);
            }

            string text = inputField.text.ToLower();
            if (String.IsNullOrEmpty(text)) {
                predictiveText.text = "";
                text = "";
            }

            List<string> predicted = null;
            string toPredicte = "";
            string[] parts = text.Split(null); // splits whitespaces
            
            string first = parts[0];
            if (parts.Length == 1) {
                toPredicte = parts[0];
                predicted = GetFilterCommands(cc.FirstLevelCommands.ToList(), toPredicte);
            }
            for (int i = 1; i < parts.Length; i++) {
                if (cc.FirstLevelCommands.Contains(first) == false) {
                    //doesnt exist so do nothing more
                    predictiveText.text = "";
                    return;
                }
                if (i < parts.Length - 1) {
                    if(GetCommands(parts[i - 1], i - 1 == 0).ToList().Contains(parts[i].ToLower())) {
                        continue;
                    } else {
                        return;
                    }
                }
                toPredicte = parts[parts.Length-1];
                predicted = GetFilterCommands(GetCommands(parts[i-1], i-1 == 0).ToList(), toPredicte);
            }

            if (predicted == null || predicted.Count == 0) {
                predictiveText.text = "";
                return;
            }
            string allPredicatedText = "";
            if (string.IsNullOrEmpty(text)) {
                allPredicatedText += "\n";
            }
            for (int i = predicted.Count - 1; i >= 0; i--) {
                string predicte = predicted[i];
                predicte = predicte.Remove(0, toPredicte.Length);
                allPredicatedText += text + "<color=yellow>" + predicte + "</color>";
                if (i > 0)
                    allPredicatedText += "\n";
            }
            predictiveText.text = allPredicatedText;

            if (Input.GetKeyDown(KeyCode.Tab)) {
                string add = predicted[predicted.Count - 1];
                add = add.Remove(0, toPredicte.Length);
                inputField.text = text + add + " ";
                inputField.MoveTextEnd(false);
                predictiveText.text = "";
            }
        }

        public void WriteToConsole(string text) {
            if (outputTransform.childCount > showLast) {
                SimplePool.Despawn(outputTransform.GetChild(0).gameObject);
            }
            GameObject go = SimplePool.Spawn(TextPrefab, Vector3.zero, Quaternion.identity);
            go.GetComponent<Text>().text = text;
            go.name = text;
            go.transform.SetParent(outputTransform, false);
            go.transform.SetAsLastSibling();
        }

        public void ReadFromConsole() {
            string command = inputField.text;
            if (String.IsNullOrWhiteSpace(command))
                return;
            if (cheats_enabled == false) {
                return;
            }
            Commands.Add(command);
            if (Commands.Count > 100)
                Commands.RemoveAt(0);
            currentCommandIndex = Commands.Count;
            if (ConsoleController.Instance.HandleInput(command)) {
                WriteToConsole(command + "! Command succesful executed!");
            }
            else {
                WriteToConsole(command + "! Command execution failed!");
            }
            inputField.text = "";
            inputField.Select();
            inputField.ActivateInputField();
        }
        /// <summary>
        /// Get all commands starting with the filter or when empty all
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<string> GetFilterCommands(List<string> commands, string filter) {
            if(string.IsNullOrEmpty(filter)) {
                return commands;
            } else {
                return commands.FindAll(x => x.StartsWith(filter));
            }
        }

        IReadOnlyList<string> GetCommands(string lastCommand, bool isFirstLevel) {
            switch (lastCommand) {
                case "speed":
                    return new List<string>();

                case "player":
                    return cc.PlayerCommands;

                case "maxfps":
                    return new List<string>();

                case "city":
                    return cc.CityCommands;

                case "unit":
                    if(isFirstLevel)
                        return cc.UnitCommands;
                    else
                        return UnitIDs;

                case "ship":
                    return cc.ShipCommands;

                case "island":
                    return new List<string>();

                case "spawn":
                    return cc.SpawnCommands;

                case "event":
                    if (isFirstLevel)
                        return cc.EventsCommands;
                    else
                        return EventIDs;

                case "camera":
                    return new List<string>();

                case "effect":
                    return cc.EffectsCommands;

                case "graphy":
                    return cc.GraphyCommands;

                case "toggle":
                    return cc.CheatCommands;

                case "structure":
                    return cc.StructureCommands;
                case "item":
                case "crate":
                    return ItemIDs;

                case "trigger":
                    return EventIDs;

                case "diplomatic":
                    return new List<string>(Enum.GetNames(typeof(DiplomacyType)));
                //IMPORTANT if in future any other type of command gets add/remove this needs rework then
                case "add":
                case "remove":
                    return EffectIDs;
                default:
                    return new List<string>();
            }
        }

    }
}