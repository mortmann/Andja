using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	public OutputStructure contact;
	//dont know why this aint working
	void OnCollisionEnter2D(Collision2D coll) {
		UnitHoldingScript uhs = coll.gameObject.GetComponent<UnitHoldingScript> ();
		if (uhs != null && uhs.unit.inventory!=null) {
			Unit u = uhs.unit;
			u.isInRangeOfWarehouse (contact);
			((Warehouse)contact).addUnitToTrade (u);
		}
	}

	void OnTriggerEnter2D(Collider2D coll) {
		UnitHoldingScript uhs = coll.gameObject.GetComponent<UnitHoldingScript> ();
		if (uhs != null && uhs.unit.inventory!=null) {
			Unit u = uhs.unit;
			u.isInRangeOfWarehouse (contact);
			((Warehouse)contact).addUnitToTrade (u);
		}
	}
	void OnCollisionExit2D(Collision2D coll) {
		UnitHoldingScript uhs = coll.gameObject.GetComponent<UnitHoldingScript> ();
		if (uhs != null && uhs.unit.inventory!=null) {
			Unit u = uhs.unit;
			u.isInRangeOfWarehouse (null);
			((Warehouse)contact).removeUnitFromTrade (u);
		}
	}
}
