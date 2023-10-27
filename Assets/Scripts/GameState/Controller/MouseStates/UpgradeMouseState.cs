using Andja.Model;
using Andja.Utility;

namespace Andja.Controller {
    public class UpgradeMouseState : BaseMouseState {
        public override CursorType CursorType => CursorType.Upgrade;
        public override void Update() {
            if (InputHandler.GetMouseButtonDown(InputMouse.Secondary)) {
                MouseController.Instance.SetMouseState(MouseState.Idle);
            }
            Structure structure = MouseController.Instance.GetTileUnderneathMouse()?.Structure;
            if (structure == null || structure.CanBeUpgraded == false) {
                MouseController.Instance.ResetBuildCost();
                return;
            }
            Structure upgradeTo = null;
            foreach (string item in structure.CanBeUpgradedTo) {
                if (structure is HomeStructure == false && PlayerController.CurrentPlayer.HasStructureUnlocked(item) == false)
                    continue;
                upgradeTo = PrototypController.Instance.GetStructure(item);
                MouseController.Instance.NeededItemsToBuild = upgradeTo.BuildingItems?.CloneArrayWithCounts();
                MouseController.Instance.NeededBuildCost = upgradeTo.BuildCost;
                break;
            }
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) && upgradeTo != null) {
                if(structure is HomeStructure home) {
                    home.UpgradeHouse();
                    return;
                }
                BuildController.Instance.BuildOnTile(upgradeTo, structure.Tiles, PlayerController.currentPlayerNumber, false);
            }
        }
        public override void Deactivate() {
            MouseController.Instance.ResetBuildCost();
        }
    }
}