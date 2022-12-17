using Andja.Model;
using Andja.Pathfinding;
using Andja.Utility;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Andja.Controller {
    public class PathBuildMouseState : MultiBuildMouseState {
        private PathJob _buildPathJob;
        private BuildPathAgent _buildPathAgent;
        public PathBuildMouseState() {
            _buildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber);
            PlayerController.Instance.cbPlayerChange += (a, b) => { _buildPathAgent = new BuildPathAgent(PlayerController.currentPlayerNumber); };
        }
        public override void Update() {
            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary)) {
                MouseController.Instance.ResetBuild();
                MouseController.Instance.SetMouseState(MouseState.Idle);
                return;
            }
            if (EventSystem.current.IsPointerOverGameObject()) {
                return;
            }
            // Start Path
            if (InputHandler.GetMouseButtonDown(InputMouse.Primary)) {
                PathStartPosition = CurrentFramePositionOffset;
                ResetSingleStructurePreview();
            }
            if (InputHandler.GetMouseButton(InputMouse.Primary)) {
                int startX = Mathf.FloorToInt(PathStartPosition.x);
                int startY = Mathf.FloorToInt(PathStartPosition.y);
                Tile pathStartTile = World.Current.GetTileAt(startX, startY);

                if (pathStartTile == null || pathStartTile.Island == null) {
                    return;
                }
                int endX = Mathf.FloorToInt(CurrentFramePositionOffset.x);
                int endY = Mathf.FloorToInt(CurrentFramePositionOffset.y);
                Tile pathEndTile = World.Current.GetTileAt(endX, endY);
                if (pathEndTile == null) {
                    return;
                }
                if (pathStartTile.Island != null && pathEndTile.Island != null &&
                        (_buildPathJob == null || _buildPathJob.End != pathEndTile.Vector2)) {
                    _buildPathJob = new PathJob(_buildPathAgent, pathStartTile.Island.Grid, pathStartTile.Vector2, pathEndTile.Vector2);
                    PathfindingThreadHandler.EnqueueJob(_buildPathJob, null, true);
                    if (_buildPathJob.Path != null)
                        UpdateMultipleStructurePreviews(World.Current.GetTilesQueue(_buildPathJob.Path));
                }
            }
            else {
                UpdateSinglePreview();
            }
            // End path
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) == false) return;
            Reset();
            if (_buildPathJob == null || _buildPathJob.Status != JobStatus.Done) {
                return;
            }
            MouseController.Instance.Build(World.Current.GetTilesQueue(_buildPathJob.Path).ToList(), true);
        }
    }
}
