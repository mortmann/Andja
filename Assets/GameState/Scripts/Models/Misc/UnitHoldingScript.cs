﻿using UnityEngine;
using System.Collections;

public class UnitHoldingScript : MonoBehaviour {
    public Unit unit;
	public int x;
	public int y ;

	//FIXME TODO REMOVE DIS
	public void Update(){
		x= unit.pathfinding.currTile.X;
		y=unit.pathfinding.currTile.Y;
	}
}
