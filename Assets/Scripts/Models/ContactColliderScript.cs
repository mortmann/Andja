using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	public UserStructure contact;
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
		Unit u = coll.gameObject.GetComponent<Unit> ();
		if (u != null) {
			u.isInRangeOfWarehouse (contact);
			((Warehouse)contact).addUnitToTrade (u);
		}
	}
	void OnCollisionExit2D(Collision2D coll) {
		Unit u = coll.gameObject.GetComponent<Unit> ();
		if (coll.gameObject.GetComponent<Unit> () != null) {
			u.isInRangeOfWarehouse (null);
			((Warehouse)contact).removeUnitFromTrade (u);
		}
	}
}
