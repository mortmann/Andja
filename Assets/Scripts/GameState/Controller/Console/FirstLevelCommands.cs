using Andja.Controller;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Andja.Controller { 
    public class FirstLevelCommands {
        private static Dictionary<GameObject, TransformData> _gameObjectGOtoPosition;

        public static ConsoleCommand[] GetFirstLevel() {
            return new ConsoleCommand[] {
                new ConsoleCommand("maxfps", SetMaxFps),
                new ConsoleCommand("devzoom", DevZoom),
                new ConsoleCommand("speed", ChangeSpeed),
                new ConsoleCommand("itsrainingbuildings", RainingStructures),
                new ConsoleCommand("itsdrainingbuildings", DrainingStructures),
            };
        }

        private static bool ChangeSpeed(string[] parameters) {
            if (float.TryParse(parameters[0], out float speed)) {
                WorldController.Instance.SetSpeed(speed);
                return true;
            }
            return false;
        }

        private static bool DrainingStructures(string[] arg) {
            if (_gameObjectGOtoPosition == null)
                return false;
            foreach (GameObject go in _gameObjectGOtoPosition.Keys) {
                if (go == null) {
                    continue;
                }
                go.transform.position = _gameObjectGOtoPosition[go].Position;
                go.transform.rotation = _gameObjectGOtoPosition[go].Rotation;
                UnityEngine.Object.Destroy(go.GetComponent<Rigidbody2D>());
            }
            return true;
        }


        private static bool RainingStructures(string[] arg) {
            //easteregg!
            _gameObjectGOtoPosition = new Dictionary<GameObject, TransformData>();
            BoxCollider2D[] all = UnityEngine.Object.FindObjectsOfType<BoxCollider2D>();
            foreach (BoxCollider2D b2d in all) {
                if (b2d.gameObject.GetComponent<Rigidbody2D>() != null) {
                    continue;
                }
                _gameObjectGOtoPosition.Add(b2d.gameObject, new TransformData(b2d.gameObject.transform.position, b2d.gameObject.transform.rotation));
                Rigidbody2D rb2 = b2d.gameObject.AddComponent<Rigidbody2D>();
                rb2.gravityScale = UnityEngine.Random.Range(0.6f, 2.7f);
                rb2.inertia = UnityEngine.Random.Range(0.5f, 1.5f);
            }
            return true;
        }

        private static bool DevZoom(string[] parameters) {
            if (int.TryParse(parameters[1], out int num)) {
                CameraController.devCameraZoom = Convert.ToBoolean(num);
                return true;
            }
            if (bool.TryParse(parameters[1], out bool change)) {
                CameraController.devCameraZoom = change;
                return true;
            }
            return false;
        }

        private static bool SetMaxFps(string[] parameters) {
            if (int.TryParse(parameters[1], out int fps)) {
                Application.targetFrameRate = Mathf.Clamp(fps, -1, 1337);
                return true;
            }
            return false;
        }
        private struct TransformData {
            public Vector3 Position;
            public Quaternion Rotation;

            public TransformData(Vector3 position, Quaternion rotation) {
                Position = position;
                Rotation = rotation;
            }
        }
    }

}