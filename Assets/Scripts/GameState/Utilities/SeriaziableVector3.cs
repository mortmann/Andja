using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class SeriaziableVector3 {

    [JsonPropertyAttribute] float X;
    [JsonPropertyAttribute] float Y;
    [JsonPropertyAttribute] float Z;

    [JsonIgnore]
    public Vector3 Vec {
        get { return new Vector3(X, Y, Z); }
        set { X = value.x; Y = value.y; Z = value.z; }
    }
    public SeriaziableVector3(Vector3 vec) {
        X = vec.x;
        Y = vec.y;
        Z = vec.z;
    }
    public SeriaziableVector3() { }
    public static implicit operator Vector2(SeriaziableVector3 v) { return v.Vec; }
    public static implicit operator SeriaziableVector3(Vector2 v) { return new SeriaziableVector3(v); }
    public static implicit operator SeriaziableVector3(Vector3 v) { return new SeriaziableVector3(v); }
    public static implicit operator Vector3(SeriaziableVector3 v) { return v.Vec; }

}
