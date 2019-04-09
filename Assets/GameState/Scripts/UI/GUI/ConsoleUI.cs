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

    // Use this for initialization
    void Start() {
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
        if (command.Trim().Length <= 0) {
            return;
        }
        string[] parameters = command.Split(' ');
        if (parameters.Length < 1) {
            return;
        }
        if (cheats_enabled == false) {
            return;
        }
        if (ConsoleController.Instance.HandleInput(parameters)) {
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
