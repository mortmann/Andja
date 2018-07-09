using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class IslandPathfinding : Pathfinding {

	Tile start;
	public IslandPathfinding() : base() {
	}
	public IslandPathfinding(Unit u, Tile start){
		this._speed = u.Speed;
		this.rotationSpeed = u.RotationSpeed;
		CurrTile = start;
	}

	public override void SetDestination(Tile end) {
		this.start = this.CurrTile;
		this.DestTile = end;
		pathDest = Path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	public void SetDestination(Vector3 end) {
		this.start = this.CurrTile;
		this.DestTile = World.Current.GetTileAt(end.x,end.y);
		dest_X = end.x;
		dest_Y = end.y;
		pathDest = Path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	public override void SetDestination(float x, float y) {
		this.start = this.CurrTile;
		this.DestTile = World.Current.GetTileAt(x,y);
		dest_X = x;
		dest_Y = y;
		pathDest = Path_dest.exact;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	protected override void CalculatePath(){
        if (start == null)
            start = World.Current.GetTileAt(X, Y);
		Path_AStar pa = new Path_AStar (start.MyIsland,start,DestTile);
		worldPath = pa.path;
		CreateReversePath ();
        if (worldPath.Count > 0) {
            worldPath.Dequeue();
        }
        if (worldPath.Count > 0) {
            nextTile = worldPath.Dequeue();
        }

    }


}
