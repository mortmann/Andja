using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	public UserStructure contact;

	void OnCollisionEnter2D(Collision2D coll) {
		Unit u = coll.gameObject.GetComponent<Unit> ();
		if (u != null)
			u.isInRangeOfWarehouse (contact);

	}

	void OnCollisionExit2D(Collision2D coll) {
		Unit u = coll.gameObject.GetComponent<Unit> ();
		if (coll.gameObject.GetComponent<Unit> ()!=null)
			u.isInRangeOfWarehouse (contact);

	}
}
