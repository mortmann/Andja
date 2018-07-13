using UnityEngine;
using Newtonsoft.Json;

[JsonObject(MemberSerialization.OptIn)]
public class SeriaziableVector3 {

	[JsonPropertyAttribute] float X;
	[JsonPropertyAttribute] float Y;
	[JsonPropertyAttribute] float Z;

	public SeriaziableVector3(Vector3 vec){
		X = vec.x;
		Y = vec.y;
		Z = vec.z;
	}
	public Vector3 GetVector3(){
		return new Vector3 (X, Y, Z);
	}

}
