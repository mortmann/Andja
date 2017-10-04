using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RoutePathfinding : Pathfinding {

	List<Tile> startTiles;
	List<Tile> endTiles;

	/// <summary>
	/// Preferrably those list ONLY contains tiles WITH roads build on it for
	/// Performance reasons. But it checks if there is a route
	/// </summary>
	/// <param name="startTiles">Start tiles.</param>
	/// <param name="endTiles">End tiles.</param>
	public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
		this.startTiles = startTiles;
		this.endTiles = endTiles;

		myTurnType = turn_type.OnPoint;
//		Thread calcPath = new Thread (CalculatePath);
//		calcPath.Start ();
		CalculatePath();
	}

	protected override void CalculatePath(){
		if(startTiles==null){
			startTiles = new List<Tile> ();
			startTiles.Add (startTile);
			endTiles = new List<Tile> ();
			endTiles.Add (destTile);
		}
		Queue<Tile> currentQueue=null;
		List<Route> checkedRoutes = new List<Route> ();
		IsAtDest = false;
		foreach(Tile st in startTiles){
			if(st.Structure==null || st.Structure.GetType ()!=typeof(Road)){
				continue;
			}
			Road r1 = st.Structure as Road;
			foreach(Tile et in endTiles){
				if(et.Structure.GetType ()!=typeof(Road)){
					continue;
				}
				Road r2 = et.Structure as Road;
				if(r1.Route != r2.Route){
					continue;
				}
				if(checkedRoutes.Contains(r1.Route)==false){
					Path_AStar pa = new Path_AStar (r1.Route, st, et, startTiles, endTiles);
					checkedRoutes.Add (r1.Route);
					if(pa.path == null || currentQueue!=null && currentQueue.Count<pa.path.Count ){
						continue;
					}
					currentQueue = pa.path;
				} 
			}
		}
		worldPath = currentQueue;
		if(worldPath==null||worldPath.Count==0){
			return;
		}
		if(startTile!=null&&startTile!=currTile){
			CreateReversePath ();
			while(worldPath.Peek () != currTile) {
				// remove as long as it is not the current tile 
				worldPath.Dequeue ();
			} 



		} else {
			currTile = worldPath.Peek ();
			startTile = currTile;
			CreateReversePath ();
			destTile = backPath.Peek ();
		}

	}

}
