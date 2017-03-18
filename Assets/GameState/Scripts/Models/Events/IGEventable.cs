using UnityEngine;
using System;

public interface IGEventable {
	void RegisterOnEvent (Action<GameEvent> create, Action<GameEvent> ending);
	int GetPlayerNumber();
//	bool IsTarget (IGEventable target);
	int GetTarget();
}
