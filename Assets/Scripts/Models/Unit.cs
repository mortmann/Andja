using UnityEngine;
using System.Collections;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

public class Unit : MonoBehaviour, IXmlSerializable {
   
	public UserStructure rangeUStructure;

    void Start() {
		speed = 2f;
		pathfinding = new Pathfinding (transform, speed, WorldController.Instance.world.GetTileAt(35, 35));


        transform.Translate(new Vector3(35,35, 0));
		inventory = new Inventory (6);
        isShip = true;
		r2d = GetComponent<Rigidbody2D>();

    }

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
    // If we aren't moving, then destTile = currTile


    public GameObject GetGameObject() {
        return gameObject;
    }


    float speed;   // Tiles per second



    Action<Unit> cbUnitChanged;

    public Inventory inventory;

    internal float width;
    internal float height;
    

    public void FixedUpdate() {
		r2d.MovePosition (transform.position + pathfinding.Update_DoMovement(Time.deltaTime));
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
		if(rangeUStructure != null && rangeUStructure is Warehouse){
			rangeUStructure.city.tradeFromShip (clicked);
		}
	}
    public void RegisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged += cb;
    }

    public void UnregisterOnChangedCallback(Action<Unit> cb) {
        cbUnitChanged -= cb;
    }

    public void AddMovementCommand(float x, float y) {
        Tile tile = WorldController.Instance.world.GetTileAt(x, y);
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
        Debug.Log("AddMovementCommand " + tile.toString());
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
		if (inventory != null) {
			writer.WriteStartElement ("Inventory");
			inventory.WriteXml (writer);
			writer.WriteEndElement ();
		}
		writer.WriteAttributeString("currTile_X", pathfinding.currTile.X.ToString () );
		writer.WriteAttributeString("currTile_Y", pathfinding.currTile.Y.ToString () );
		writer.WriteAttributeString("dest_X", pathfinding.dest_X.ToString () );
		writer.WriteAttributeString("dest_Y", pathfinding.dest_Y.ToString () );

	}
	public void ReadXml (XmlReader reader){
		isShip = bool.Parse(reader.GetAttribute ("IsShip"));
		int x = int.Parse( reader.GetAttribute("destTile_X") );
		int y = int.Parse( reader.GetAttribute("destTile_Y") );
		AddMovementCommand (x, y);

	}
}
