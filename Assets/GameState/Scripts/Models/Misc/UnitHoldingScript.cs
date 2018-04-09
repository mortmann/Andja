using UnityEngine;
using System.Collections;

public class UnitHoldingScript : MonoBehaviour {
    public Unit unit;
	public int x;
	public int y;
	public float rot;
	public int playerNumber;
	//FIXME TODO REMOVE DIS
	public void Update(){
		if(unit==null){
			Destroy (this);
		}
		if(unit.pathfinding==null){
			return;
		}
		if(unit.pathfinding.worldPath!=null){
			LineRenderer line = gameObject.GetComponentInChildren<LineRenderer>();
			line.positionCount = unit.pathfinding.worldPath.Count+1;
			line.useWorldSpace = true;
			int s = 0;
			foreach(Tile t in unit.pathfinding.worldPath){
				line.SetPosition (s, t.Vector + Vector3.back);
				s++;
			}
			line.SetPosition (s, new Vector3 (unit.pathfinding.dest_X, unit.pathfinding.dest_Y,-1));
		}
		x=unit.pathfinding.currTile.X;
		y=unit.pathfinding.currTile.Y;
		rot =unit.pathfinding.rotation; 
		playerNumber = unit.playerNumber;
	}
}
