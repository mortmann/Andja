using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	public OutputStructure contact;
	//dont know why this aint working
	void OnCollisionEnter2D(Collision2D coll) {
		ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript> ();
        if (ihs == null || ihs.IsUnit == false)
            return;
        Unit unit = (Unit)ihs.Holding;
        if (unit.inventory!=null) {
            unit.IsInRangeOfWarehouse (contact);
			((Warehouse)contact).AddUnitToTrade (unit);
		}
	}
	void OnTriggerEnter2D(Collider2D coll) {
        ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript>();
        if (ihs == null || ihs.IsUnit == false)
            return;
        Unit unit = (Unit)ihs.Holding;
        if (unit.inventory!=null) {
			unit.IsInRangeOfWarehouse (contact);
			((Warehouse)contact).AddUnitToTrade (unit);
		}
	}
	void OnCollisionExit2D(Collision2D coll) {
        ITargetableHoldingScript ihs = coll.gameObject.GetComponent<ITargetableHoldingScript>();
        if (ihs == null || ihs.IsUnit == false)
            return;
        Unit unit = (Unit)ihs.Holding;
        if (unit.inventory != null) {
            unit.IsInRangeOfWarehouse (null);
			((Warehouse)contact).RemoveUnitFromTrade (unit);
		}
	}
}
