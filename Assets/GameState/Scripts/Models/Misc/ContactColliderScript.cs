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
			u.IsInRangeOfWarehouse (contact);
			((Warehouse)contact).AddUnitToTrade (u);
		}
	}

	void OnTriggerEnter2D(Collider2D coll) {
		UnitHoldingScript uhs = coll.gameObject.GetComponent<UnitHoldingScript> ();
		if (uhs != null && uhs.unit.inventory!=null) {
			Unit u = uhs.unit;
			u.IsInRangeOfWarehouse (contact);
			((Warehouse)contact).AddUnitToTrade (u);
		}
	}
	void OnCollisionExit2D(Collision2D coll) {
		UnitHoldingScript uhs = coll.gameObject.GetComponent<UnitHoldingScript> ();
		if (uhs != null && uhs.unit.inventory!=null) {
			Unit u = uhs.unit;
			u.IsInRangeOfWarehouse (null);
			((Warehouse)contact).RemoveUnitFromTrade (u);
		}
	}
}
