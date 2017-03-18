using UnityEngine;
using System.Collections;

public class UnitHoldingScript : MonoBehaviour {
    public Unit unit;
	public int x;
	public int y;
	public float rot;
	//FIXME TODO REMOVE DIS
	public void Update(){
		if(unit==null){
			Destroy (this);
		}
		if(unit.pathfinding==null){
			return;
		}
		x=unit.pathfinding.currTile.X;
		y=unit.pathfinding.currTile.Y;
		rot =unit.pathfinding.rotation; 
	}
}
