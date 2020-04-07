using System;
using UnityEngine;

public static class ExtensionMethods {
    public static T ToEnum<T>(this string value, bool ignoreCase = true) {
        return (T)Enum.Parse(typeof(T), value, ignoreCase);
    }
    public static Vector2 FloorToInt(this Vector2 v) {
        return new Vector2(Mathf.FloorToInt(v.x), Mathf.FloorToInt(v.y));
    }
    public static Vector2 CeilToInt(this Vector2 v) {
        return new Vector2(Mathf.CeilToInt(v.x), Mathf.CeilToInt(v.y));
    }
    public static Vector2 RoundToInt(this Vector2 v) {
        return new Vector2(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
    }
    public static bool IsFlooredVector(this Vector2 v, Vector2 other) {
        return FloorToInt(v) == FloorToInt(other);
    }
}
