using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public enum UnitType{ship,land};
public class Unit : IXmlSerializable {
   
	public int playerNumber;
	public UserStructure rangeUStructure;
	protected GameObject myGameobject;
	public Transform transform;
	public Tile startTile;

	public TradeRoute tradeRoute;

	float speed;   // Tiles per second



	Action<Unit> cbUnitChanged;

	public Inventory inventory;

	internal float width;
	internal float height;
	private Pathfinding pathfinding;

	public Rigidbody2D r2d;

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


	public bool isShip;
	public bool hasChanged = false;


	public Unit(Tile t,UnitType ut=UnitType.ship) {
		speed = 2f;
		inventory = new Inventory (6,"SHIP");
        isShip = true;
		startTile = t;
		pathfinding = new Pathfinding (speed, startTile);

    }
	public void SetGameObject(GameObject go){
		myGameobject = go;
		myGameobject.transform.position = new Vector3(startTile.X,startTile.Y,0);
		transform = myGameobject.transform;
		pathfinding.transform = transform;
		r2d = myGameobject.GetComponent<Rigidbody2D>();
		r2d.MoveRotation (pathfinding.rotation);
	}


    public GameObject GetGameObject() {
		return myGameobject;
    }


	public void Update(float deltaTime) {
		if(myGameobject==null){
			return;
		}
		if(tradeRoute!=null){
			if(pathfinding.currTile==tradeRoute.getCurrentDestination ()){
				//do trading here
				//take some time todo that

				//then get a next destination
				AddMovementCommand (tradeRoute.getNextDestination ());
			} else {
				//start the route
				AddMovementCommand (tradeRoute.getNextDestination ());
			}
		}
		r2d.MovePosition (transform.position + pathfinding.Update_DoMovement(deltaTime));
		r2d.MoveRotation (transform.rotation.z + pathfinding.UpdateRotation ());
		if(hasChanged){
	        if (cbUnitChanged != null)
	            cbUnitChanged(this);
		}
    }

	public void isInRangeOfWarehouse(UserStructure ware){
		if(ware!=null){
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

	public void AddMovementCommand(Tile t) {
		if(t==null){
			//not really an error it can happen
			Debug.LogWarning ("AddMovementCommand |This Tile is null -- REMOVE THIS AFTER DEBUGGING!");
			return;
		} else {
			AddMovementCommand (t.X,t.Y);
		}
	}
    public void AddMovementCommand(float x, float y) {
		Tile tile = World.current.GetTileAt(x, y);
        if(tile == null){
            return;
        }
        if (isShip) {
            if (tile.Type != TileType.Water) {
                return;
            }
        } else {
            if (tile.Type == TileType.Water) {
                return;
            }
            if (tile.Type == TileType.Mountain) {
                return;
            }
        }
//        Debug.Log("AddMovementCommand " + tile.toString());
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
		reader.ReadToDescendant ("Inventory");
		inventory = new Inventory ();
		inventory.ReadXml (reader);


		AddMovementCommand (x, y);

	}
}
