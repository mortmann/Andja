using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.FogOfWar {
    public class FogOfWarTrigger : MonoBehaviour {

        private void OnTriggerEnter2D(Collider2D collision) {
            Debug.Log(collision.gameObject.name);
        }


    }
}
