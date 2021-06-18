using Andja.Model;
using Newtonsoft.Json;
using System;
using UnityEngine;
using Andja.Controller;

namespace Andja.FogOfWar {
    [JsonObject(MemberSerialization.OptIn)]
    public class FogOfWarStructure : MonoBehaviour {
        Structure structure;
        public bool isCurrentlyVisible;
        [JsonPropertyAttribute] public bool isBeingShown;
        [JsonPropertyAttribute] public string lastShownSprite;
        [JsonPropertyAttribute] public uint buildID; //link to the build structure
        [JsonPropertyAttribute] public string id = null; //if the linked structure is destroyed -- we need to know the size etc
        [JsonPropertyAttribute] public int rotation = 0; //and -- we need to know the rotation of the sprite
        public void Link(Structure structure) {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            buildID = structure.buildID;
            this.structure = structure;
            structure.RegisterOnDestroyCallback(Destroyed);
        }
        public void ShowLastShown() {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = StructureSpriteController.Instance.GetSprite(lastShownSprite);
        }
        public void LoadStructureDestroyed() {
            ShowLastShown();
        }
        private void Destroyed(Structure str, IWarfare des) {
            if(isCurrentlyVisible) {
                Destroy(this.gameObject);
                return;
            }
            id = str.ID;
            rotation = str.rotation;
            structure = null;
        }

        private void OnTriggerEnter2D(Collider2D collision) {
            if(structure == null) {
                Destroy(this.gameObject);
                return;
            }
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = true;
                isBeingShown = true;
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                sr.maskInteraction = SpriteMaskInteraction.None;
                StructureSpriteController.Instance.OnStructureChanged(structure);
            }
        }
        private void OnTriggerExit2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = false;
            }
            lastShownSprite = structure.SpriteName;
        }
    }

}
