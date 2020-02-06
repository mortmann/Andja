using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
//TDOD: arrow keys for switching between old commands
public class ConsoleController : MonoBehaviour {

    public static ConsoleController Instance;

    public static List<string> logs = new List<string>();
    Dictionary<GameObject, Vector3> GOtoPosition;
    Action<string> writeToConsole;
    StreamWriter logWriter;
    // Use this for initialization
    void OnEnable() {
        Instance = this;
        Application.logMessageReceived += LogCallbackHandler;
#if UNITY_EDITOR == false
        string path = Path.Combine(SaveController.GetSaveGamesPath(), "logs");
        string filepath = Path.Combine(path,SaveController.SaveName + "_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") +".log");
        if (Directory.Exists(path) == false) {
            Directory.CreateDirectory(path);
        } 
        if (File.Exists(filepath) == false) {
            logWriter = File.CreateText(filepath);
        }
        int fCount = Directory.GetFiles(path, "*.log", SearchOption.TopDirectoryOnly).Length;
        if(fCount>5) {
            FileSystemInfo fileInfo = new DirectoryInfo(path)
                         .GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First();
            fileInfo.Delete();
        }
#endif

    }
    public void LogCallbackHandler(string condition, string stackTrace, LogType type) {
        string color = "";
        string typestring = "<b> " + type + "</b>: ";
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
                typestring = "";
                color = "c0c0c0ff";
                break;
            case LogType.Exception:
                color = "ff00ffff";
                break;
        }
        string log = "<color=#" + color + ">"+ typestring + " <i>" + condition + "</i> "/* +Environment.NewLine +"<size=9>" + stackTrace + "</size>"*/ + "</color> ";
        if(writeToConsole == null) {
            logs.Add(log);
        }
        else {
            writeToConsole?.Invoke(log);
        }
#if UNITY_EDITOR == false
        logWriter.Write(type + ": " + condition + Environment.NewLine + stackTrace);
#endif
    }

    internal void RegisterOnLogAdded(Action<string> writeToConsole) {
        this.writeToConsole += writeToConsole;
    }
    public List<string> FirstLevelCommands = new List<string> 
        { "speed", "player", "maxfps", "city", "unit", "ship", "island", "spawn", "event", "camera" };
    public bool HandleInput(string command) {
        if (command.Trim().Length <= 0) {
            return false;
        }
        string[] parameters = command.Split(null); // splits  whitespaces
        if (parameters.Length < 1) {
            return false;
        }
        for (int i = 0; i < parameters.Length; i++)
            parameters[i]=parameters[i].Trim();
        bool happend = false;
        switch (parameters[0]) {
            case "speed":
                float speed = 1;
                happend = float.TryParse(parameters[1], out speed);
                if (happend)
                    WorldController.Instance.SetSpeed(speed);
                break;
            case "player":
                happend = HandlePlayerCommands(parameters.Skip(1).ToArray());
                break;
            case "maxfps":
                int fps = -1;
                happend = int.TryParse(parameters[1], out fps);
                if(happend)
                    Application.targetFrameRate = fps;
                break;
            case "city":
                happend = HandleCityCommands(parameters.Skip(1).ToArray());
                break;
            case "unit":
                happend = HandleUnitCommands(parameters.Skip(1).ToArray());
                break;
            case "ship":
                happend = HandleShipCommands(parameters.Skip(1).ToArray());
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
    public List<string> ShipCommands = new List<string>
        { "cannon" };
    private bool HandleShipCommands(string[] parameters) {
        Ship ship = MouseController.Instance.SelectedUnit as Ship;
        if (ship == null) {
            Debug.Log("no ship selected");
            return false;
        }
        switch (parameters[0]) {
            case "cannon":
                return ShipAddCannon(parameters.Skip(1).ToArray(),ship);
            default:
                break;
        }
        return false;
    }

    private bool ShipAddCannon(string[] parameters,Ship ship) {
        if (parameters.Length == 0)
            return false;
        int amount = -1;
        if (int.TryParse(parameters[0], out amount) == false) {
            return false;
        }
        ship.CannonItem.count = amount;
        return true;
    }
    public List<string> EventsCommands = new List<string>
        { "trigger" };

    private bool HandleEventCommands(string[] parameters) {
        if (parameters.Length < 1) {
            return false;
        }
        string id = parameters[1];
        if (PrototypController.Instance.GameEventPrototypeDatas.ContainsKey(id) == false) {
            return false;
        }
        switch (parameters[0]) {
            case "trigger":
                int player = -1;
                if (parameters.Length == 3) {
                    int.TryParse(parameters[2], out player);
                }
                if(player < 0)
                    EventController.Instance.TriggerEventForEventable(new GameEvent(id), MouseController.Instance.CurrentlySelectedIGEventable);
                else
                    EventController.Instance.TriggerEventForPlayer(new GameEvent(id), PlayerController.GetPlayer(player));
                break;
            default:
                return false;
        }
        return true;
    }
    public List<string> SpawnCommands = new List<string>
        { "unit", "crate" };
    private bool HandleSpawnCommands(string[] parameters) {
        if (parameters.Length < 2) {
            return false;
        }
        //spawn unit UID playerid 
        //spawn building BID playerid --> not currently implementing
        int pos = 1;
        string id = parameters[pos];
       
        pos++;
        switch (parameters[0]) {
            case "unit":
                pos++;
                // anything can thats not a number can be the current player
                if (PrototypController.Instance.UnitPrototypes.ContainsKey(id) == false) {
                    return false;
                }
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
                if (PlayerController.GetPlayer(player) == null)
                    return false;
                World.Current.CreateUnit(u.Clone(player, t));
                return true;
            case "crate":
                pos++;
                // anything can thats not a number can be the current player
                if (PrototypController.Instance.AllItems.ContainsKey(id) == false) {
                    return false;
                }
                Item i = new Item(id);
                if (i.Exists() == false) {
                    return false;
                }
                if (parameters.Length > pos) {
                    if (int.TryParse(parameters[pos], out i.count) == false) {
                        i.count = 1;
                    }
                }
                World.Current.SpawnItemOnMap(i, MouseController.Instance.GetMousePosition());
                return true;
        }
        return false;
    }
    public List<string> CityCommands = new List<string>
        { "item","fillitup","builditems", "name", "player" };
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
            case "builditems":
                return AddAllItems(c.inventory, true);
            case "name":
                break;
            case "player":
                Debug.Log("Console Command not implemented!");
                break;
            case "event":
                Debug.Log("Console Command not implemented!");
                break;
            default:
                break;
        }
        return false;
    }
    public List<string> PlayerCommands = new List<string>
        { "change","money","diplomatic" };
    bool HandlePlayerCommands(string[] parameters) {
        switch (parameters[0]) {
            case "change":
                return ChangePlayer(parameters.Skip(1).ToArray());
            case "money":
                return ChangePlayerMoney(parameters.Skip(1).ToArray());
            case "diplomatic":
                return ChangeWar(parameters.Skip(1).ToArray());
            default:
                break;
        }
        return false;
    }

    private bool ChangeWar(string[] parameters) {
        if (parameters.Length == 0)
            return false;
        int playerOne = PlayerController.currentPlayerNumber;
        int pos = 0;
        // anything can thats not a number can be the current player
        if (parameters.Length > 2) {
            if (int.TryParse(parameters[pos], out playerOne) == false) {
                return false;
            }
            else {
                pos++;
            }
        }
        int playerTwo = 0;
        if (int.TryParse(parameters[pos], out playerTwo) == false) {
            return false;
        }
        if (playerOne < 0 || playerOne >= PlayerController.PlayerCount)
            return false;
        if (playerTwo < 0 || playerTwo >= PlayerController.PlayerCount)
            return false;

        if (PlayerController.Instance.ArePlayersAtWar(playerOne, playerTwo) == false)
            PlayerController.Instance.ChangeDiplomaticStanding(playerOne, playerTwo, DiplomacyType.War);
        else
            PlayerController.Instance.ChangeDiplomaticStanding(playerOne, playerTwo, DiplomacyType.Neutral);
        return true;
    }

    private bool ChangePlayerMoney(string[] parameters) {
        if (parameters.Length == 0)
            return false;
        int player = PlayerController.currentPlayerNumber;
        int pos = 0;
        // anything can thats not a number can be the current player
        if (parameters.Length > 1) { 
            if (int.TryParse(parameters[pos], out player) == false) {
                return false;
            }
            else {
                pos++;
            }
        }
        int money = 0;
        if (int.TryParse(parameters[pos], out money) == false) {
            return false;
        }
        PlayerController.Instance.AddMoney(money,player);
        return true;
    }

    private bool ChangePlayer(string[] parameters) {
        if (parameters.Length == 0)
            return false;
        int player = -1000;
        int pos = 0;
        // anything can thats not a number can be the current player
        if (int.TryParse(parameters[pos], out player) == false) {
            return false;
        }
        return PlayerController.Instance.ChangeCurrentPlayer(player);
    }
    public List<string> UnitCommands = new List<string>
        { "item","build","kill", "name", "player", "event" };
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
                u.inventory.AddItem(new Item("wood", 50));
                u.inventory.AddItem(new Item("tools", 50));
                return true;
            case "kill":
                u.Destroy();
                return true;
            case "name":
                Debug.Log("Console Command not implemented!");
                break;
            case "player":
                Debug.Log("Console Command not implemented!");
                break;
            case "event":
                Debug.Log("Console Command not implemented!");
                break;
            default:
                break;
        }
        return false;
    }
    public bool ChangeItemInInventory(string[] parameters, Inventory inv) {
        string id = null;
        int amount = 0; // amount can be plus for add or negative for remove
        if (parameters.Length != 2) {
            return false;
        }
        id = parameters[0];

        if (int.TryParse(parameters[1], out amount) == false) {
            return false;
        }
        if (PrototypController.Instance.AllItems.ContainsKey(id) == false) {
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
    private void OnDestroy() {
#if UNITY_EDITOR == false
        logWriter.Flush();
        logWriter.Close();
#endif
    }
}
