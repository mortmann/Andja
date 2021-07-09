using Andja.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tayx.Graphy;
using UnityEngine;

namespace Andja.Controller {

    //TDOD: arrow keys for switching between old commands
    public class ConsoleController : MonoBehaviour {
        public static ConsoleController Instance;
        public GraphyManager GraphyPrefab;
        private GraphyManager GraphyInstance;
        public static List<string> logs = new List<string>();
        private Dictionary<GameObject, Vector3> GOtoPosition;
        private Action<string> writeToConsole;
        private StreamWriter logWriter;

        readonly string tempLogName = "temp.log";
        static string logPath = "";
        private void OnEnable() {
            Instance = this;
            Application.logMessageReceived += LogCallbackHandler;
            if (Application.isEditor == false) {
                logPath = Path.Combine(SaveController.GetSaveGamesPath(), "logs");
                string filepath = Path.Combine(logPath, tempLogName);
                if (Directory.Exists(logPath) == false) {
                    Directory.CreateDirectory(logPath);
                }
                if (File.Exists(filepath)) {
                    File.Move(Path.Combine(logPath, tempLogName),
                          Path.Combine(logPath, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_unknown_crash.log")
                    );
                }
                logWriter = File.CreateText(filepath);
                logWriter.Write("" +
                    "ID: " + SystemInfo.deviceUniqueIdentifier + "\n" +
                    "SystemOS: " + SystemInfo.operatingSystem + "\n" +
                    "CPU: " + SystemInfo.processorType + "\n" +
                    "GPU: " + SystemInfo.graphicsDeviceName + " " + SystemInfo.graphicsDeviceVersion + "\n" +
                    "RAM: " + SystemInfo.systemMemorySize + "\n" +
                    "DeviceModel: " + SystemInfo.deviceModel);
                int fCount = Directory.GetFiles(logPath, "*.log", SearchOption.TopDirectoryOnly).Length;
                if (fCount > 5) {
                    FileSystemInfo fileInfo = new DirectoryInfo(logPath)
                                 .GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First();
                    fileInfo.Delete();
                }
            }
        }

        internal string GetLogs() {
            string filepath = Path.Combine(logPath, tempLogName);
            logWriter.Flush();
            logWriter.Dispose();
            string upload = File.ReadAllText(filepath);
            logWriter = new StreamWriter(filepath);
            return upload;
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
            string log = 
                "<color=#" + color + ">"
                    + typestring + " <i>" + condition + "</i> " + 
                    (Application.isEditor && type == LogType.Error? "" : Environment.NewLine +"<size=9>" + stackTrace + "</size>")
                + "</color> ";
            if (writeToConsole == null) {
                logs.Add(log);
            }
            else {
                writeToConsole?.Invoke(log);
            }
            if (Application.isEditor == false) {
                if (string.IsNullOrEmpty(stackTrace) == false) {
                    stackTrace += Environment.NewLine;
                }
                logWriter.Write(Environment.NewLine
                                + type + "{ " + Environment.NewLine
                                + condition + Environment.NewLine
                                + stackTrace
                                + "}");
            }
        }

        internal void RegisterOnLogAdded(Action<string> writeToConsole) {
            this.writeToConsole += writeToConsole;
        }
        [HideInInspector]
        public IReadOnlyList<string> FirstLevelCommands = new List<string>{   
            "speed", "player", "maxfps", "city", 
            "graphy", "profiler", "unit", "ship", 
            "island", "spawn", "event", 
            "debugdata", "camera", "fogofwar"
        };
        /// <summary>
        /// Splits the command into its parts on whitespaces and then tries to execute the different level commands
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool HandleInput(string command) {
            if (command.Trim().Length <= 0) {
                return false;
            }
            command = command.ToLower();
            string[] parameters = command.Split(null); // splits  whitespaces
            if (parameters.Length < 1) {
                return false;
            }
            int realIndex = 0;
            for (int i = 0; i < parameters.Length; i++) {
                if (string.IsNullOrEmpty(parameters[i]))
                    continue;
                parameters[realIndex] = parameters[i].Trim();
                realIndex++;
            }
            parameters = parameters.Take(realIndex).ToArray();
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
                    if (happend)
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
                    happend = int.TryParse(parameters[1], out int num);
                    bool turn = num == 1;
                    CameraController.devCameraZoom = turn;
                    break;

                case "graphy":
                    happend = HandleGraphyCommands(parameters.Skip(1).ToArray());
                    break;

                case "debugdata":
                    UIController.Instance?.ToggleDebugData();
                    happend = true;
                    break;

                case "fogofwar":
                    var fogOfWar = GameObject.Find("FOW Canvas").transform.GetChild(0).gameObject;
                    fogOfWar.SetActive(!fogOfWar.activeSelf);
                    break;

                case "isgod":
                    MouseController.Instance.IsGod = !MouseController.Instance.IsGod;
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
                    happend = AddAllItems(c.Inventory);
                    break;

                default:
                    break;
            }
            return happend;
        }
        [HideInInspector]
        public IReadOnlyList<string> GraphyCommands = new List<string>
            { "full", "medium", "light", "fps", "switchmode" };
        private bool HandleGraphyCommands(string[] parameters) {
            if(parameters.Length == 0) {
                if(GraphyInstance != null) {
                    Destroy(GraphyInstance);
                }
                return true;
            }
            if (GraphyInstance == null) {
                GraphyInstance = Instantiate(GraphyPrefab);
                return true;
            }
            switch (parameters[0]) {
                case "full":
                    GraphyInstance.SetPreset(GraphyManager.ModulePreset.FPS_FULL_RAM_FULL_AUDIO_FULL_ADVANCED_FULL);
                    return true;

                case "medium":
                    GraphyInstance.SetPreset(GraphyManager.ModulePreset.FPS_FULL_RAM_FULL_AUDIO_FULL);
                    return true;

                case "light":
                    GraphyInstance.SetPreset(GraphyManager.ModulePreset.FPS_FULL);
                    return true;

                case "fps":
                    GraphyInstance.SetPreset(GraphyManager.ModulePreset.FPS_BASIC);
                    return true;

                case "switchmode":
                    GraphyInstance.ToggleModes();
                    return true;

                default:
                    return false;
            }
        }
        [HideInInspector]
        public IReadOnlyList<string> ShipCommands = new List<string>
            { "cannon" };

        private bool HandleShipCommands(string[] parameters) {
            Ship ship = MouseController.Instance.SelectedUnit as Ship;
            if (ship == null) {
                Debug.Log("no ship selected");
                return false;
            }
            switch (parameters[0]) {
                case "cannon":
                    return ShipAddCannon(parameters.Skip(1).ToArray(), ship);

                default:
                    break;
            }
            return false;
        }

        private bool ShipAddCannon(string[] parameters, Ship ship) {
            if (parameters.Length == 0)
                return false;
            int amount = -1;
            if (int.TryParse(parameters[0], out amount) == false) {
                return false;
            }
            ship.CannonItem.count = amount;
            return true;
        }
        [HideInInspector]
        public List<string> EventsCommands = new List<string>
            { "trigger", "stop" };

        private bool HandleEventCommands(string[] parameters) {
            if (parameters.Length < 1) {
                return false;
            }
            switch (parameters[0]) {
                case "trigger":
                    if (parameters.Length < 2) {
                        return false;
                    }
                    string id = parameters[1].Trim();
                    if (PrototypController.Instance.GameEventExists(id) == false) {
                        return false;
                    }
                    int player = -1;
                    if (parameters.Length == 3 && string.IsNullOrEmpty(parameters[2]) == false) {
                        int.TryParse(parameters[2], out player);
                    }
                    if(parameters.Length > 3 && parameters[3].StartsWith("s"))
                        return EventController.Instance.TriggerEventForEventable(new GameEvent(id), MouseController.Instance.CurrentlySelectedIGEventable);
                    if (player < 0)
                        return EventController.Instance.TriggerEvent(id);
                    else
                        return EventController.Instance.TriggerEventForPlayer(new GameEvent(id), PlayerController.GetPlayer(player));
                case "stop":
                    if (parameters.Length == 2 && string.IsNullOrEmpty(parameters[1]) == false && 
                            uint.TryParse(parameters[1], out uint gid)) {
                        return EventController.Instance.StopGameEvent(gid);
                    }
                    return false;
                case "list":
                    EventController.Instance.ListAllActiveEvents();
                    return true;
                default:
                    return false;
            }
        }
        [HideInInspector]
        public IReadOnlyList<string> SpawnCommands = new List<string>
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
                    World.Current.CreateUnit(u, PlayerController.GetPlayer(player), t);
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

        [HideInInspector]
        public IReadOnlyList<string> CityCommands = new List<string>
            { "item","fillitup","builditems", "name", "player", "effect" };

        private bool HandleCityCommands(string[] parameters) {
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
                    return ChangeItemInInventory(parameters.Skip(1).ToArray(), c.Inventory);

                case "fillitup":
                    return AddAllItems(c.Inventory);

                case "builditems":
                    return AddAllItems(c.Inventory, true);

                case "name":
                    break;

                case "player":
                    Debug.Log("Console Command not implemented!");
                    break;

                case "event":
                    return EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[pos + 1]), c);

                case "effect":
                    return HandleEffects(parameters.Skip(1).ToArray(), c);

                default:
                    break;
            }
            return false;
        }

        [HideInInspector]
        public IReadOnlyList<string> PlayerCommands = new List<string>
            { "change","money","diplomatic","effect" };

        private bool HandlePlayerCommands(string[] parameters) {
            switch (parameters[0]) {
                case "change":
                    return ChangePlayer(parameters.Skip(1).ToArray());

                case "money":
                    return ChangePlayerMoney(parameters.Skip(1).ToArray());

                case "diplomatic":
                    return ChangeWar(parameters.Skip(1).ToArray());

                case "effect":
                    if (parameters.Length < 3)
                        return false;
                    // anything can thats not a number can be the current player
                    if (int.TryParse(parameters[1], out int player) == false) {
                        return false;
                    }
                    return HandleEffects(parameters.Skip(2).ToArray(), PlayerController.GetPlayer(player));

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
            PlayerController.Instance.AddMoney(money, player);
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

        [HideInInspector]
        public IReadOnlyList<string> UnitCommands = new List<string>
            { "item","build","kill", "name", "player", "event", "effect" };

        private bool HandleUnitCommands(string[] parameters) {
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
                    u.Destroy(null);
                    return true;

                case "name":
                    u.SetName(parameters[pos + 1]);
                    return true;

                case "player":
                    if(int.TryParse(parameters[pos + 1], out int num)) {
                        u.playerNumber = num;
                    }
                    return true;

                case "event":
                    return EventController.Instance.TriggerEventForEventable(new GameEvent(parameters[pos + 1]), u);

                case "effect":
                    return HandleEffects(parameters.Skip(1).ToArray(), u);

                default:
                    break;
            }
            return false;
        }
        [HideInInspector]
        public IReadOnlyList<string> EffectsCommands = new List<string>  { "add","remove" };

        private bool HandleEffects(string[] parameters, IGEventable eventable) {
            if (PrototypController.Instance.EffectPrototypeDatas.ContainsKey(parameters[1]) == false)
                return false;
            switch(parameters[0]) {
                case "add":
                    return eventable.AddEffect(new Effect(parameters[1]));
                case "remove":
                    bool all = parameters.Length > 2 ? bool.TryParse(parameters[2], out _) : false;
                    return eventable.RemoveEffect(new Effect(parameters[1]), all);
                default:
                    return false;
            }
        }

        public bool ChangeItemInInventory(string[] parameters, Inventory inv) {
            string id = null;
            if (parameters.Length != 2) {
                return false;
            }
            id = parameters[0];

            if (int.TryParse(parameters[1], out int amount) == false) {
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
            Destroy(GraphyInstance);
            if (Application.isEditor == false) {
                logWriter.Flush();
                logWriter.Close();
                File.Move(Path.Combine(logPath, tempLogName), 
                          Path.Combine(logPath, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + SaveController.SaveName + ".log")
                );
            }
        }
    }
}