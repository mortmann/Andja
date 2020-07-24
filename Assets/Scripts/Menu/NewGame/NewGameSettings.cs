using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NewGameSetting { Seed, Width, Height, Pirate, Fire }
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

}
