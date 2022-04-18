using Andja.Controller;
using UnityEngine;

namespace Andja.UI.Menu {

    public enum NewGameSetting { Seed, Width, Height, Pirate, Fire, FogOfWar }

    public class NewGameSettings {

        public static void SetSeed(string value) {
            GameData.Instance.MapSeed = Mathf.Abs(value.GetHashCode());
        }

        public static void SetHeight(int value) {
            GameData.Height = Mathf.Abs(value);
        }

        public static void SetWidth(int value) {
            GameData.Width = Mathf.Abs(value);
        }

        public static void SetPirate(bool value) {
            GameData.pirates = value;
        }
        public static void SetFogOfWar(FogOfWarStyle mode) {
            GameData.FogOfWarStyle = mode;
        }

    }
}