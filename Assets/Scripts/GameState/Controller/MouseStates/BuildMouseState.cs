using Andja.Editor;
using Andja.Model;
using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace Andja.Controller {

    public abstract class BuildMouseState : BaseMouseState {
        protected Structure ToBuildStructure => MouseController.Instance.ToBuildStructure;
        protected GameObject _singleStructurePreview;
        protected Dictionary<Tile, TilePreview> _tileToPreviewGO = new Dictionary<Tile, TilePreview>();

        public override void Deactivate() {
            base.Deactivate();
            Reset();
            _singleStructurePreview = null;
            MouseController.Instance.ResetBuildCost();
            UI.Model.IslandInfoUI.Instance.ResetAddons();
        }

        public override void Reset() {
            base.Reset();
            foreach (Tile t in _tileToPreviewGO.Keys) {
                SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
            }
            _tileToPreviewGO.Clear();
            ResetStructurePreviews();
        }

        /// <summary>
        /// Change the location and rotation of a single preview.
        /// </summary>
        protected void UpdateSinglePreview() {
            if (_singleStructurePreview == null)
                _singleStructurePreview = MouseController.Instance.CreatePreviewStructure();
            float x = ToBuildStructure.TileWidth / 2f - TileSpriteController.offset;
            float y = ToBuildStructure.TileHeight / 2f - TileSpriteController.offset;
            Tile underMouse = MouseController.Instance.GetTileUnderneathMouse();
            _singleStructurePreview.transform.position = new Vector3(underMouse.X + x, underMouse.Y + y, 0);
            _singleStructurePreview.transform.eulerAngles = new Vector3(0, 0, 360 - ToBuildStructure.Rotation);
            List<Tile> tiles = ToBuildStructure.GetBuildingTiles(underMouse);
            foreach (Tile tile in _tileToPreviewGO.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(_tileToPreviewGO[tile].gameObject);
                _tileToPreviewGO.Remove(tile);
            }
            UpdateStructurePreview(tiles, 1);
            MouseController.Instance.NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts();
            MouseController.Instance.NeededBuildCost = ToBuildStructure.BuildCost;
        }

        /// <summary>
        /// Updates the Preview Tiles based on if the player has enough ressources, monemy or if the enough citytiles
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="number"></param>
        protected void UpdateStructurePreview(List<Tile> tiles, int number) {
            if (EditorController.IsEditor) {
                UpdateStructurePreviewTiles(tiles, true);
                return;
            }
            bool hasEnoughResources = PlayerController.CurrentPlayer.HasEnoughMoney(ToBuildStructure.BuildCost * number);
            if (MouseController.Instance.MouseUnitState == MouseUnitState.Build) {
                hasEnoughResources &= MouseController.Instance.SelectedUnit.Inventory.HasEnoughOfItems(ToBuildStructure.BuildingItems, times: number) == true;
            }
            else {
                hasEnoughResources &= tiles[0].Island?.FindCityByPlayer(PlayerController.currentPlayerNumber)?
                            .HasEnoughOfItems(ToBuildStructure.BuildingItems?.CloneArrayWithCounts(), number) == true;
            }
            UpdateStructurePreviewTiles(tiles, hasEnoughResources);
        }
        /// <summary>
        /// Updates the Preview Tiles red/green bassed on override or if it can be build on that tile
        /// if <paramref name="dontOverrideTile"/> is false it will make it red - if true it does not influenz it
        /// </summary>
        /// <param name="tiles"></param>
        /// <param name="dontOverrideTile"></param>
        protected void UpdateStructurePreviewTiles(List<Tile> tiles, bool dontOverrideTile) {
            Dictionary<Tile, bool> tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(tiles);
            if (MouseController.Instance.MouseState == MouseState.BuildSingle && MouseController.Autorotate) {
                int i = 0;
                while (tileToCanBuild.ContainsValue(false) && i < 4) {
                    ToBuildStructure.Rotate();
                    tiles = ToBuildStructure.GetBuildingTiles(MouseController.Instance.GetTileUnderneathMouse());
                    //TODO: think about a not so ugly solution for autorotate
                    tileToCanBuild = ToBuildStructure.CheckForCorrectSpot(tiles);
                    i++;
                }
            }
            dontOverrideTile &= EditorController.IsEditor || ToBuildStructure.InCityCheck(tiles, PlayerController.currentPlayerNumber);
            foreach (Tile tile in tiles) {
                bool specialTileCheck = true;
                if (MouseController.Instance.MouseUnitState == MouseUnitState.Build) {
                    specialTileCheck = MouseController.Instance.SelectedUnit.IsTileInBuildRange(tile);
                }
                bool canBuild = dontOverrideTile && specialTileCheck && tileToCanBuild[tile];
                canBuild &= EditorController.IsEditor || Structure.IsTileCityViable(tile, PlayerController.currentPlayerNumber);
                canBuild &= tile.Island != null && tile.Island.HasNegativeEffect == false;
                ShowTilePrefabOnTile(tile, canBuild ? TileHighlightType.Green : TileHighlightType.Red);
            }
        }
        protected void ResetStructurePreviews() {
            foreach (Tile t in _tileToPreviewGO.Keys) {
                SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
            }
            _tileToPreviewGO.Clear();
            if (_singleStructurePreview == false) return;
            SimplePool.Despawn(_singleStructurePreview);
            foreach (Transform t in _singleStructurePreview.transform) {
                Object.Destroy(t.gameObject);
            }
            _singleStructurePreview = null;
        }
        /// <summary>
        /// Decides if new prefab tile needs to be spawned or despwaned.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="type"></param>
        protected void ShowTilePrefabOnTile(Tile t, TileHighlightType type) {
            if (_tileToPreviewGO.ContainsKey(t)) {
                if (_tileToPreviewGO[t].HighlightType == type) {
                    return;
                }
                else {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                }
            }

            GameObject go = type switch {
                TileHighlightType.Green => MouseController.Instance.greenTileCursorPrefab,
                TileHighlightType.Red => MouseController.Instance.redTileCursorPrefab,
                _ => null
            };
            go = SimplePool.Spawn(go, new Vector3(t.X + 0.5f, t.Y + 0.5f, 0), Quaternion.identity);
            // Display the building hint on top of this tile position
            //go.transform.SetParent(this.transform, true);
            _tileToPreviewGO.Add(t, new TilePreview(type, go));
        }
        protected class TilePreview {
            public readonly GameObject gameObject;
            public readonly TileHighlightType HighlightType;

            public TilePreview(TileHighlightType type, GameObject gameObject) {
                HighlightType = type;
                this.gameObject = gameObject;
            }
        }
        protected class StructurePreview {
            public readonly GameObject gameObject;
            public readonly List<Tile> tiles;
            public readonly Tile tile;
            public readonly int number;
            public readonly SpriteRenderer spriteRenderer;

            public StructurePreview(Tile tile, GameObject gameObject, List<Tile> tiles, int number) {
                this.tile = tile;
                this.gameObject = gameObject;
                spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
                this.tiles = tiles;
                this.number = number;
            }
        }
    }
}