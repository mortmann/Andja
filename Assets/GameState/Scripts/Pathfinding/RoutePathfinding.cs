using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoutePathfinding : Pathfinding {

	public RoutePathfinding(List<Tile> startTiles, List<Tile> endTiles) {
		float minDist = float.MaxValue;
		foreach(Tile st in startTiles){
			if(st.Structure==null || st.Structure.GetType ()!=typeof(Road)){
				continue;
			}
			Road r = st.Structure as Road;
			foreach(Tile et in endTiles){
				
			}


		}	
	


	}



}
