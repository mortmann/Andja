using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Unit : IXmlSerializable {
   
	public int playerNumber;
	public OutputStructure rangeUStructure;
	protected GameObject myGameobject;
	public Tile startTile;

	public float currHealth=50;
	public int maxHP=50;
	public float attackDistance;
	public float damage;
	public float damageReduction;
	public float damageMultiplier;

	protected float speed;   // Tiles per second
	protected Action<Unit> cbUnitChanged;

	public Inventory inventory;

	protected internal float width;
	protected internal float height;
	public Pathfinding pathfinding;


	public float X {
		get {
			return pathfinding.X;
		}
	}
	public float Y {
		get {
			return pathfinding.Y;
		}
	}
	public Vector2 patrolTarget;
	public Vector2 patrolStart;
	public bool onWayToPatrolTarget; // false for targetPatrol, true for patrolstart
	public bool onPatrol = false;

	public bool isShip;

	public bool hasChanged = false;

	public float tradeTime=1.5f;


	public Unit(Tile t,int playernumber) {
		this.playerNumber = playernumber;
		speed = 2f;
		startTile = t;
		pathfinding = new Pathfinding (speed, startTile,path_mode.islandSingleStartpoint);
    }
	public Unit(){
	}
	public virtual void SetGameObject(GameObject go){
		myGameobject = go;
		myGameobject.name = "Unit " + UnityEngine.Random.Range (0, 1000000000);
		myGameobject.transform.position = new Vector3(startTile.X,startTile.Y,0);
	}


    public GameObject GetGameObject() {
		return myGameobject;
    }


	public virtual void Update(float deltaTime) {
		if(myGameobject==null){
			return;
		}
		//PATROL
		UpdateParol ();
		UpdateAggroRange ();
		pathfinding.Update_DoMovement (deltaTime);
		pathfinding.UpdateRotation ();
		myGameobject.transform.position = new Vector3 (X, Y, -0.1f);
		if(hasChanged){
	        if (cbUnitChanged != null)
	            cbUnitChanged(this);
		}
    }
	protected void UpdateAggroRange(){
		//Collider2D[] c2d = Physics2D.OverlapCircleAll (new Vector2(X,Y),2);
		//foreach (var item in c2d) {
//			if(item!=null&&item.gameObject!=myGameobject)
//				Debug.Log ("my " +myGameobject + " hit " + item.gameObject); 	
		//}

	}
	protected void UpdateParol(){
		//PATROL
		if(onPatrol){
			if(pathfinding.IsAtDest){
				if (onWayToPatrolTarget) {
					pathfinding.AddMovementCommand (patrolStart.x , patrolStart.y);
				} else {
					pathfinding.AddMovementCommand (patrolTarget.x , patrolTarget.y);
				}
				onWayToPatrolTarget = !onWayToPatrolTarget; 
			}
		}
	}
	public void isInRangeOfWarehouse(OutputStructure ware){
		if(ware==null){
			Debug.LogError ("WARNING Range warehouse null"); 
			return;
		}
		rangeUStructure = ware; 
	}
	public void clickedItem(Item clicked){
		Debug.Log (clicked.ToString ()); 
		if(rangeUStructure != null && rangeUStructure is Warehouse){
			rangeUStructure.City.tradeFromShip (this,clicked);
		}
	}
    public void RegisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged -= cb;
    }
	public void AddPatrolCommand(float targetX,float targetY){
		Tile tile = World.current.GetTileAt(targetX, targetY);
		if(tile == null){
			return;
		}
		if (tile.Type == TileType.Ocean) {
			return;
		}
		if (tile.Type == TileType.Mountain) {
			return;
		}
		OverrideCurrentMission ();
		onWayToPatrolTarget = true;
		onPatrol = true;
		patrolStart = new Vector2 (X, Y);
		patrolTarget = new Vector2 (targetX, targetY);
		pathfinding.AddMovementCommand(targetX, targetY);

	}
	protected virtual void OverrideCurrentMission(){
		onWayToPatrolTarget = false;
		onPatrol = false;
	}

	public void AddMovementCommand(Tile t) {
		if(t==null){
			//not really an error it can happen
			return;
		} else {
			if(t==pathfinding.currTile){
				return;
			}
			AddMovementCommand (t.X,t.Y);
		}
	}
    public virtual void AddMovementCommand(float x, float y) {
		Tile tile = World.current.GetTileAt(x, y);
        if(tile == null){
            return;
        }
        if (tile.Type == TileType.Ocean) {
            return;
        }
        if (tile.Type == TileType.Mountain) {
            return;
        }
		if(pathfinding.currTile.myIsland!=tile.myIsland){
			return;
		}
		onPatrol = false;
		pathfinding.AddMovementCommand( x, y);
    }
	public int tryToAddItemMaxAmount(Item item , int amount){
		Item t = item.Clone ();
		t.count = amount;
		return inventory.addItem (t);
	}


	//////////////////////////////////////////////////////////////////////////////////////
	/// 
	/// 						SAVING & LOADING
	/// 
	//////////////////////////////////////////////////////////////////////////////////////
	public XmlSchema GetSchema() {
		return null;
	}
	public void WriteXml(XmlWriter writer){
		writer.WriteAttributeString("IsShip", isShip.ToString () );
		writer.WriteAttributeString("playernumber", playerNumber.ToString () );
		writer.WriteAttributeString("currTile_X", pathfinding.currTile.X.ToString () );
		writer.WriteAttributeString("currTile_Y", pathfinding.currTile.Y.ToString () );
		writer.WriteAttributeString("dest_X", pathfinding.dest_X.ToString () );
		writer.WriteAttributeString("dest_Y", pathfinding.dest_Y.ToString () );
		writer.WriteAttributeString("rotation", pathfinding.rotation.ToString () );
		if (inventory != null) {
			writer.WriteStartElement ("Inventory");
			inventory.WriteXml (writer);
			writer.WriteEndElement ();
		}

	}
	public void ReadXml (XmlReader reader){
		
		isShip = bool.Parse(reader.GetAttribute ("IsShip"));
		float x = float.Parse( reader.GetAttribute("dest_X") );
		float y = float.Parse( reader.GetAttribute("dest_Y") );
		float rot = float.Parse( reader.GetAttribute("rotation") );
		pathfinding.rotation = rot;
		if (reader.ReadToDescendant ("Inventory")) {
			inventory = new Inventory ();
			inventory.ReadXml (reader);
		}

		AddMovementCommand (x, y);

	}
}
