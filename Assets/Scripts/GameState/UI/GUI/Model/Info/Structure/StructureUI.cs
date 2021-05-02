using Andja.Controller;
using Andja.Model;
using UnityEngine;

namespace Andja.UI.Model {

    public class StructureUI : MonoBehaviour {
        private Structure currentStructure;

        public void Show(Structure Info) {
            if (currentStructure == Info) {
                return;
            }
            currentStructure = Info as Structure;
            if (currentStructure == null)
                return;
            TileDeciderFuncs.Structure = currentStructure;
            TileSpriteController.Instance.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
            if (currentStructure.StructureRange > 0) {
                TileSpriteController.Instance.AddDecider(TileDeciderFuncs.StructureTileDecider);
            }
        }
        private void Update() {
            InfoUI.Instance.UpdateHealth(currentStructure.CurrentHealth, currentStructure.MaxHealth);
            InfoUI.Instance.UpdateUpkeep(currentStructure.UpkeepCost);
        }
        void OnDisable() {
            TileDeciderFuncs.Structure = null;
            TileSpriteController.Instance?.RemoveDecider(TileDeciderFuncs.StructureTileDecider);
        }
    }
}