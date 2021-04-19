using UnityEngine;

namespace Andja.Model {

    public class ProjectileHoldingScript : MonoBehaviour {
        public Projectile Projectile;
        private Rigidbody2D body;

        private void Start() {
            transform.position = Projectile.Position;
            Projectile.RegisterOnDestroyCallback(OnProjectileDestroy);
            body = gameObject.AddComponent<Rigidbody2D>();
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.velocity = Projectile.Velocity.Vec;
            body.gravityScale = 0;
        }

        private void OnProjectileDestroy(Projectile obj) {
            Destroy(this.gameObject);
        }

        // Update is called once per frame
        private void Update() {
            //transform.position = Projectile.Position;
        }

        //Doesnt get triggerd on hit because itself is a trigger
        private void OnCollisionEnter2D(Collision2D collision) {
            ITargetableHoldingScript iths = collision.collider.GetComponent<ITargetableHoldingScript>();
            if (iths == null) {
                return;
            }
            if (Projectile.OnHit(iths.Holding)) {
                Destroy(this);
            }
        }

        //THIS one is the one that works for now! Because itself is a trigger!
        private void OnTriggerEnter2D(Collider2D collider) {
            ITargetableHoldingScript iths = collider.GetComponent<ITargetableHoldingScript>();
            if (iths == null) {
                return;
            }
            if (Projectile.OnHit(iths.Holding)) {
                Destroy(this);
            }
        }
    }
}