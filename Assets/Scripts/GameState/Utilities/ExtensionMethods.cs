using System;
using System.Reflection;
using System.Xml;
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
    public static Vector2 Rotate(this Vector2 v, float degrees) {
        float sin = Mathf.Sin(degrees * Mathf.Deg2Rad);
        float cos = Mathf.Cos(degrees * Mathf.Deg2Rad);

        float tx = v.x;
        float ty = v.y;
        v.x = (cos * tx) - (sin * ty);
        v.y = (sin * tx) + (cos * ty);
        return v;
    }
    public static bool IsFlooredVector(this Vector2 v, Vector2 other) {
        return FloorToInt(v) == FloorToInt(other);
    }
    public static bool IsInBounds(this Vector2 v, int x, int y, int width, int height) {
        return v.x>=x&&v.y>=y&&v.x<width&&v.y<height;
    }
    public static T AddComponent<T>(this GameObject go, T toAdd) where T : Component {
        return go.AddComponent<T>().GetCopyOf(toAdd) as T;
    }

    public static T GetCopyOf<T>(this Component comp, T other) where T : Component {
        Type type = comp.GetType();
        if (type != other.GetType()) return null; // type mis-match
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
        PropertyInfo[] pinfos = type.GetProperties(flags);
        foreach (var pinfo in pinfos) {
            if (pinfo.IsDefined(typeof(ObsoleteAttribute), true))
                continue;
            if (pinfo.CanWrite) {
                try {
                    pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                }
                catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
            }
        }
        FieldInfo[] finfos = type.GetFields(flags);
        foreach (var finfo in finfos) {
            finfo.SetValue(comp, finfo.GetValue(other));
        }
        return comp as T;
    }

    public static int GetIntValue(this XmlElement xmlElement) {
        return int.Parse(xmlElement.InnerXml);
    }
    public static int GetIntValue(this XmlNode xmlElement) {
        return int.Parse(xmlElement.InnerXml);
    }

    public static bool IsBitSet(this byte b, int pos) {
        return (b & (1 << pos)) != 0;
    }
}
