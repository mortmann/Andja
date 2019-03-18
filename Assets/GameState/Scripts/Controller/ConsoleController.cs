using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConsoleController : MonoBehaviour {

    public static ConsoleController Instance;

    public static List<string> logs = new List<string>();
    Dictionary<GameObject, Vector3> GOtoPosition;
    Action<string> writeToConsole;
    // Use this for initialization
    void OnEnable() {
        Instance = this;
        Application.logMessageReceived += LogCallbackHandler;
    }
    public void LogCallbackHandler(string condition, string stackTrace, LogType type) {
        string color = "";
        switch (type) {
            case LogType.Error:
                color = "ff0000ff";
                break;
            case LogType.Assert:
                color = "add8e6ff";
                break;
            case LogType.Warning:
                color = "ffa500ff";
                break;
            case LogType.Log:
                color = "c0c0c0ff";
                break;
            case LogType.Exception:
                color = "ff00ffff";
                break;
        }
        string log = type + ": " + condition + " -> " + stackTrace;
        writeToConsole?.Invoke("<color=#" + color + ">" + log + "</color> ");
        logs.Add(log);
    }

    internal void RegisterOnLogAdded(Action<string> writeToConsole) {
        this.writeToConsole += writeToConsole;
    }

    public bool HandleInput(string[] parameters) {
        bool happend = false;
        switch (parameters[0]) {
            case "city":
                happend = HandleCityCommands(parameters.Skip(1).ToArray());
                break;
            case "unit":
                happend = HandleUnitCommands(parameters.Skip(1).ToArray());
                break;
            case "island":
                break;
            case "spawn":
                happend = HandleSpawnCommands(parameters.Skip(1).ToArray());
                break;
            case "event":
                happend = HandleEventCommands(parameters.Skip(1).ToArray());
                break;
            case "camera":
                int num = 0;
                happend = int.TryParse(parameters[1], out num);
                bool turn = num == 1;
                CameraController.devCameraZoom = turn;
                break;
            case "itsrainingbuildings":
                //easteregg!
                GOtoPosition = new Dictionary<GameObject, Vector3>();
                BoxCollider2D[] all = FindObjectsOfType<BoxCollider2D>();
                foreach (BoxCollider2D b2d in all) {
                    if (b2d.gameObject.GetComponent<Rigidbody2D>() != null) {
                        continue;
                    }
                    GOtoPosition.Add(b2d.gameObject, b2d.gameObject.transform.position);
                    Rigidbody2D rb2 = b2d.gameObject.AddComponent<Rigidbody2D>();
                    rb2.gravityScale = UnityEngine.Random.Range(0.6f, 2.7f);
                    rb2.inertia = UnityEngine.Random.Range(0.5f, 1.5f);
                }
                happend = true;
                break;
            case "itsdrainingbuildings":
                if (GOtoPosition == null)
                    break;
                foreach (GameObject go in GOtoPosition.Keys) {
                    if (go == null) {
                        continue;
                    }
                    go.transform.position = GOtoPosition[go];
                    Destroy(go.GetComponent<Rigidbody2D>());
                }
                happend = true;
                break;
            case "1":
                City c = CameraController.Instance.nearestIsland.FindCityByPlayer(PlayerController.currentPlayerNumber);
                happend = AddAllItems(c.inventory);
                break;
            default:
                break;
        }
        return happend;
    }

    private bool HandleEventCommands(string[] parameters) {
        if (parameters.Length < 1) {
            return false;
        }
        int id = -1;
        if (int.TryParse(parameters[1], out id) == false) {
            return false;
        }
        switch (parameters[0]) {
            case "triggerplayer":
                int player = PlayerController.currentPlayerNumber;
                if (parameters.Length == 3) {
                    int.TryParse(parameters[2], out player);
                }
                EventController.Instance.TriggerEventForPlayer(new GameEvent(id), PlayerController.Instance.GetPlayer(player));
                break;
            case "trigger":
                EventController.Instance.TriggerEventForEventable(new GameEvent(id), MouseController.Instance.CurrentlySelectedIGEventable);
                break;
            default:
                return false;
        }
        return true;
    }

    private bool HandleSpawnCommands(string[] parameters) {
        if (parameters.Length < 1) {
            return false;
        }
        //spawn unit UID playerid 
        //spawn building BID playerid --> not currently implementing
        int pos = 0;
        // switch(parameters[pos]) case unit : case building
        pos++;
        int id = -1000;
        // anything can thats not a number can be the current player
        if (int.TryParse(parameters[pos], out id) == false) {
            return false;
        }
        pos++;
        int player = PlayerController.currentPlayerNumber;
        if (parameters.Length > pos) {
            if (int.TryParse(parameters[pos], out player) == false) {
                return false;
            }
            else {
                pos++;
            }
        }

        Unit u = PrototypController.Instance.GetUnitForID(id);
        if (u == null)
            return false;
        Tile t = MouseController.Instance.GetTileUnderneathMouse();
        if (u.IsShip && t.Type != TileType.Ocean) {
            return false;
        }
        if (u.IsShip == false && t.Type == TileType.Ocean) {
            return false;
        }
        if (PlayerController.Instance.GetPlayer(player) == null)
            return false;
        World.Current.CreateUnit(u.Clone(player, t));
        return true;
    }

    bool HandleCityCommands(string[] parameters) {
        if (parameters.Length < 1) {
            return false;
        }
        int player = -1000;
        int pos = 0;
        // anything can thats not a number can be the current player
        if (int.TryParse(parameters[pos], out player) == false) {
            player = PlayerController.currentPlayerNumber;
        }
        else {
            pos++;
        }
        if (player < 0) { // do we want to be able to console access to wilderness
            return false;
        }
        City c = CameraController.Instance.nearestIsland?.FindCityByPlayer(player);
        if (c == null) {
            return false;
        }
        switch (parameters[pos]) {
            case "item":
                return ChangeItemInInventory(parameters.Skip(2).ToArray(), c.inventory);
            case "fillitup":
                return AddAllItems(c.inventory);
            case "build":
                return AddAllItems(c.inventory, true);
            case "name":
                break;
            case "player":
                break;
            case "event":
                break;
            default:
                break;
        }
        return false;
    }
    bool HandleUnitCommands(string[] parameters) {
        if (parameters.Length < 1) {
            return false;
        }
        int player = -1000;
        int pos = 0;
        // anything can thats not a number can be the current player
        if (int.TryParse(parameters[pos], out player) == false) {
            player = PlayerController.currentPlayerNumber;
        }
        else {
            pos++;
        }
        if (player < 0) { // do we want to be able to console access to wilderness
            Debug.Log("player<0");
            return false;
        }
        Unit u = MouseController.Instance.SelectedUnit;
        if (u == null) {
            Debug.Log("no unit selected");
            return false;
        }
        switch (parameters[pos]) {
            case "item":
                return ChangeItemInInventory(parameters.Skip(1).ToArray(), u.inventory);
            case "build":
                u.inventory.AddItem(new Item(1, 50));
                u.inventory.AddItem(new Item(2, 50));
                return true;
            case "kill":
                u.Destroy();
                return true;
            case "name":
                break;
            case "player":
                break;
            case "event":
                break;
            default:
                break;
        }
        return false;
    }
    public bool ChangeItemInInventory(string[] parameters, Inventory inv) {
        int id = -1;
        int amount = 0; // amount can be plus for add or negative for remove
        if (parameters.Length != 2) {
            return false;
        }
        if (int.TryParse(parameters[0], out id) == false) {

            return false;
        }
        if (int.TryParse(parameters[1], out amount) == false) {

            return false;
        }
        if (id < PrototypController.StartID) {
            return false;
        }
        Item i = new Item(id, Mathf.Abs(amount));
        if (amount > 0) {
            inv.AddItem(i);
        }
        else {
            inv.RemoveItemAmount(i);
        }
        return true;
    }

    private bool AddAllItems(Inventory inv, bool onlyBuildItems = false) {
        foreach (Item i in inv.Items.Values) {
            if (onlyBuildItems) {
                if (i.Type != ItemType.Build) {
                    continue;
                }
            }
            inv.AddItem(new Item(i.ID, int.MaxValue));
        }
        return true;
    }
}
