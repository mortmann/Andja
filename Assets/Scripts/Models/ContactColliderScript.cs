using UnityEngine;
using System.Collections;
using System;

public class ContactColliderScript : MonoBehaviour {
	CircleCollider2D cc2d;
	// Use this for initialization
	void Start () {
		cc2d = gameObject.GetComponent<CircleCollider2D>();
	}
	

	public void OnCollisionEnter(Collision coll){
		
	}
}
