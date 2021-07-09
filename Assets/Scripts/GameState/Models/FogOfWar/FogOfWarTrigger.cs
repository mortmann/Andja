using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Andja.FogOfWar {
    public class FogOfWarTrigger : MonoBehaviour {
        private void Start() {
            if (Controller.FogOfWarController.IsFogOfWarAlways == false)
                Destroy(gameObject);
        }

        //private void OnTriggerEnter2D(Collider2D collision) {
        //}

    }
}
