using Andja.Editor;
using Andja.Model;
using Andja.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class DestroyBuildMouseState : MultiBuildMouseState {
        private HashSet<Tile> DestroyTiles;
        public override CursorType CursorType => CursorType.Destroy;
        public DestroyBuildMouseState() {
            DestroyTiles = new HashSet<Tile>();
        }
        public override void Update() {
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                DragStartPosition = CurrentFramePositionOffset;
            }
            int startX = Mathf.FloorToInt(DragStartPosition.x);
            int endX = Mathf.FloorToInt(CurrentFramePositionOffset.x);
            int startY = Mathf.FloorToInt(DragStartPosition.y);
            int endY = Mathf.FloorToInt(CurrentFramePositionOffset.y);
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                List<Tile> tiles = GetTilesStructures(startX, endX, startY, endY);
                foreach (Tile t in DestroyTiles.Except(tiles).ToArray()) {
                    SimplePool.Despawn(_tileToPreviewGO[t].gameObject);
                    _tileToPreviewGO.Remove(t);
                    DestroyTiles.Remove(t);
                }
                foreach (Tile t in tiles) {
                    if (DestroyTiles.Contains(t))
                        continue;
                    ShowTilePrefabOnTile(t, TileHighlightType.Red);
                    DestroyTiles.Add(t);
                }
            }

            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            List<Tile> ts = new List<Tile>(GetTilesStructures(startX, endX, startY, endY));
            if (ts.Count > 0) {
                bool isGod = EditorController.IsEditor || MouseController.Instance.IsGod; //TODO: add cheat to set this
                BuildController.Instance.DestroyStructureOnTiles(ts, PlayerController.CurrentPlayer, isGod);
            }
            MouseController.Instance.ResetBuild(false);
        }
        public override void Deactivate() {
            base.Deactivate();
            DestroyTiles.Clear();
        }
    }
}
