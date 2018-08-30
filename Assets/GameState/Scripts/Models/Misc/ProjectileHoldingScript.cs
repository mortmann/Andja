using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileHoldingScript : MonoBehaviour {

    public Projectile myProjectile;

    private void Start() {
        myProjectile.RegisterOnDestroyCallback(OnProjectileDestroy);
    }

    private void OnProjectileDestroy(Projectile obj) {
        Destroy(this.gameObject);
    }

    // Update is called once per frame
    void Update () {
        transform.position = myProjectile.Position;
	}
    //Doesnt get triggerd on hit because itself is a trigger
    private void OnCollisionEnter2D(Collision2D collision) {
        ITargetableHoldingScript iths = collision.collider.GetComponent<ITargetableHoldingScript>();
        if(iths == null) {
            return;
        }
        if (myProjectile.OnHit(iths.Holding)) {
            Destroy(this);
        }
    }
    //THIS one is the one that works for now! Because itself is a trigger!
    private void OnTriggerEnter2D(Collider2D collider) {
        ITargetableHoldingScript iths = collider.GetComponent<ITargetableHoldingScript>();
        if (iths == null) {
            return;
        }
        if (myProjectile.OnHit(iths.Holding)) {
            Destroy(this);
        }
    }
}
