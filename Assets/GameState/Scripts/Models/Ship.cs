using UnityEngine;
using System.Collections.Generic;

public class Ship : Unit {
	public Transform transform;
	public TradeRoute tradeRoute;
	public Rigidbody2D r2d;
	public Ship(Tile t,int playernumber){
		this.playerNumber = playernumber;
		inventory = new Inventory (6, "SHIP");
		isShip = true;
		startTile = t;
		pathfinding = new Pathfinding (speed, startTile);
		speed = 2f;
	}


	public override void Update (float deltaTime){
		if(myGameobject==null){
			return;
		}
		//TRADEROUTE
		if(tradeRoute!=null&&tradeRoute.Valid){
			if(pathfinding.IsAtDest&&tradeRoute.isStarted){
				//do trading here
				//take some time todo that
				if(tradeTime<0){
					tradeRoute.doCurrentTrade (this);
					tradeTime = 1.5f;
					//then get a next destination
					AddMovementCommand (tradeRoute.getNextDestination ());	
				} else {
					tradeTime -= deltaTime;
				}
			} 
			if(tradeRoute.isStarted==false){
				//start the route
				AddMovementCommand (tradeRoute.getNextDestination ());
			}
		}
		//PAROL
		UpdateParol ();

		r2d.MovePosition (transform.position + pathfinding.Update_DoMovement(deltaTime));
		r2d.MoveRotation (transform.rotation.z + pathfinding.UpdateRotation ());
		if(hasChanged){
			if (cbUnitChanged != null)
				cbUnitChanged(this);
		}
	}
	protected override void OverrideCurrentMission (){
		onWayToPatrolTarget = false;
		onPatrol = false;
		if (tradeRoute != null)
			tradeRoute.isStarted = false;
	}
	public override void SetGameObject (GameObject go)	{
		myGameobject = go; //what?
		myGameobject.transform.position = new Vector3(startTile.X,startTile.Y,0);
		transform = myGameobject.transform;
		pathfinding.transform = transform;
		r2d = myGameobject.GetComponent<Rigidbody2D>();
		r2d.MoveRotation (pathfinding.rotation);
	}

	public override void AddMovementCommand (float x, float y){
		Tile tile = World.current.GetTileAt(x, y);
		if(tile == null){
			return;
		}
		if (tile.Type != TileType.Water) {
			return;
		}
		onPatrol = false;
		pathfinding.AddMovementCommand( x, y);

	}
}
