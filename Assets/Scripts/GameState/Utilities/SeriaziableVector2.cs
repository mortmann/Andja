using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

[JsonObject(MemberSerialization.OptIn)]
public class SeriaziableVector2 {

    [JsonPropertyAttribute] float X;
    [JsonPropertyAttribute] float Y;

    [JsonIgnore]
    public Vector2 Vec {
        get { return new Vector2(X, Y); }
        set { X = value.x; Y = value.y; }
    }
    public SeriaziableVector2(Vector2 vec) {
        X = vec.x;
        Y = vec.y;
    }
    public SeriaziableVector2(Vector3 vec) {
        X = vec.x;
        Y = vec.y;
    }
    public SeriaziableVector2() { }
    public static implicit operator Vector2(SeriaziableVector2 v) { return v.Vec; }
    public static implicit operator SeriaziableVector2(Vector2 v) { return new SeriaziableVector2(v); }
    public static implicit operator SeriaziableVector2(Vector3 v) { return new SeriaziableVector2(v); }

    public override bool Equals(object obj) {
        // If parameter cannot be cast to War return false:
        SeriaziableVector2 other = obj as SeriaziableVector2;
        if (other == null) {
            return false;
        }
        // Return true if the fields match:
        return Vec == other.Vec;
    }

    public override int GetHashCode() {
        var hashCode = -464161712;
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<Vector2>.Default.GetHashCode(Vec);
        return hashCode;
    }
}