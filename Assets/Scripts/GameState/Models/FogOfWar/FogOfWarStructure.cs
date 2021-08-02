using Andja.Model;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;
using Andja.Controller;
using System;
using Andja.Utility;
namespace Andja.FogOfWar {
    /// <summary>
    /// IS responsible for what is being shown on the map when the structure is invisible.
    /// And when it is supposed to be visible/completly hidden 
    /// -- counterpart for unit is on the ITargetableHoldingScript because it is a simple bool for visible (when it can be clicked on)
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class FogOfWarStructureData {
        [JsonPropertyAttribute] public bool isBeingShown;
        [JsonPropertyAttribute] public string lastShownSprite;
        [JsonPropertyAttribute] public uint buildID; //link to the build structure
        [JsonPropertyAttribute] public string id = null; //if the linked structure is destroyed -- we need to know the size etc
        [JsonPropertyAttribute] public int rotation = 0; //and -- we need to know the rotation of the sprite
        [JsonPropertyAttribute] public SeriaziableVector2 buildTileVector;
    }

    public class FogOfWarStructure : MonoBehaviour {
        public Structure structure;
        private bool isCurrentlyVisible;
        public bool IsCurrentlyVisible {
            get {
                return isCurrentlyVisible || structure.IsPlayer();
            }
        }
        public FogOfWarStructureData Data = new FogOfWarStructureData();

        public void Link(Structure structure) {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            Data.buildID = structure.buildID;
            this.structure = structure;
            structure.RegisterOnDestroyCallback(Destroyed);
        }
        public void ShowLastShown() {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            sr.sprite = StructureSpriteController.Instance.GetSprite(Data.lastShownSprite);
        }
        public void LoadStructure() {
            this.structure = BuildController.Instance.buildIdToStructure[Data.buildID];
            ShowLastShown();
        }
        public void LoadStructureDestroyed() {
            var structure = PrototypController.Instance.GetStructureCopy(Data.id);
            if(structure == null) {
                Destroy(gameObject);
                return;
            }
            float x = ((float)structure.TileWidth) / 2f - TileSpriteController.offset;
            float y = ((float)structure.TileHeight) / 2f - TileSpriteController.offset;
            transform.position = new Vector3(Data.buildTileVector.X + x, Data.buildTileVector.Y + y);
            transform.transform.eulerAngles = new Vector3(0, 0, 360 - Data.rotation);
            SpriteRenderer sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = structure.SortingLayer;
            ShowLastShown();
            structure.ChangeRotation(Data.rotation);
            //structure.GetBuildingTiles(Data.buildTile);
        }
        private void Destroyed(Structure str, IWarfare des) {
            //do we see it at the moment or is it not shown atall -- if so we can destroy it directly
            if(isCurrentlyVisible || Data.isBeingShown == false) {
                FogOfWarController.Instance.RemoveFogOfWarStructure(Data.buildID);
                Destroy(this.gameObject);
                return;
            }
            Data.id = str.ID;
            Data.rotation = str.rotation;
            structure = null;
            Data.buildID = 0;
            Data.buildTileVector = str.BuildTile.Vector2;
            foreach (Tile tile in str.Tiles) {
                ((LandTile)tile).fogOfWarStructure = this;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision) {
            if(structure == null) {
                FogOfWarController.Instance.RemoveFogOfWarStructure(Data.buildID);
                Destroy(this.gameObject);
                return;
            }
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = true;
                Data.isBeingShown = true;
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                sr.maskInteraction = SpriteMaskInteraction.None;
                StructureSpriteController.Instance.OnStructureChanged(structure);
            }
        }
        private void OnTriggerExit2D(Collider2D collision) {
            if (collision.gameObject.GetComponent<FogOfWarTrigger>() != null) {
                isCurrentlyVisible = false;
            }
            if(structure != null)
                Data.lastShownSprite = structure.SpriteName;
        }

        internal void Set(FogOfWarStructureData data) {
            Data = data;
            if(data.buildID == 0) {
                LoadStructureDestroyed();
            } else
            if(BuildController.Instance.buildIdToStructure.ContainsKey(data.buildID)) {
                Link(BuildController.Instance.buildIdToStructure[data.buildID]);
            }
        }

        public static bool IsStructureVisible(GameObject go) {
            if (FogOfWarController.IsFogOfWarAlways) {
                FogOfWarStructure fwg = go.GetComponent<FogOfWarStructure>();
                if (fwg != null) {
                    return fwg.IsCurrentlyVisible;
                }
            }
            return true;
        } 

    }

}
