using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum NewGameSetting { Seed, Width, Height, Pirate, Fire }
public class NewGameSettings {
    public static void SetSeed(string value) {
        GameDataHolder.Instance.MapSeed = Mathf.Abs(value.GetHashCode());
    }
    public static void SetHeight(int value) {
        GameDataHolder.Height = Mathf.Abs(value);
    }
    public static void SetWidth(int value) {
        GameDataHolder.Width = Mathf.Abs(value);
    }
    public static void SetPirate(bool value) {
        GameDataHolder.pirates = value;
    }
    public static void SetFire(bool value) {
        GameDataHolder.fire = value;
    }
}
