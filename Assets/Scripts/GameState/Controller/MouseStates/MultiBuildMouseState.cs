using Andja.Editor;
using Andja.Model;
using Andja.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public abstract class MultiBuildMouseState : BuildMouseState {
        protected Vector3 LastFrameGuiPosition => MouseController.Instance.LastFrameGuiPosition;
        protected Vector3 CurrentFramePosition => MouseController.Instance.CurrentFramePosition;
        protected Vector3 DragStartPosition;
        protected Vector3 PathStartPosition;
        protected Dictionary<Tile, StructurePreview> _tileToStructurePreview = new Dictionary<Tile, StructurePreview>();
        protected Vector3 CurrentFramePositionOffset => CurrentFramePosition
                                            + new Vector3(TileSpriteController.offset, TileSpriteController.offset, 0);

        /// <summary>
        /// Updates which previews are to be deleted and where to create new ones.
        /// </summary>
        /// <param name="tiles"></param>
        protected void UpdateMultipleStructurePreviews(IEnumerable<Tile> tiles) {
            foreach (Tile tile in _tileToStructurePreview.Keys.Except(tiles).ToArray()) {
                SimplePool.Despawn(_tileToStructurePreview[tile].gameObject);
                foreach (Tile t in _tileToStructurePreview[tile].tiles) {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                }
                _tileToStructurePreview.Remove(tile);
            }
            foreach (Tile tile in tiles) {
                if (_tileToStructurePreview.ContainsKey(tile)) continue;
                StructurePreview preview = new StructurePreview(
                    tile,
                    CreatePreviewStructure(tile),
                    ToBuildStructure.GetBuildingTiles(tile), _tileToStructurePreview.Count + 1
                );
                _tileToStructurePreview[tile] = preview;
            }
            MouseController.Instance.NeededItemsToBuild = ToBuildStructure.BuildingItems?.CloneArrayWithCounts(tiles.Count());
            MouseController.Instance.NeededBuildCost = ToBuildStructure.BuildCost * tiles.Count();
            foreach (StructurePreview preview in _tileToStructurePreview.Values) {
                if (ToBuildStructure is RoadStructure) {
                    string sprite = ToBuildStructure.SpriteName + RoadStructure.UpdateOrientation(preview.tile, tiles);
                    preview.spriteRenderer.sprite = StructureSpriteController.Instance.GetStructureSprite(sprite);
                }
                UpdateStructurePreview(preview.tiles, preview.number);
            }
        }
        /// <summary>
        /// Calculates for the given rectangle which tiles are the buildtiles for selected structure.
        /// </summary>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <param name="startY"></param>
        /// <param name="endY"></param>
        /// <returns></returns>
        protected List<Tile> GetTilesStructures(int startX, int endX, int startY, int endY) {
            int width = 1;
            int height = 1;
            List<Tile> tiles = new List<Tile>();
            if (ToBuildStructure != null) {
                width = ToBuildStructure.TileWidth;
                height = ToBuildStructure.TileHeight;
            }
            if (endX >= startX && endY >= startY) {
                for (int x = startX; x <= endX; x += width) {
                    for (int y = startY; y <= endY; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX > startX && endY <= startY) {
                for (int x = startX; x <= endX; x += width) {
                    for (int y = startY; y >= endY; y -= height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX <= startX && endY > startY) {
                for (int x = startX; x >= endX; x -= width) {
                    for (int y = startY; y <= endY; y += height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            else
            if (endX <= startX && endY <= startY) {
                for (int x = startX; x >= endX; x -= width) {
                    for (int y = startY; y >= endY; y -= height) {
                        tiles.Add(World.Current.GetTileAt(x, y));
                    }
                }
            }
            return tiles;
        }
        public override void Reset() {
            base.Reset();
            foreach (Tile tile in _tileToStructurePreview.Keys) {
                foreach (Transform t in _tileToStructurePreview[tile].gameObject.transform) {
                    Object.Destroy(t.gameObject);
                }
                SimplePool.Despawn(_tileToStructurePreview[tile].gameObject);
            }
            _tileToStructurePreview.Clear();
        }

    }
   
}
