using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using EpPathFinding.cs;
using UnityEngine;

public class OceanPathfinding : Pathfinding {
	Tile start; 

	StaticGrid tileGrid;

	public OceanPathfinding(){
	}

	public OceanPathfinding(Tile t, Ship s){
		_speed = s.speed;
		rotationSpeed = s.rotationSpeed;
		currTile = t;
	}


	public void SetDestination(Tile end){
		dest_X = end.X;
		dest_Y = end.Y;
		this.start = currTile;
		this.destTile = end;
		tileGrid = World.current.TilesGrid;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	public void SetDestination(float x , float y){
		pathDest = path_dest.exact;
		dest_X = x;
		dest_Y = y;
		this.start = currTile;
		this.destTile = World.current.GetTileAt(x, y);
		tileGrid = World.current.TilesGrid;
		Thread calcPath = new Thread (CalculatePath);
		calcPath.Start ();
	}
	protected override void CalculatePath(){
		JumpPointParam jpParam = new JumpPointParam(tileGrid,new GridPos(start.X,start.Y), new GridPos(destTile.X,destTile.Y),true,DiagonalMovement.OnlyWhenNoObstacles);
		List<GridPos> pos = JumpPointFinder.FindPath (jpParam);
		worldPath = new Queue<Tile> ();
		for (int i =0; i < pos.Count; i++) {
			worldPath.Enqueue (World.current.GetTileAt (pos [i].x, pos [i].y));
		}
		CreateReversePath ();
	}
}
