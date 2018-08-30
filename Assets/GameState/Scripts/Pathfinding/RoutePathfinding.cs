using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

public class RoutePathfinding : Pathfinding {

	List<Tile> startTiles;
	List<Tile> endTiles;
    public RoutePathfinding() : base() { }
    /// <summary>
    /// Preferrably those list ONLY contains tiles WITH roads build on it for
    /// Performance reasons. But it checks if there is a route
    /// </summary>
    /// <param name="startTiles">Start tiles.</param>
    /// <param name="endTiles">End tiles.</param>
    public void SetDestination(List<Tile> startTiles, List<Tile> endTiles) {
		this.startTiles = startTiles;
		this.endTiles = endTiles;

		myTurnType = Turn_type.OnPoint;
        Thread calcPath = new Thread(CalculatePath);
        calcPath.Start();

    }

    public override void SetDestination(Tile end) {
        DestTile = end;
        Thread calcPath = new Thread(CalculatePath);
        calcPath.Start();

    }

    public override void SetDestination(float x, float y) {
        //get the tiles from the world to get a current reference and not an empty from the load
        DestTile  = World.Current.GetTileAt(x, y);
        startTile = World.Current.GetTileAt(startTile.X, startTile.Y);
        Thread calcPath = new Thread(CalculatePath);
        calcPath.Start();

    }

    protected override void CalculatePath(){
        pathDest = Path_dest.tile;

        if (startTiles==null){
            startTiles = new List<Tile> {
                startTile
            };
            endTiles = new List<Tile> {
                DestTile
            };
        }
		Queue<Tile> currentQueue=null;
		List<Route> checkedRoutes = new List<Route> ();
		IsAtDestination = false;
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
		if(startTile!=null&&startTile!=CurrTile){
			CreateReversePath ();
			while(worldPath.Peek () != CurrTile) {
				// remove as long as it is not the current tile 
				worldPath.Dequeue ();
			}
            worldPath.Dequeue();

        }
        else {
			CurrTile = worldPath.Peek ();
			startTile = CurrTile;
			CreateReversePath ();
			DestTile = backPath.Peek ();
		}
        worldPath.Dequeue();

        dest_X = DestTile.X;
        dest_Y = DestTile.Y;
    }

}
