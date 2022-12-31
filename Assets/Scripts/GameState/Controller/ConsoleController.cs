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
        private GraphyManager _graphyInstance;
        public GraphyManager GraphyInstance {
            get {
                if (_graphyInstance == null) {
                    _graphyInstance = Instantiate(GraphyPrefab);
                }
                return _graphyInstance;
            }
            set {
                _graphyInstance = value;
            }
        }

        public static bool DEBUG_MODE { get; internal set; }

        public static List<string> logs = new List<string>();
        private Action<string> _writeToConsole;
        private StreamWriter _logWriter;

        private const string TempLogName = "temp.log";
        private static string _logPath = "";

        public ConsoleCommand EntryCommand;

        public void OnEnable() {
            Instance = this;
            EntryCommand = ConsoleCommand.GetEntryCommand();
            Application.logMessageReceived += LogCallbackHandler;
            if (Application.isEditor) return;
            _logPath = Path.Combine(SaveController.GetSaveGamesPath(), "logs");
            string filepath = Path.Combine(_logPath, TempLogName);
            if (Directory.Exists(_logPath) == false) {
                Directory.CreateDirectory(_logPath);
            }
            if (File.Exists(filepath)) {
                File.Move(Path.Combine(_logPath, TempLogName),
                    Path.Combine(_logPath, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_unknown_crash.log")
                );
            }
            _logWriter = File.CreateText(filepath);
            _logWriter.Write("" +
                             "ID: " + SystemInfo.deviceUniqueIdentifier + "\n" +
                             "SystemOS: " + SystemInfo.operatingSystem + "\n" +
                             "CPU: " + SystemInfo.processorType + "\n" +
                             "GPU: " + SystemInfo.graphicsDeviceName + " " + SystemInfo.graphicsDeviceVersion + "\n" +
                             "RAM: " + SystemInfo.systemMemorySize + "\n" +
                             "DeviceModel: " + SystemInfo.deviceModel + "\n" +
                             "Graphics API: " + SystemInfo.graphicsDeviceType);
            int fCount = Directory.GetFiles(_logPath, "*.log", SearchOption.TopDirectoryOnly).Length;
            if (fCount <= 5) return;
            FileSystemInfo fileInfo = new DirectoryInfo(_logPath)
                .GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First();
            fileInfo.Delete();
        }

        internal string GetLogs() {
            string upload;
            if (Application.isEditor == false) {
                string filepath = Path.Combine(_logPath, TempLogName);
                _logWriter.Flush();
                FileStream fs = File.Open(filepath,
                            FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                upload = new StreamReader(fs, System.Text.Encoding.Default).ReadToEnd();
            }
            else {
                upload = string.Join(Environment.NewLine, logs);
            }
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
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
            string log =
                "<color=#" + color + ">"
                    + typestring + " <i>" + condition + "</i> " +
                    (Application.isEditor && type == LogType.Error ? "" : Environment.NewLine + "<size=9>" + stackTrace + "</size>")
                + "</color> ";
            if (_writeToConsole == null) {
                logs.Add(log);
            }
            else {
                _writeToConsole?.Invoke(log);
            }

            if (Application.isEditor) return;
            if (string.IsNullOrEmpty(stackTrace) == false) {
                stackTrace += Environment.NewLine;
            }
            _logWriter.Write(Environment.NewLine
                             + type + "{" + Environment.NewLine
                             + condition + Environment.NewLine
                             + stackTrace.TrimEnd(Environment.NewLine.ToCharArray())
                             + "}");
        }

        internal void RegisterOnLogAdded(Action<string> writeToConsole) {
            this._writeToConsole += writeToConsole;
        }
        internal bool HandleInput(string command) {
            return EntryCommand.Do(command.Split(' '));
        }

        public void OnDestroy() {
            Instance = null;
            Destroy(GraphyInstance);
            if (Application.isEditor) return;
            _logWriter.WriteLine("Closed safely.");
            _logWriter.Flush();
            _logWriter.Close();
            File.Move(Path.Combine(_logPath, TempLogName), 
                Path.Combine(_logPath, DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + SaveController.SaveName + ".log")
            );
        }

        
    }
}