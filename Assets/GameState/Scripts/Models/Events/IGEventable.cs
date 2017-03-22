using UnityEngine;
using System;

public interface IGEventable {
	void RegisterOnEvent (Action<GameEvent> create, Action<GameEvent> ending);
	int GetPlayerNumber();
//	bool IsTarget (IGEventable target);
	/// <summary>
	/// Gets the type of the target.
	/// 1 = Player
	/// 10 = World
	/// 11 = island
	/// 12 = city
	/// 100 + StructureID = Specific Structure
	/// Gets only the generic Type.
	/// </summary>
	/// <returns>The target type.</returns>
	int GetTargetType();
}
