using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	public OutputStructure contact;
	//dont know why this aint working
	void OnCollisionEnter2D(Collision2D coll) {
		Debug.Log ("Collision"); 
		Unit u = coll.gameObject.GetComponent<Unit> ();
		if (u != null) {
			u.isInRangeOfWarehouse (contact);
			((Warehouse)contact).addUnitToTrade (u);
		}
	}

	void OnTriggerEnter2D(Collider2D coll) {
		Unit u = coll.gameObject.GetComponent<UnitHoldingScript> ().unit;
		if (u != null) {
			u.isInRangeOfWarehouse (contact);
			((Warehouse)contact).addUnitToTrade (u);
		}
	}
	void OnCollisionExit2D(Collision2D coll) {
		Unit u = coll.gameObject.GetComponent<UnitHoldingScript> ().unit;
		if (coll.gameObject.GetComponent<UnitHoldingScript> () != null) {
			u.isInRangeOfWarehouse (null);
			((Warehouse)contact).removeUnitFromTrade (u);
		}
	}
}
