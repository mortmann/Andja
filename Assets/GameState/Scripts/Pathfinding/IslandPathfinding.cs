using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class IslandPathfinding : Pathfinding {

	Tile start;
	public IslandPathfinding(){
	}
	public IslandPathfinding(Unit u, Tile start){
		this._speed = u.speed;
		this.rotationSpeed = u.rotationSpeed;
		currTile = start;
	}

	public void SetDestination(Tile end) {
		this.start = this.currTile;
		this.destTile = end;
		pathDest = path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	public void SetDestination(Vector3 end) {
		this.start = this.currTile;
		this.destTile = World.current.GetTileAt(end.x,end.y);
		dest_X = end.x;
		dest_Y = end.y;
		pathDest = path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	public void SetDestination(float x, float y) {
		this.start = this.currTile;
		this.destTile = World.current.GetTileAt(x,y);
		dest_X = x;
		dest_Y = y;
		pathDest = path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	protected override void CalculatePath(){
		Path_AStar pa = new Path_AStar (start.myIsland,start,destTile);
		worldPath = pa.path;
		CreateReversePath ();
	}


}
