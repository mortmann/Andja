using Andja.Model;
using Andja.Utility;

namespace Andja.Controller {
    public class UpgradeMouseState : BaseMouseState {
        public override CursorType CursorType => CursorType.Upgrade;
        public override void Update() {
            Tile t = MouseController.Instance.GetTileUnderneathMouse();
            MouseController.Instance.NeededItemsToBuild = null;
            MouseController.Instance.NeededBuildCost = 0;
            if (t.Structure == null)
                return;
            if (t.Structure.CanBeUpgraded == false)
                return;
            Structure upgradeTo = null;
            foreach (string item in t.Structure.CanBeUpgradedTo) {
                if (t.Structure is HomeStructure == false && PlayerController.CurrentPlayer.HasStructureUnlocked(item) == false)
                    continue;
                upgradeTo = PrototypController.Instance.GetStructure(item);
                MouseController.Instance.NeededItemsToBuild = upgradeTo.BuildingItems?.CloneArrayWithCounts();
                MouseController.Instance.NeededBuildCost = upgradeTo.BuildCost;
                break;
            }
            if (InputHandler.GetMouseButtonUp(InputMouse.Primary) && upgradeTo != null) {
                BuildController.Instance.BuildOnTile(upgradeTo, t.Structure.Tiles, PlayerController.currentPlayerNumber, false);
            }
        }
    }
}