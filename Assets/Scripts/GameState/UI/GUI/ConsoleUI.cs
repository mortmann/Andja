using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Andja.UI {

    public class ConsoleUI : MonoBehaviour {
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

        // Use this for initialization
        private void Start() {
            Commands = new List<string>();
            foreach (string s in ConsoleController.logs)
                WriteToConsole(s);
            ConsoleController.Instance.RegisterOnLogAdded(WriteToConsole);
            LayoutElement le = TextPrefab.GetComponent<LayoutElement>();
            le.preferredWidth = Screen.width;
            le.minWidth = Screen.width;
            ItemIDs = new List<string>(PrototypController.Instance.AllItems.Keys);
            UnitIDs = new List<string>(PrototypController.Instance.UnitPrototypeDatas.Keys);
            EventIDs = new List<string>(PrototypController.Instance.GameEventPrototypeDatas.Keys);
        }

        private void OnEnable() {
            inputField.Select();
            inputField.ActivateInputField();
        }

        private void OnDisable() {
        }

        // Update is called once per frame
        private void Update() {
            if (Input.GetKeyDown(KeyCode.UpArrow)) {
                currentCommandIndex = Mathf.Clamp(currentCommandIndex - 1, 0, currentCommandIndex);
                if (Commands.Count > 0)
                    inputField.text = Commands[currentCommandIndex];
            }
            if (Input.GetKeyDown(KeyCode.DownArrow)) {
                currentCommandIndex = Mathf.Clamp(currentCommandIndex + 1, 0, Commands.Count);
                if (currentCommandIndex == Commands.Count)
                    inputField.text = "";
                else
                    inputField.text = Commands[currentCommandIndex];
            }

            string text = inputField.text.ToLower();
            if (String.IsNullOrEmpty(text)) {
                predictiveText.text = "";
                return;
            }

            List<string> predicted = null;
            string toPredicte = "";
            string[] parts = text.Split(null); // splits whitespaces
            if (parts.Length == 0) {
                predictiveText.text = "";
                return;
            }
            string first = parts[0];
            if (parts.Length == 1) {
                toPredicte = parts[0];
                predicted = cc.FirstLevelCommands.FindAll(x => x.StartsWith(toPredicte));
            }
            string second = null;
            if (parts.Length >= 2) {
                if (cc.FirstLevelCommands.Contains(first) == false) {
                    //doesnt exist so do nothing more
                    predictiveText.text = "";
                    return;
                }
                toPredicte = parts[1];
                predicted = GetSecondLevelCommands(first).FindAll(x => x.StartsWith(toPredicte));
                second = parts[parts.Length - 2].ToLower();
            }
            if (parts.Length > 2) {
                //TODO: changer it to like the second level
                // if the command doesnt exist in the second level return
                if (GetSecondLevelCommands(first).Contains(second) == false) {
                    //doesnt exist so do nothing more
                    predictiveText.text = "";
                    return;
                }
                toPredicte = parts[parts.Length - 1]?.ToLower();
                if (String.IsNullOrEmpty(toPredicte)) {
                    predictiveText.text = "";
                    return;
                }
                predicted = GetThirdLevelCommands(second).FindAll(x => x.StartsWith(toPredicte));
            }
            if (String.IsNullOrEmpty(toPredicte)) {
                predictiveText.text = "";
                return;
            }
            if (predicted == null || predicted.Count == 0) {
                predictiveText.text = "";
                return;
            }
            string allPredicatedText = "";
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
            GameObject go = Instantiate(TextPrefab);
            go.GetComponent<Text>().text = text;
            go.transform.SetParent(outputTransform, false);
            if (outputTransform.childCount > 30) {
                Destroy(outputTransform.GetChild(0).gameObject);
            }
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

        private List<string> GetSecondLevelCommands(string firstlevel) {
            switch (firstlevel) {
                case "speed":
                    return new List<string>();

                case "player":
                    return cc.PlayerCommands;

                case "maxfps":
                    return new List<string>();

                case "city":
                    return cc.CityCommands;

                case "unit":
                    return cc.UnitCommands;

                case "ship":
                    return cc.ShipCommands;

                case "island":
                    return new List<string>();

                case "spawn":
                    return cc.SpawnCommands;

                case "event":
                    return cc.EventsCommands;

                case "camera":
                    return new List<string>();

                default:
                    Debug.Log("Predicte-List not found.");
                    return new List<string>();
            }
        }

        private List<string> GetThirdLevelCommands(string secondlevel) {
            switch (secondlevel) {
                case "item":
                case "crate":
                    return ItemIDs;

                case "unit":
                    return UnitIDs;

                case "trigger":
                case "event":
                    return EventIDs;

                case "diplomatic":
                    return new List<string>(Enum.GetNames(typeof(DiplomacyType)));

                default:
                    Debug.Log("Predicte-List not found.");
                    return new List<string>();
            }
        }
    }
}