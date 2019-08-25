using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;

public class ConsoleUI : MonoBehaviour {

    public GameObject TextPrefab;
    public Transform outputTransform;
    public InputField inputField;
    public bool cheats_enabled;
    List<string> Commands;
    int currentCommandIndex=0;
    // Use this for initialization
    void Start() {
        Commands = new List<string>();
        foreach (string s in ConsoleController.logs)
            WriteToConsole(s);
        ConsoleController.Instance.RegisterOnLogAdded(WriteToConsole);
    }

    void OnEnable() {
        inputField.Select();
        inputField.ActivateInputField();
    }
    private void OnDisable() {
        
    }
    // Update is called once per frame
    void Update() {
        if (Input.GetKeyDown(KeyCode.UpArrow)) {
            currentCommandIndex = Mathf.Clamp(currentCommandIndex-1, 0, currentCommandIndex);
            if(Commands.Count>0)
                inputField.text = Commands[currentCommandIndex];
        }
        if (Input.GetKeyDown(KeyCode.DownArrow)) {
            currentCommandIndex = Mathf.Clamp(currentCommandIndex + 1, 0, Commands.Count);
            if(currentCommandIndex==Commands.Count)
                inputField.text = "";
            else
                inputField.text = Commands[currentCommandIndex];
        }
    }
    public void WriteToConsole(string text) {
        GameObject go = Instantiate(TextPrefab);
        go.GetComponent<Text>().text = text;
        float width = outputTransform.GetComponent<RectTransform>().rect.width;
        go.GetComponent<LayoutElement>().minWidth = width;
        go.transform.SetParent(outputTransform);
        if (outputTransform.childCount > 30) {
            Destroy(outputTransform.GetChild(0).gameObject);
        }
    }
    public void ReadFromConsole() {
        string command = inputField.text;
        if (command.Length < 0)
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

}
